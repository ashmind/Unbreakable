using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CommandLine;
using Unbreakable.Policy;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Tools.PolicyReport {
    using static ApiAccess;

    public static class Program {
        public static int Main(string[] args) {
            var exitCode = 0;
            Parser.Default.ParseArguments<Arguments>(args).WithParsed(arguments => {
                try {
                    SafeMain(arguments);
                }
                catch (Exception ex) {
                    Console.WriteLine(ex);
                    exitCode = ex.HResult;
                }
            }).WithNotParsed(errors => {
                foreach (var error in errors) {
                    Console.WriteLine(error);
                }
                exitCode = 1;
            });
            return exitCode;
        }

        private static void SafeMain(Arguments arguments) {
            var context = new AssemblyLoadContext("Policy");
            ConfigureAssemblyResolution(context, arguments.PolicyFactoryAssemblyPath);

            var policyAssembly = context.LoadFromAssemblyPath(arguments.PolicyFactoryAssemblyPath);
            var assemblies = new HashSet<Assembly> { policyAssembly };

            LoadAllReferencedAssemblies(context, assemblies);

            var policy = GetPolicy(policyAssembly, arguments.PolicyFactoryTypeName, arguments.PolicyFactoryMethodName);

            var namespaces = assemblies
                .SelectMany(GetExportedTypesSafe)
                .GroupBy(GetNamespace)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());

            using (var writer = new StreamWriter(arguments.OutputPath)) {
                foreach (var @namespace in namespaces.OrderBy(n => n.Key)) {
                    WriteNamespaceReport(writer, @namespace.Key, @namespace.Value, policy);
                }
            }
        }

        private static void ConfigureAssemblyResolution(AssemblyLoadContext policyContext, string policyFactoryAssemblyPath) {
            var policyAssemblyPathBase = Path.GetDirectoryName(policyFactoryAssemblyPath);
            string GetCandidatePath(string assemblyName) {
                var path = assemblyName + ".dll";
                if (policyAssemblyPathBase != null)
                    path = Path.Combine(policyAssemblyPathBase, path);
                return path;
            }

            policyContext.Resolving += (sender, assemblyName) => {
                if (assemblyName.Name == "Unbreakable" || assemblyName.Name == null)
                    return null;

                var candidatePath = GetCandidatePath(assemblyName.Name);
                if (File.Exists(candidatePath))
                    return policyContext.LoadFromAssemblyPath(candidatePath);

                return null;
            };

            AssemblyLoadContext.Default.Resolving += (sender, assemblyName) => {
                if (assemblyName.Name == "Unbreakable.Policy")
                    return policyContext.LoadFromAssemblyPath(GetCandidatePath(assemblyName.Name));

                return null;
            };
        }

        private static void LoadAllReferencedAssemblies(AssemblyLoadContext context, HashSet<Assembly> assemblies) {
            var queue = new Queue<Assembly>(assemblies);
            while (queue.TryDequeue(out var assembly)) {
                foreach (var reference in assembly.GetReferencedAssemblies()) {
                    if (!TryLoadAssembly(context, reference, out var loaded))
                        continue;

                    if (assemblies.Add(loaded))
                        queue.Enqueue(loaded);
                }
            }
        }

        private static bool TryLoadAssembly(AssemblyLoadContext context, AssemblyName name, [NotNullWhen(true)] out Assembly? assembly) {
            try {
                assembly = context.LoadFromAssemblyName(name);
                return true;
            }
            catch (FileNotFoundException) {
                Console.WriteLine($"[WARN] Could not find assembly '{name.FullName}'.");
                assembly = null;
                return false;
            }
        }

        private static Type[] GetExportedTypesSafe(Assembly assembly) {
            try {
                return assembly.GetExportedTypes();
            }
            catch (FileNotFoundException ex) {
                Console.WriteLine($"[WARN] Could not load types from '{assembly.Location}':");
                Console.WriteLine(ex);
                return new Type[0];
            }
        }

        private static void WriteNamespaceReport(StreamWriter writer, string @namespace, IEnumerable<Type> types, ApiPolicy policy) {
            if (!policy.Namespaces.TryGetValue(@namespace, out var namespacePolicy))
                return;
            writer.WriteLine(@namespace);
            if (namespacePolicy.Access == Denied)
                return;

            var typesWithNames = types.Select(type => new {
                value = type,
                name = @namespace.Length > 0 ? type.FullName!.Substring(@namespace.Length + 1) : type.FullName!
            });
            foreach (var type in typesWithNames.OrderBy(t => t.name)) {
                WriteTypeReport(writer, type.value, type.name, namespacePolicy);
            }
        }

        private static void WriteTypeReport(StreamWriter writer, Type type, string typeName, NamespacePolicy namespacePolicy) {
            if (!namespacePolicy.Types.TryGetValue(typeName, out TypePolicy? typePolicy))
                typePolicy = null;

            var effectiveTypeAccess = GetEffectiveTypeAccess(typePolicy?.Access, namespacePolicy.Access);

            writer.Write("  ");
            writer.Write(typeName);
            writer.Write(": ");
            writer.WriteLine(effectiveTypeAccess);

            if (effectiveTypeAccess == Denied)
                return;

            foreach (var methodName in type.GetMembers().OfType<MethodBase>().Select(m => m.Name).Distinct().OrderBy(n => n)) {
                WriteMethodReport(writer, methodName, typePolicy, effectiveTypeAccess);
            }
        }

        private static void WriteMethodReport(StreamWriter writer, string methodName, TypePolicy? typePolicy, ApiAccess effectiveTypeAccess) {
            var methodPolicy = (MemberPolicy?)null;
            if (typePolicy != null)
                typePolicy.Members.TryGetValue(methodName, out methodPolicy);

            var effectiveMethodAccess = GetEffectiveMethodAccess(methodPolicy?.Access, typePolicy?.Access, effectiveTypeAccess);
            writer.Write("     ");
            writer.Write(methodName);
            writer.Write(": ");
            writer.Write(effectiveMethodAccess);
            if (methodPolicy?.HasRewriters ?? false) {
                writer.Write(" (");
                writer.Write(string.Join(", ", methodPolicy!.Rewriters.Cast<IMemberRewriterInternal>().Select(r => r.GetShortName())));
                writer.Write(")");
            }
            writer.WriteLine();
        }

        private static string? GetNamespace(Type type) {
            return !type.IsNested
                 ? type.Namespace
                 : GetNamespace(type.DeclaringType!);
        }

        private static ApiPolicy GetPolicy(Assembly assembly, string factoryTypeName, string factoryMethodName) {
            var factoryType = assembly.GetType(factoryTypeName, throwOnError: true)!;
            var factoryMethod = factoryType.GetMethod(factoryMethodName);
            if (factoryMethod == null)
                throw new Exception($"Method '{factoryMethodName}' was not found in '{factoryTypeName}'.");

            var factory = (object?)null;
            if (!factoryMethod.IsStatic)
                factory = Activator.CreateInstance(factoryType);

            return (ApiPolicy)factoryMethod.Invoke(factory, null)!;
        }

        private static ApiAccess GetEffectiveTypeAccess(ApiAccess? typeAccess, ApiAccess namespaceAccess) {
            if (typeAccess == null)
                return namespaceAccess == Allowed ? Allowed : Denied;

            if (typeAccess == Neutral)
                return Allowed;

            return typeAccess.Value;
        }

        private static ApiAccess GetEffectiveMethodAccess(ApiAccess? methodAccess, ApiAccess? typeAccess, ApiAccess effectiveTypeAccess) {
            return methodAccess ?? (typeAccess == Allowed ? Allowed : Denied);
        }
    }
}

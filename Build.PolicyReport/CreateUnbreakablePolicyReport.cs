using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Unbreakable.Policy;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Build.PolicyReport {
    using static ApiAccess;

    [LoadInSeparateAppDomain]
    public class CreateUnbreakablePolicyReport : Microsoft.Build.Utilities.AppDomainIsolatedTask {
        public override bool Execute() {
            var policy = GetPolicy();
            var assemblies = ReferencedAssemblyPaths.Select(Assembly.LoadFrom).ToDictionary(a => a.GetName().Name);
            SetupAssemblyResolution(assemblies);
            var namespaces = assemblies
                .Values
                .SelectMany(a => a.GetExportedTypes())
                .GroupBy(GetNamespace)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());

            using (var writer = new StreamWriter(OutputPath)) {
                foreach (var @namespace in namespaces.OrderBy(n => n.Key)) {
                    WriteNamespaceReport(writer, @namespace.Key, @namespace.Value, policy);
                }
            }

            return true;
        }
        
        private void WriteNamespaceReport(StreamWriter writer, string @namespace, IEnumerable<Type> types, ApiPolicy policy) {
            if (!policy.Namespaces.TryGetValue(@namespace, out var namespacePolicy))
                return;
            writer.WriteLine(@namespace);
            if (namespacePolicy.Access == Denied)
                return;

            var typesWithNames = types.Select(type => new {
                value = type,
                name = @namespace.Length > 0 ? type.FullName.Substring(@namespace.Length + 1) : type.FullName
            });
            foreach (var type in typesWithNames.OrderBy(t => t.name)) {
                WriteTypeReport(writer, type.value, type.name, namespacePolicy);
            }
        }

        private void WriteTypeReport(StreamWriter writer, Type type, string typeName, NamespacePolicy namespacePolicy) {
            if (!namespacePolicy.Types.TryGetValue(typeName, out var typePolicy))
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

        private void WriteMethodReport(StreamWriter writer, string methodName, TypePolicy typePolicy, ApiAccess effectiveTypeAccess) {
            if (!typePolicy.Members.TryGetValue(methodName, out var methodPolicy))
                methodPolicy = null;
            var effectiveMethodAccess = GetEffectiveMethodAccess(methodPolicy?.Access, typePolicy.Access, effectiveTypeAccess);
            writer.Write("     ");
            writer.Write(methodName);
            writer.Write(": ");
            writer.Write(effectiveMethodAccess);
            if (methodPolicy?.HasRewriters ?? false) {
                writer.Write(" (");
                writer.Write(string.Join(", ", methodPolicy.Rewriters.Cast<IMemberRewriterInternal>().Select(r => r.GetShortName())));
                writer.Write(")");
            }
            writer.WriteLine();
        }

        private string GetNamespace(Type type) {
            return !type.IsNested ? type.Namespace : GetNamespace(type.DeclaringType);
        }

        private void SetupAssemblyResolution(IReadOnlyDictionary<string, Assembly> assemblies) {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
                var name = new AssemblyName(e.Name);
                return assemblies.TryGetValue(name.Name, out var assembly) ? assembly : null;
            };
        }

        private ApiPolicy GetPolicy() {
            var policyAssembly = Assembly.LoadFrom(PolicyAssemblyPath);
            var type = policyAssembly.GetType(PolicyTypeName, true);
            var method = type.GetMethod(PolicyMethodName);
            if (method == null)
                throw new Exception($"Method '{PolicyMethodName}' was not found in '{PolicyTypeName}'.");
            return (ApiPolicy)method.Invoke(null, null);
        }

        private ApiAccess GetEffectiveTypeAccess(ApiAccess? typeAccess, ApiAccess namespaceAccess) {
            if (typeAccess == null)
                return namespaceAccess == Allowed ? Allowed : Denied;

            if (typeAccess == Neutral)
                return Allowed;

            return typeAccess.Value;
        }

        private ApiAccess GetEffectiveMethodAccess(ApiAccess? methodAccess, ApiAccess? typeAccess, ApiAccess effectiveTypeAccess) {           
            return methodAccess ?? (typeAccess == Allowed ? Allowed : Denied);
        }

        [Required] public string[] ReferencedAssemblyPaths { get; set; }
        [Required] public string PolicyAssemblyPath { get; set; }
        [Required] public string PolicyTypeName { get; set; }
        [Required] public string PolicyMethodName { get; set; }
        [Required] public string OutputPath { get; set; }
    }
}

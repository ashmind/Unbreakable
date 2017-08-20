using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Unbreakable.Build.PolicyReport {
    [LoadInSeparateAppDomain]
    public class CreateUnbreakablePolicyReport : Microsoft.Build.Utilities.AppDomainIsolatedTask {
        public override bool Execute() {
            var policy = GetPolicy();
            var assemblies = ReferencedAssemblyPaths.Select(Assembly.LoadFrom).ToDictionary(a => a.GetName().Name);
            SetupAssemblyResolution(assemblies);
            var namespaces = assemblies
                .Values
                .SelectMany(a => a.GetExportedTypes())
                .GroupBy(t => t.Namespace)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());

            using (var writer = new StreamWriter(OutputPath)) {
                foreach (var @namespace in namespaces.OrderBy(n => n.Key)) {
                    var namespacePolicy = GetItem(policy.Namespaces, @namespace.Key);
                    if (namespacePolicy == null)
                        continue;
                    var namespaceAccess = (string)namespacePolicy.Access.ToString();
                    writer.WriteLine(@namespace.Key);
                    if (namespaceAccess == "Denied")
                        continue;

                    foreach (var type in @namespace.Value.OrderBy(t => t.Name)) {
                        var typePolicy = GetItem(namespacePolicy.Types, type.Name);
                        var typeAccess = (string)typePolicy?.Access.ToString();
                        var effectiveTypeAccess = GetEffectiveTypeAccess(typeAccess, namespaceAccess);

                        writer.Write("  ");
                        writer.Write(type.Name);
                        writer.Write(": ");
                        writer.WriteLine(effectiveTypeAccess);

                        if (effectiveTypeAccess == "Denied")
                            continue;

                        foreach (var methodName in type.GetMethods().Select(m => m.Name).Distinct().OrderBy(n => n)) {
                            var methodPolicy = typePolicy != null ? GetItem(typePolicy.Members, methodName) : null;
                            var effectiveMethodAccess = GetEffectiveMethodAccess((string)methodPolicy?.Access.ToString(), typeAccess, effectiveTypeAccess);
                            writer.Write("     ");
                            writer.Write(methodName);
                            writer.Write(": ");
                            writer.WriteLine(effectiveMethodAccess);
                        }
                    }
                }
            }

            return true;
        }

        private void SetupAssemblyResolution(IReadOnlyDictionary<string, Assembly> assemblies) {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
                var name = new AssemblyName(e.Name);
                return assemblies.TryGetValue(name.Name, out var assembly) ? assembly : null;
            };
        }

        private dynamic GetPolicy() {
            var policyAssembly = Assembly.LoadFrom(PolicyAssemblyPath);
            var type = policyAssembly.GetType(PolicyTypeName, true);
            var method = type.GetMethod(PolicyMethodName);
            if (method == null)
                throw new Exception($"Method '{PolicyMethodName}' was not found in '{PolicyTypeName}'.");
            var policy = (dynamic)method.Invoke(null, null);
            return policy;
        }

        private dynamic GetItem(dynamic dictionary, string name) {
            return dictionary.ContainsKey(name) ? dictionary[name] : null;
        }

        private string GetEffectiveTypeAccess(string typeAccess, string namespaceAccess) {
            if (typeAccess == null)
                return namespaceAccess == "Allowed" ? "Allowed" : "Denied";

            if (typeAccess == "Neutral")
                return "Allowed";

            return typeAccess;
        }

        private string GetEffectiveMethodAccess(string methodAccess, string typeAccess, string effectiveTypeAccess) {
            if (methodAccess == null)
                return typeAccess == "Allowed" ? "Allowed" : "Denied";
            
            return methodAccess;
        }

        [Required] public string[] ReferencedAssemblyPaths { get; set; }
        [Required] public string PolicyAssemblyPath { get; set; }
        [Required] public string PolicyTypeName { get; set; }
        [Required] public string PolicyMethodName { get; set; }
        [Required] public string OutputPath { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Unbreakable.Tools.Analyzer {
    public static class Program {
        public static void Main(string[] args) {
            var assemblies = new[] {
                typeof(object).Assembly
            };
            var analysis = new Analysis();
            AddInterimTrustedMethods(analysis);
            analysis.Process(assemblies);

            var groupsByType = analysis.Results.Values
                .Where(
                    r => r.Method.IsPublic && !(r.Method.IsGenericMethod && !r.Method.IsGenericMethodDefinition)
                      && r.Method.DeclaringType.IsPublic && !(r.Method.DeclaringType.IsGenericType && !r.Method.DeclaringType.IsGenericTypeDefinition)
                )
                .GroupBy(m => m.Method.DeclaringType);
            var groupsByNamespace = groupsByType.GroupBy(g => g.Key.Namespace);

            var reportPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Report.txt");
            using (var writer = new StreamWriter(reportPath)) {
                foreach (var @namespace in groupsByNamespace.OrderBy(g => g.Key)) {
                    writer.WriteLine(@namespace.Key);
                    foreach (var type in @namespace.OrderBy(g => g.Key.Name)) {
                        writer.Write("  ");
                        writer.WriteLine(type.Key.Name);
                        foreach (var result in type.OrderBy(r => r.Method.Name)) {
                            writer.Write("    ");
                            writer.Write(result.Method.Name);
                            writer.Write(": ");
                            WriteResult(writer, result);
                        }
                    }
                }
            }
        }

        private static void AddInterimTrustedMethods(Analysis analysis) {
            // TODO: use policy instead
            analysis.RegisterTrusted(typeof(object).GetConstructors().Single());
            analysis.RegisterTrusted(typeof(string).GetMethod("get_Length"));
            analysis.RegisterTrusted(typeof(string).GetMethod("op_Equality"));
        }

        private static void WriteResult(StreamWriter writer, MethodDetails result) {
            const int StackPushCutoff = 16;

            if (result.IsTrusted) {
                writer.Write("Trusted");
            }
            if (result.IsInternalCall) {
                writer.Write("Internal Call");
            }
            else if(result.HasUnknownOpCodes) {
                writer.Write("Unknown");
            }
            else if (result.UntrustedCalls.Count > 0 || result.UntrustedFieldAccess.Count > 0) {
                writer.Write("Untrusted Access");
            }
            else if (result.HasLoops) {
                writer.Write("Loops?");
            }
            else if (result.HasDirectRecursion) {
                writer.Write("Recursion (Direct)");
            }
            else if (result.StackPushSize.Value > StackPushCutoff || result.StackPushSize.DependsOnGenericArguments) {
                writer.Write("Stack?");
            }
            else {
                writer.Write("Safe");
            }

            if (!result.HasDirectRecursion && !result.HasLoops)
                writer.Write(" (stack {0})", result.StackPushSize);
            writer.WriteLine();

            if (result.UntrustedCalls.Count > 0) {
                writer.WriteLine("      Untrusted Calls: ");
                foreach (var method in result.UntrustedCalls.OrderBy(c => c.Name)) {
                    writer.WriteLine("        {0}.{1}", method.DeclaringType.FullName, method.Name);
                }
            }

            if (result.UntrustedFieldAccess.Count > 0) {
                writer.WriteLine("      Untrusted Field Access: ");
                foreach (var access in result.UntrustedFieldAccess.OrderBy(p => p.Key.Name)) {
                    var field = access.Key;
                    writer.WriteLine("        {0}.{1}: {2}", field.DeclaringType.FullName, field.Name, access.Value);
                }
            }

            if (result.HasUnknownOpCodes) {
                writer.Write("      Unknown: ");
                writer.WriteLine(string.Join(", ", result.UnknownOpCodes.OrderBy(c => c.Name)));
            }
        }
    }
}

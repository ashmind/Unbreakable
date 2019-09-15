using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;
using Unbreakable.Runtime.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class RegexDangerousMethodCallRewriter : IMemberRewriterInternal {
        private const string MatchTimeoutParameterName = "matchTimeout";

        private static class KnownMethods {
            public static readonly MethodInfo GuardRegexOptions = typeof(RuntimeRegexGuard.FlowThrough)
                .GetMethod(nameof(RuntimeRegexGuard.FlowThrough.GuardOptions));
            public static readonly MethodInfo GetTimeUntilLimit = typeof(RuntimeGuard)
                .GetMethod(nameof(RuntimeGuard.GetTimeUntilLimit));
        }

        private static readonly Lazy<IReadOnlyDictionary<int, RegexMethodDetails>> MethodDetailsByMetadataToken = new Lazy<IReadOnlyDictionary<int, RegexMethodDetails>>(
            SlowBuildMethodDetailsByMetadataToken,
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public static RegexDangerousMethodCallRewriter Default { get; } = new RegexDangerousMethodCallRewriter();

        public string GetShortName() => nameof(RegexDangerousMethodCallRewriter);

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var method = ((MethodReference)instruction.Operand).Resolve();

            if (!MethodDetailsByMetadataToken.Value.TryGetValue(method.MetadataToken.ToInt32(), out var details))
                throw new AssemblyGuardException($"Regex method {method.Name} is not allowed.");

            if (details.ReasonIfNotAllowed != null)
                throw new AssemblyGuardException(details.ReasonIfNotAllowed);

            if (details.HasRegexOptionsAsLastParameter) {
                var guardRegexOptions = context.Module.ImportReference(KnownMethods.GuardRegexOptions);
                context.IL.InsertBeforeAndRetargetJumps(instruction, context.IL.CreateCall(guardRegexOptions));
            }

            if (!method.IsStatic && !method.IsConstructor)
                return true;

            if (details.MethodWithTimeout == null)
                throw new AssemblyGuardException($"Could not find a timeout variant for this overload of Regex.{method.Name}. Please try another overload.");

            if (details.MethodWithTimeoutNeedsRegexOptions) {
                // 0 = RegexOptions.None
                context.IL.InsertBeforeAndRetargetJumps(instruction, context.IL.Create(OpCodes.Ldc_I4_0));
            }

            var getTimeUntilLimit = context.Module.ImportReference(KnownMethods.GetTimeUntilLimit);
            context.IL.InsertBeforeAndRetargetJumps(instruction, context.IL.CreateLdlocBest(context.RuntimeGuardVariable));
            context.IL.InsertBefore(instruction, context.IL.CreateCall(getTimeUntilLimit));

            var importedTimeoutMethod = context.Module.ImportReference(details.MethodWithTimeout);
            instruction.Operand = importedTimeoutMethod;
            return true;
        }

        private static IReadOnlyDictionary<int, RegexMethodDetails> SlowBuildMethodDetailsByMetadataToken() {
            var methods = typeof(Regex)
                .GetMembers()
                .OfType<MethodBase>()
                .Where(m => !m.IsSpecialName || m.IsConstructor)
                .ToList();

            var result = new Dictionary<int, RegexMethodDetails>();
            foreach (var method in methods) {
                var parameters = method.GetParameters();

                string? reasonIfNotAllowed = null;
                if (parameters.Any(p => p.Name == MatchTimeoutParameterName))
                    reasonIfNotAllowed = $"Overload of Regex.{method.Name} taking {MatchTimeoutParameterName} is not allowed. Timeout will be provided automatically.";

                var indexOfOptions = Array.FindIndex(parameters, p => p.ParameterType.FullName == typeof(RegexOptions).FullName);
                var hasOptions = indexOfOptions >= 0;
                var optionsAreLast = hasOptions && (indexOfOptions == parameters.Length - 1);
                if (reasonIfNotAllowed == null && hasOptions && !optionsAreLast)
                    reasonIfNotAllowed = $"This overload of Regex.{method.Name} is not allowed, please try another one.";

                var methodWithTimeoutNeedsRegexOptions = false;
                var methodWithTimeout = reasonIfNotAllowed == null
                    ? FindSameMethodWithTimeout(method, parameters, optionsAreLast, methods, out methodWithTimeoutNeedsRegexOptions)
                    : null;

                result.Add(method.MetadataToken, new RegexMethodDetails(
                    methodWithTimeout: methodWithTimeout,
                    methodWithTimeoutNeedsRegexOptions: methodWithTimeoutNeedsRegexOptions,
                    hasRegexOptionsAsLastParameter: optionsAreLast,
                    reasonIfNotAllowed: reasonIfNotAllowed
                ));
            }
            return result;
        }

        private static MethodBase? FindSameMethodWithTimeout(
            MethodBase method,
            ParameterInfo[] parameters,
            bool hasRegexOptionsAsLastParameter,
            IReadOnlyCollection<MethodBase> methods,
            out bool needsRegexOptions
        ) {
            foreach (var other in methods) {
                if (other == method)
                    continue;

                if (other.IsStatic != method.IsStatic)
                    continue;

                if (other.Name != method.Name)
                    continue;

                var otherParameters = other.GetParameters();
                if (otherParameters.Length == 0)
                    continue;

                if (otherParameters.Last().Name != MatchTimeoutParameterName)
                    continue;

                if (!StartsWithParameters(otherParameters, parameters))
                    continue;

                // one extra parameter: timeout
                if (otherParameters.Length == parameters.Length + 1) {
                    needsRegexOptions = false;
                    return other;
                }

                // two extra parameters: regex options?, timeout
                if (!hasRegexOptionsAsLastParameter && otherParameters.Length == parameters.Length + 2) {
                    if (otherParameters[otherParameters.Length - 2].ParameterType != typeof(RegexOptions))
                        continue;
                    needsRegexOptions = true;
                    return other;
                }
            }
            needsRegexOptions = false;
            return null;
        }

        private static bool StartsWithParameters(ParameterInfo[] candidate, ParameterInfo[] parameters) {
            for (var i = 0; i < parameters.Length; i++) {
                if (candidate[i].Name != parameters[i].Name)
                    return false;
                if (candidate[i].ParameterType != parameters[i].ParameterType)
                    return false;
            }
            return true;
        }

        private class RegexMethodDetails {
            public RegexMethodDetails(
                MethodBase? methodWithTimeout,
                bool methodWithTimeoutNeedsRegexOptions,
                bool hasRegexOptionsAsLastParameter,
                string? reasonIfNotAllowed
            ) {
                MethodWithTimeout = methodWithTimeout;
                MethodWithTimeoutNeedsRegexOptions = methodWithTimeoutNeedsRegexOptions;
                HasRegexOptionsAsLastParameter = hasRegexOptionsAsLastParameter;
                ReasonIfNotAllowed = reasonIfNotAllowed;
            }

            public MethodBase? MethodWithTimeout { get; }
            public bool MethodWithTimeoutNeedsRegexOptions { get; }
            public bool HasRegexOptionsAsLastParameter { get; }
            public string? ReasonIfNotAllowed { get; }
        }
    }
}

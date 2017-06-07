namespace Unbreakable.Roslyn {
    public static class CSharpRoslynGuard {
        public static void Validate(string code, RoslynGuardSettings settings = null) {
            settings = settings ?? RoslynGuardSettings.Default;
            if (string.IsNullOrEmpty(code))
                return;

            // TODO: proper basic tokenizer
            // https://github.com/dotnet/roslyn/issues/20062
            var nestingLevel = 0;
            // https://github.com/dotnet/roslyn/issues/9795
            var dotCount = 0;
            foreach (var @char in code) {
                switch (@char) {
                    case '{': nestingLevel += 1; break;
                    case '}': nestingLevel -= 1; break;
                    case '(': nestingLevel += 1; break;
                    case ')': nestingLevel -= 1; break;
                    case '[': nestingLevel += 1; break;
                    case ']': nestingLevel -= 1; break;
                    case '.': dotCount += 1; break;
                }

                if (nestingLevel > settings.NestingLevelLimit)
                    throw new RoslynGuardException("Code nesting level is above the limit.");

                if (dotCount > settings.DotCountLimit)
                    throw new RoslynGuardException("Total count of '.' is above the limit.");
            }
        }
    }
}

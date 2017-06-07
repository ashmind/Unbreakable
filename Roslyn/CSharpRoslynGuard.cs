namespace Unbreakable.Roslyn {
    public static class CSharpRoslynGuard {
        public static void Validate(string code, RoslynGuardSettings settings = null) {
            settings = settings ?? RoslynGuardSettings.Default;
            if (string.IsNullOrEmpty(code))
                return;

            var nestingLevel = 0;
            foreach (var @char in code) {
                switch (@char) {
                    case '{': nestingLevel += 1; break;
                    case '}': nestingLevel -= 1; break;
                    case '(': nestingLevel += 1; break;
                    case ')': nestingLevel -= 1; break;
                    case '[': nestingLevel += 1; break;
                    case ']': nestingLevel -= 1; break;
                }

                if (nestingLevel > settings.NestingLevelLimit)
                    throw new RoslynGuardException("Code nesting level is above the limit.");
            }
        }
    }
}

namespace Unbreakable.Roslyn {
    public static class CSharpRoslynGuard {
        public static void Validate(string code, RoslynGuardSettings? settings = null) {
            settings = settings ?? RoslynGuardSettings.Default;
            if (string.IsNullOrEmpty(code))
                return;

            // TODO: proper basic tokenizer?
            // https://github.com/dotnet/roslyn/issues/20062
            var curlyBracketCount = 0;
            var roundBracketCount = 0;
            var squareBracketCount = 0;
            // https://github.com/dotnet/roslyn/issues/9795
            var dotCount = 0;
            foreach (var @char in code) {
                switch (@char) {
                    case '{':
                        curlyBracketCount += 1;
                        ValidateCount('{', curlyBracketCount, settings.OpenCurlyBracketCountLimit);
                        break;
                    case '(':
                        roundBracketCount += 1;
                        ValidateCount('(', roundBracketCount, settings.OpenRoundBracketCountLimit);
                        break;
                    case '[':
                        squareBracketCount += 1;
                        ValidateCount('[', squareBracketCount, settings.OpenSquareBracketCountLimit);
                        break;
                    case '.':
                        dotCount += 1;
                        ValidateCount('.', dotCount, settings.DotCountLimit);
                        break;
                }
            }
        }

        private static void ValidateCount(char @char, int count, int limit) {
            if (count > limit)
                throw new RoslynGuardException($"Total count of '{@char}' is above the limit.");
        }
    }
}

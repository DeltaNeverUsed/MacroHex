using System.Diagnostics;

namespace MacroHexCompiler;

public enum GeneratorType
{
    Num
}

public static class Generator
{
    public static List<Token> Generate(GeneratorType type, string[] args)
    {
        switch (type)
        {
            case GeneratorType.Num:
                if (float.TryParse(args[0], out float number))
                    return Number(number);
                throw new Exception("Invalid number");
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return [];
    }

    private static List<Token> Number(float number) {
        Stopwatch timer = Stopwatch.StartNew();
        (string result, bool exact, float diff) = NumericalReflection.NumberSolver.Solve(number, 12);
        timer.Stop();
        Compiler.VerbosePrint($"Number generation took {timer.Elapsed}");

        if (!exact) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: Generated number for {number} is not exact, generated number is {number - diff} with a difference of {diff}");
            Console.ResetColor();
        }
        
        return [new Token("number generator", 0, -1, result, TokenType.Pattern)];
    }
}
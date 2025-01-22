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
                if (double.TryParse(args[0], out double number))
                    return Number(number);
                else 
                    throw new Exception("Invalid number");
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return [];
    }

    private static List<Token> Number(double number)
    {


        long bits = BitConverter.DoubleToInt64Bits(number);

        // thank you john skeet: https://stackoverflow.com/a/390072

        bool negative = (bits & (1L << 63)) != 0;
        int  exponent = (int)((bits >> 52) & 0x7ffL);
        long mantissa = bits & 0xfffffffffffffL;



        mantissa |= 0x10000000000000L; // fractional part is between [1...2], so let's make it actually so
        exponent -= 1023; // exponent bias

        // precision adjustments
        int precision = 6;
        mantissa >>= 52 - precision;
        exponent -= 1 + precision;   // convert fractional to integral

        string pattern = negative ? "dedd" : "aqaa";

        // read mantissa into a stack so we can consume most significant bit first
        Stack<bool> bitStack = new(); // aqaawadawadadaadadadaadadadadaadadadadadaadadadadadadaadadadadadadadaa

        while (mantissa != 0)
        {
            // push least significant bit first
            bitStack.Push((mantissa & 1) != 0);
            mantissa >>= 1;
        }

        // we want the most significant bit to be our first '1' and shift it left 
        // (i.e. mult. by 2) for every lesser significant bit

        string padding = "da";
        while (bitStack.Count > 0)
        {
            // pop most significant bit first
            if (bitStack.Pop())
            {
                pattern += "w" /* +1 */;
            }
            pattern += "a" /* x2 */;

            pattern += padding;
            padding += "da";
        }
        // TODO: combine above loops

        // TODO: read exponent as integer and multiply/divide by 2 as needed
        char exponentSymbol = exponent < 0 ? 'd' /* /2 */ : 'a' /* x2 */;
        pattern += new string(exponentSymbol, Math.Abs(exponent));        

        return [new Token("number generator", 0, -1, pattern, TokenType.Pattern)];
    }
}
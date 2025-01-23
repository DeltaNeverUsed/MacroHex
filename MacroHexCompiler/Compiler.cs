using System.Text;

namespace MacroHexCompiler;

public static class Compiler {
    private static string _workingProgram = "";

    public static bool IncludeStd;
    public static bool MacroHexOutput;
    public static bool Verbose;
    
    private static string _sourcePath;
    private static string _outputPath;

    public static string StdPath => AppContext.BaseDirectory + "/std/";

    public static Dictionary<string, List<Token>> MacroDictionary = new();
    public static List<string> Included = [];
    
    public static void LogInstructions() {
        StringBuilder sb = new();
        Console.WriteLine( "Usage:");
        Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} [options..] source.macrohex [output.rawhex]\n");
        Console.WriteLine( "Options:");
        Console.WriteLine( "  --nostd               // Disables built-in Macros");
        Console.WriteLine( "  --verbose             // More logging");
        Console.WriteLine( "  --outputmacrohex      // outputs a valid macrohex file instead of rawhex **TODO**");
    }

    public static void PrintError(object msg) {
        ConsoleColor orgColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = orgColor;
    }

    public static void VerbosePrint(object msg) {
        if (Verbose)
            Console.WriteLine(msg);
    }

    private static bool ParseArgs(string[] args) {
        if (args.Length < 1) {
            LogInstructions();
            return false;
        }

        if (args.Length == 1 || args[^2].StartsWith("--")) {
            if (args[^1].StartsWith("--")) {
                PrintError("No input file\n");
                LogInstructions();
                return false;
            }

            _sourcePath = args[^1];
            if (!File.Exists(_sourcePath)) {
                PrintError($"source: {_sourcePath} doesn't exist\n");
                return false;
            }

            _outputPath = _sourcePath[.._sourcePath.LastIndexOf('.')] + ".rawhex";
        }
        else {
            _sourcePath = args[^2];
            _outputPath = args[^1];
            if (!File.Exists(_sourcePath)) {
                PrintError($"source: {_sourcePath} doesn't exist\n");
                return false;
            }
            
            if (!File.Exists(_outputPath)) {
                PrintError($"output: {_outputPath} doesn't exist\n");
                return false;
            }
        }

        IncludeStd     = !args.Contains("--nostd", StringComparer.OrdinalIgnoreCase);
        Verbose        = args.Contains( "--verbose", StringComparer.OrdinalIgnoreCase);
        MacroHexOutput = !args.Contains("--outputmacrohex", StringComparer.OrdinalIgnoreCase);

        return true;
    }
    
    private static void Main(string[] args) {
        if (!ParseArgs(args))
            return;
        
        _workingProgram = File.ReadAllText(_sourcePath);

        try {
            Parser parser = new(_sourcePath);
            List<Token> result = parser.Parse();
        
            File.WriteAllText(_outputPath, string.Join('\n', result.Select(t => t.Content.Trim())));
        }
        catch (Exception e) {
            PrintError(e);
        }
    }
}
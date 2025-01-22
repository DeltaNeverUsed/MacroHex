using System.Diagnostics;

namespace MacroHexCompiler;

public class Parser(string filePath) {
    public Stopwatch LifeTime = Stopwatch.StartNew();
    private TimeSpan _lastStep = TimeSpan.Zero;
    
    public string FilePath = filePath;
    public string FileName;
    
    public const int MaxIterationCount = 100;

    private List<Token> _tokens = [];
    private string _sourceText;

    private int _macroDepth;
    private int _column;
    private int _row;

    private void Add(string content, TokenType type) {
        _tokens.Add(new Token(FilePath, _row, _column, content, type));
    }
    
    private void AddRest(string line, TokenType type) {
        _tokens.Add(new Token(FilePath, _row, _column, GetRest(line), type));
    }
    
    private string GetRest(string line) {
        return line.Substring(_row, line.Length - _row);
    }

    private void AppendMacro(string content) {
        _tokens[^1].Content += content;
    }
    
    private void AttemptIncludeStd() {
        if (!Compiler.IncludeStd)
            return;

        if (!Directory.Exists(Compiler.StdPath)) {
            Compiler.VerbosePrint("warning: Couldn't Find std path");
            return;
        }
        
        foreach (string std in Directory.EnumerateFiles(Compiler.StdPath, "*.macrohex", SearchOption.AllDirectories)) {
            _tokens.Add(new Token(std, 0, -1, $"include {std}", TokenType.CompilerAction));
        }
    }

    private static string StripComments(string input) {
        int index = input.IndexOf("//", StringComparison.Ordinal);
        return index >= 0 ? input[..index] : input;
    }

    private void Throw(object msg) {
        throw new Exception($"Error! {FileName} {_column+1}, {_row+1}: {msg}");
    }
    private void Throw(Token token, object msg) {
        throw new Exception($"Error! {FileName} {token.Column+1}, {token.Row+1}: {msg}\n    -> {token.Content}");
    }

    private bool TokenizeNormal(string line) {
        switch (line[_row]) {
            case '#': // Compiler Action
                _row++;
                if (line.Length <= _row)
                    Throw("Expected something after #");
                AddRest(line, TokenType.CompilerAction);
                break;
            case '!': // Literal pattern
                _row++;
                if (line.Length <= _row)
                    Throw("Expected something after !");
                AddRest(line, TokenType.Pattern);
                break;
            case '<': // Creating a new Macro
                Add("", TokenType.MacroDefinition);
                _macroDepth++;
                return false;
            case '\t':
            case ' ':
                return false;
            default:
                AddRest(line, TokenType.MacroCall);
                return true;
        }

        return true;
    }

    private bool TokenizeMacro(string line) {
        switch (line[_row]) {
            case '<':
                AppendMacro("<");
                _macroDepth++;
                return false;
            case '>':
                _macroDepth--;
                if (_macroDepth == 0) return true;
                
                AppendMacro(">");
                return false;
            default:
                AppendMacro(line[_row].ToString()); // not super performant, but eh
                return false;
        }
        return true;
    }

    private void Tokenize(string source) {
        string[] lines = source.Split(
            ["\r\n", "\r", "\n"],
            StringSplitOptions.None
        );

        for (_column = 0; _column < lines.Length; _column++) {
            string line = StripComments(lines[_column]);
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            for (_row = 0; _row < line.Length; _row++) {
                bool complete;
                
                if (_macroDepth < 1)
                    complete = TokenizeNormal(line);
                else 
                    complete = TokenizeMacro(line);
                
                if (complete)
                    break;
            }
            
            if (_macroDepth > 0)
                AppendMacro("\n"); // make sure the newlines persist for macros
        }

        if (_macroDepth > 0)
            throw new Exception($"Macro in {FileName} never closed, macro started at {_column+1}, {_row+1}");
    }

    private void IncludePass() {
        for (int i = 0; i < _tokens.Count; i++) {
            Token token = _tokens[i];
            if (token.Type != TokenType.CompilerAction)
                continue;
            
            string content = token.Content;
            
            if (!content.StartsWith("include"))
                continue;

            string[] args = content.Split(' ', StringSplitOptions.TrimEntries);
            if (args.Length != 2)
                Throw(token, $"Includes require 1 arg, but got {args.Length}");

            string includePath = args[1];
            if (!File.Exists(includePath)) {
                int index = FilePath.LastIndexOf('/');
                includePath = (index >= 0 ? FilePath[..index] : FilePath) + "/" + includePath;
            }

            Parser includeParser = new(includePath);
            List<Token> result = includeParser.Parse(true);
            
            _tokens.RemoveAt(i);
            _tokens.InsertRange(i, result);
        }
    }

    private void MacroDefinitionPass() {
        for (int i = 0; i < _tokens.Count; i++) {
            Token token = _tokens[i];
            if (token.Type != TokenType.MacroDefinition)
                continue;

            string content = token.Content;
            
            int quoteCount = token.Content.Count(c => c == '"');
            
            if (quoteCount < 2)
                Throw(token, "Expected Macro but got none");
            
            if (content[0] != '"')
                Throw(token, $"Expected first \" but got {content[0]}");

            int nextQuote = content.IndexOf('"', 1);

            string name = content.Substring(1, nextQuote-1);
            string macroContent = content.Substring(nextQuote+1).TrimStart();
            
            if (Compiler.MacroDictionary.ContainsKey(name))
                Throw(token, $"macro already exists by name of {name}");

            string currentFile = FilePath; // save current file
            List<Token> currentTokens = _tokens; // save current tokens
            _tokens = [];
            FilePath = token.Origin;
            
            Tokenize(macroContent); // overwrites _tokens with the tokens of the macro
            Compiler.MacroDictionary.Add(name, _tokens);

            _tokens = currentTokens; // restore tokens
            FilePath = currentFile;
            
            _tokens.RemoveAt(i);
            i--;
        }
    }

    private void MacroCallPass() {
        for (int i = 0; i < _tokens.Count; i++) {
            Token token = _tokens[i];
            if (token.Type != TokenType.MacroCall)
                continue;

            string content = token.Content;
            string contentTrimmed = token.Content.Trim();
            if (!Compiler.MacroDictionary.ContainsKey(content)) {
                if (Compiler.MacroDictionary.ContainsKey(contentTrimmed)) {
                    content = contentTrimmed;
                } else {
                    Throw(token, $"Couldn't find macro \"{content}\"");
                }
            }

            _tokens.RemoveAt(i);
            _tokens.InsertRange(i, Compiler.MacroDictionary[content]);
        }
    }

    private void GeneratorPass() {
        for (int i = 0; i < _tokens.Count; i++) {
            Token token = _tokens[i];
            if (token.Type != TokenType.CompilerAction)
                continue;

            string content = token.Content;
            if (content.Length < 1)
                continue;
            
            if (content[0] != 'g')
                continue;

            string[] args = content[1..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length < 2)
                Throw(token, "Generator expected gen type and at least one argument.");

            if (!Enum.TryParse(args[0], true, out GeneratorType genType))
                Throw(token, $"Generator expected valid gen type but got {args[0]}");
            

            _tokens.RemoveAt(i);
            _tokens.InsertRange(i, Generator.Generate(genType, args[1..]));
        }
    }

    private void VerbosePrintTokenStats() {
        if (!Compiler.Verbose)
            return;
        Compiler.VerbosePrint($"{FileName} Token Stats:");
        Compiler.VerbosePrint($"    Total: {_tokens.Count}");
        Compiler.VerbosePrint($"    {TokenType.CompilerAction.ToString()}: {_tokens.Count(t => t.Type == TokenType.CompilerAction)}");
        Compiler.VerbosePrint($"    {TokenType.Pattern.ToString()}: {_tokens.Count(t => t.Type == TokenType.Pattern)}");
        Compiler.VerbosePrint($"    {TokenType.MacroDefinition.ToString()}: {_tokens.Count(t => t.Type == TokenType.MacroDefinition)}");
        Compiler.VerbosePrint($"    {TokenType.MacroCall.ToString()}: {_tokens.Count(t => t.Type == TokenType.MacroCall)}");
    }

    private string GetStepTime() {
        TimeSpan t = _lastStep;
        _lastStep = LifeTime.Elapsed;
        return (LifeTime.Elapsed - t).ToString();
    }

    public List<Token> Parse(bool includeOnly = false) {
        if (!File.Exists(FilePath)) {
            throw new Exception($"Could not find {FilePath}");
        }
        
        FileName = Path.GetFileName(Path.GetFullPath(FilePath));
        if (Compiler.Included.Contains(FileName))
            return [];
        Compiler.Included.Add(FileName);
        
        _sourceText = File.ReadAllText(FilePath);

        AttemptIncludeStd();
        Tokenize(_sourceText);
        
        Compiler.VerbosePrint($"Tokenization for {FileName} completed! took {GetStepTime()}");
        VerbosePrintTokenStats();
        
        IncludePass();
        Compiler.VerbosePrint($"IncludePass for {FileName} completed! took {GetStepTime()}");
        VerbosePrintTokenStats();
        
        if (includeOnly)
            goto earlyEnd;

        MacroDefinitionPass();
        Compiler.VerbosePrint($"MacroDefinitionPass for {FileName} completed! took {GetStepTime()}");
        VerbosePrintTokenStats();

        int iterCount = 0;
        while (_tokens.Any(t => t.Type is TokenType.MacroCall or TokenType.CompilerAction) || iterCount >= MaxIterationCount) {
            iterCount++;
            Compiler.VerbosePrint($"MacroCall/Generator Pass for {FileName} pass {iterCount}");

            MacroCallPass();
            GeneratorPass();
            VerbosePrintTokenStats();
        }
        
        Compiler.VerbosePrint($"MacroCall/Generator Passes for {FileName} completed! took {GetStepTime()}");

        
        earlyEnd:
        Compiler.VerbosePrint($"Parser for {FileName} took {LifeTime.Elapsed} total");
        
        LifeTime.Stop();
        return _tokens;
    }
}
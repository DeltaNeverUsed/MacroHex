namespace MacroHexCompiler;

public enum TokenType {
    CompilerAction,
    Pattern,
    MacroDefinition,
    MacroCall,
}

public class Token(string origin, int row, int column, string content, TokenType type) {
    public string Origin = origin;
    public int Row = row;
    public int Column = column;

    public string Content = content;
    public TokenType Type = type;
}
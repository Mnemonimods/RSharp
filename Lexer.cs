namespace RSharp;

public enum TokenKind
{
    FnKeyword,
    VoidKeyword,
    Int32Keyword,
    Int64Keyword,
    UInt32Keyword,
    UInt64Keyword,
    Identifier,
    LParen,
    RParen,
    LBrace,
    RBrace,
    LBracket,
    RBracket,
    Return,
    IntLiteral,
    Semicolon,
    Colon,
    Arrow,
    Plus,
    Minus,
    Equals,
    EndOfFile
}

public record Token(TokenKind Kind, string Text, int Position);

public class Lexer(string source)
{
    private readonly string _source = source;
    private int _pos = 0;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_pos < _source.Length)
        {
            char c = _source[_pos];

            if (char.IsWhiteSpace(c))
            {
                _pos++;
            }
            else if (char.IsLetter(c))
            {
                int start = _pos;

                while (_pos < _source.Length && char.IsLetterOrDigit(_source[_pos]))
                {
                    _pos++;
                }

                string word = _source[start.._pos];

                TokenKind kind = word switch
                {
                    "fn" => TokenKind.FnKeyword,
                    "void" => TokenKind.VoidKeyword,
                    "i32" => TokenKind.Int32Keyword,
                    "i64" => TokenKind.Int64Keyword,
                    "u32" => TokenKind.UInt32Keyword,
                    "u64" => TokenKind.UInt64Keyword,
                    "return" => TokenKind.Return,
                    _ => TokenKind.Identifier
                };

                tokens.Add(new Token(kind, word, start));
            }
            else if (char.IsDigit(c))
            {
                int start = _pos;

                while (_pos < _source.Length && char.IsDigit(_source[_pos]))
                {
                    _pos++;
                }

                string num = _source[start.._pos];

                tokens.Add(new Token(TokenKind.IntLiteral, num, start));
            }
            /* dead code for arrow */
            else if (c == '-' && _pos + 1 < _source.Length && _source[_pos + 1] == '>')
            {
                tokens.Add(new Token(TokenKind.Arrow, "->", _pos));
                _pos += 2;
            }
            else
            {
                TokenKind kind = c switch
                {
                    '(' => TokenKind.LParen,
                    ')' => TokenKind.RParen,
                    '{' => TokenKind.LBrace,
                    '}' => TokenKind.RBrace,
                    '[' => TokenKind.LBracket,
                    ']' => TokenKind.RBracket,
                    ';' => TokenKind.Semicolon,
                    ':' => TokenKind.Colon,
                    '+' => TokenKind.Plus,
                    '-' => TokenKind.Minus,
                    '=' => TokenKind.Equals,
                    _ => throw new NotImplementedException($"Unexpected character '{c}' at position {_pos}")
                };

                tokens.Add(new Token(kind, c.ToString(), _pos));
                _pos++;
            }
        }

        tokens.Add(new Token(TokenKind.EndOfFile, "", _pos));
        return tokens;
    }
}
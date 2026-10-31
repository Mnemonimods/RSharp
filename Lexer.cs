namespace RSharp;

public enum TokenKind
{
    Int32Keyword,
    Identifier,
    LParen,
    RParen,
    LBrace,
    RBrace,
    Return,
    IntLiteral,
    Semicolon,
    EndOfFile
}

public record Token(TokenKind Kind, string Text, int Position);

public class Lexer
{
    private readonly string _source;
    private int _pos;

    public Lexer(string source)
    {
        _source = source;
        _pos = 0;
    }

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

                string word = _source.Substring(start, _pos - start);

                TokenKind kind = word switch
                {
                    "i32" => TokenKind.Int32Keyword,
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

                string num = _source.Substring(start, _pos - start);

                tokens.Add(new Token(TokenKind.IntLiteral, num, start));
            }
            else
            {
                TokenKind kind = c switch
                {
                    '(' => TokenKind.LParen,
                    ')' => TokenKind.RParen,
                    '{' => TokenKind.LBrace,
                    '}' => TokenKind.RBrace,
                    ';' => TokenKind.Semicolon,
                    _ => throw new NotImplementedException()
                };

                tokens.Add(new Token(kind, c.ToString(), _pos));
                _pos++;
            }
        }

        tokens.Add(new Token(TokenKind.EndOfFile, "", _pos));
        return tokens;
    }
}
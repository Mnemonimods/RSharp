namespace RSharp;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _pos;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _pos = 0;
    }

    public List<FunctionDecl> ParseProgram()
    {
        var functions = new List<FunctionDecl>();

        while (_tokens[_pos].Kind != TokenKind.EndOfFile)
        {
            functions.Add(ParseFunction());
        }

        return functions;
    }

    public FunctionDecl ParseFunction()
    {
        Expect(TokenKind.FnKeyword);
        Token nameToken = Expect(TokenKind.Identifier);
        Expect(TokenKind.LParen);
        Expect(TokenKind.RParen);
        Expect(TokenKind.Colon); /* change to arrow if you want to */
        Token typeToken = ExpectType();
        Expect(TokenKind.LBrace);

        var statements = new List<Stmt>();
        while (_tokens[_pos].Kind != TokenKind.RBrace)
        {
            statements.Add(ParseStatement());
        }

        Expect(TokenKind.RBrace);

        return new FunctionDecl(typeToken.Text, nameToken.Text, statements);
    }

    private Stmt ParseStatement()
    {
        if (_tokens[_pos].Kind == TokenKind.Return)
        {
            _pos++;
            Expr value = ParseExpression();
            Expect(TokenKind.Semicolon);
            return new ReturnStmt(value);
        }

        if (_tokens[_pos].Kind == TokenKind.Identifier && _tokens[_pos + 1].Kind == TokenKind.Colon)
        {
            Token nameToken = Expect(TokenKind.Identifier);
            Expect(TokenKind.Colon);
            Token typeToken = ExpectType();
            Expect(TokenKind.Equals);
            Expr initializer = ParseExpression();
            Expect(TokenKind.Semicolon);
            return new VarDeclStmt(typeToken.Text, nameToken.Text, initializer);
        }

        if (_tokens[_pos].Kind == TokenKind.Identifier && _tokens[_pos + 1].Kind == TokenKind.Equals)
        {
            Token nameToken = Expect(TokenKind.Identifier);
            Expect(TokenKind.Equals);
            Expr value = ParseExpression();
            Expect(TokenKind.Semicolon);
            return new AssignStmt(nameToken.Text, value);
        }

        throw new Exception($"Unexpected token {_tokens[_pos].Kind} at position {_pos} while parsing a statement");
    }

    private Expr ParseExpression()
    {
        Expr left = ParsePrimary();

        while (_tokens[_pos].Kind is TokenKind.Plus or TokenKind.Minus)
        {
            string op = _tokens[_pos].Kind == TokenKind.Plus ? "+" : "-";
            _pos++;
            Expr right = ParsePrimary();
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    private Expr ParsePrimary()
    {
        if (_tokens[_pos].Kind == TokenKind.Minus)
        {
            _pos++;
            Token token = Expect(TokenKind.IntLiteral);
            return new IntLiteralExpr(-int.Parse(token.Text));
        }

        if (_tokens[_pos].Kind == TokenKind.Identifier)
        {
            Token token = Expect(TokenKind.Identifier);
            return new NameExpr(token.Text);
        }

        Token numberToken = Expect(TokenKind.IntLiteral);
        return new IntLiteralExpr(int.Parse(numberToken.Text));
    }

    private Token Expect(TokenKind kind)
    {
        if (_tokens[_pos].Kind != kind)
            throw new Exception($"Expected {kind} but got {_tokens[_pos].Kind} at position {_pos}");

        Token token = _tokens[_pos];
        _pos++;
        return token;
    }

    private Token ExpectType()
    {
        Token token = _tokens[_pos];
        bool isType = token.Kind is TokenKind.VoidKeyword or TokenKind.Int32Keyword
            or TokenKind.Int64Keyword or TokenKind.UInt32Keyword or TokenKind.UInt64Keyword;

        if (!isType)
            throw new Exception($"Expected a type but got {token.Kind} at position {_pos}");

        _pos++;
        return token;
    }
}
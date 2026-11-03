namespace RSharp;

public abstract record Expr;
public record IntLiteralExpr(int Value) : Expr;
public record BinaryExpr(Expr Left, string Operator, Expr Right) : Expr;
public record NameExpr(string Name) : Expr;

public abstract record Stmt;
public record VarDeclStmt(string Type, string Name, Expr Initializer) : Stmt;
public record AssignStmt(string Name, Expr Value) : Stmt;
public record ReturnStmt(Expr Value) : Stmt;

public record FunctionDecl(string ReturnType, string Name, List<Stmt> Body);
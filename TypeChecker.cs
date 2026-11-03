namespace RSharp;

public class TypeChecker
{
    public static void Check(List<FunctionDecl> functions)
    {
        var functionReturnTypes = new Dictionary<string, string>();
        foreach (var function in functions)
        {
            functionReturnTypes[function.Name] = function.ReturnType;
        }

        foreach (var function in functions)
        {
            var locals = new Dictionary<string, string>();
            foreach (var stmt in function.Body)
            {
                CheckStmt(stmt, locals, function.ReturnType, functionReturnTypes);
            }
        }
    }

    private static void CheckStmt(
        Stmt stmt, 
        Dictionary<string, string> locals, 
        string returnType,
        Dictionary<string, string> functionReturnTypes
    )
    {
        switch (stmt)
        {
            case VarDeclStmt decl:
                string initType = CheckExpr(decl.Initializer, locals, functionReturnTypes);

                if (initType != decl.Type)
                    throw new Exception(
                        $"Cannot assign value of type '{initType}' to variable '{decl.Name}' declared as '{decl.Type}'"
                    );

                locals[decl.Name] = decl.Type;
                break;

            case AssignStmt assign:
                if (!locals.TryGetValue(assign.Name, out string? declaredType))
                    throw new Exception($"unknown variable '{assign.Name}'");

                string valueType = CheckExpr(assign.Value, locals, functionReturnTypes);

                if (valueType != declaredType)
                    throw new Exception(
                        $"Cannot assign value of type '{valueType}' to variable '{assign.Name}' of type '{declaredType}'"
                    );

                break;

            case ReturnStmt ret:
                string retType = CheckExpr(ret.Value, locals, functionReturnTypes);

                if (retType != returnType)
                    throw new Exception(
                        $"Function declared to return '{returnType}' but returned '{retType}'"
                    );

                break;

            case IfStmt ifStmt:
                string condType = CheckExpr(ifStmt.Condition, locals, functionReturnTypes);

                if (condType != "bool")
                    throw new Exception($"'if' condition must be bool, got '{condType}'");

                foreach (var inner in ifStmt.Then)
                {
                    CheckStmt(inner, locals, returnType, functionReturnTypes);
                }

                break;

            case ExprStmt exprStmt:
                CheckExpr(exprStmt.Value, locals, functionReturnTypes);
                break;
        }
    }

    private static string CheckExpr(
        Expr expr, 
        Dictionary<string, string> locals,
        Dictionary<string, string> functionReturnTypes
    )
    {
        switch (expr)
        {
            case IntLiteralExpr:
                return "i32";

            case NameExpr name:
                if (!locals.TryGetValue(name.Name, out string? type))
                    throw new Exception($"Unknown variable '{name.Name}'");
                return type;

            case BinaryExpr bin:
                string leftType = CheckExpr(bin.Left, locals, functionReturnTypes);
                string rightType = CheckExpr(bin.Right, locals, functionReturnTypes);

                if (leftType != rightType)
                    throw new Exception(
                        $"Type mismatch: cannot apply '{bin.Operator}' to '{leftType}' and '{rightType}'");

                return bin.Operator == "==" ? "bool" : leftType;

            case CallExpr call:
                if (call.Name == "print")
                {
                    if (call.Args.Count != 1)
                        throw new Exception("'print' expects exactly one argument");

                    string argType = CheckExpr(call.Args[0], locals, functionReturnTypes);
                    if (argType != "i32")
                        throw new Exception($"'print' expects an i32 argument, got '{argType}'");

                    return "void";
                }

                if (call.Args.Count != 0)
                    throw new Exception($"Function '{call.Name}' does not take arguments yet");

                if (!functionReturnTypes.TryGetValue(call.Name, out string? calleeReturnType))
                    throw new Exception($"Unknown function '{call.Name}'");

                return calleeReturnType;
            
            default:
                throw new NotImplementedException();
        }
    }
}
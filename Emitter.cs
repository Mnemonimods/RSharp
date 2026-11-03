using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace RSharp;

public class Emitter
{
    public static int EmitAndRun(List<FunctionDecl> functions)
    {
        var assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName("RSharpOutput"),
            typeof(object).Assembly);

        ModuleBuilder module = assemblyBuilder.DefineDynamicModule("RSharpModule");
        TypeBuilder type = module.DefineType("RSharpProgram", TypeAttributes.Public | TypeAttributes.Class);

        // method's signature(s)
        var methodTable = new Dictionary<string, MethodBuilder>();

        foreach (var function in functions)
        {
            Type returnType = ResolveType(function.ReturnType);

            MethodBuilder method = type.DefineMethod(
                function.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                returnType,
                Type.EmptyTypes);

            methodTable[function.Name] = method;
        }

        foreach (var function in functions)
        {
            MethodBuilder method = methodTable[function.Name];
            ILGenerator il = method.GetILGenerator();
            var locals = new Dictionary<string, LocalBuilder>();

            foreach (var stmt in function.Body)
            {
                EmitStmt(il, stmt, locals, methodTable);
            }

            if (method.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ret);
            }
        }

        type.CreateType();

        using var stream = new MemoryStream();
        assemblyBuilder.Save(stream);
        File.WriteAllBytes("output.dll", stream.ToArray());
        stream.Position = 0;

        Assembly loaded = AssemblyLoadContext.Default.LoadFromStream(stream);
        Type? loadedType = loaded.GetType("RSharpProgram");
        MethodInfo mainMethod = loadedType!.GetMethod("main")
            ?? throw new Exception("No 'main' function found.");

        if (mainMethod.ReturnType != typeof(int))
            throw new Exception($"'main' must return i32 for now (found {mainMethod.ReturnType}).");

        return (int)mainMethod.Invoke(null, null)!;
    }

    private static Type ResolveType(string typeName) => typeName switch
    {
        "void" => typeof(void),
        "bool" => typeof(System.Boolean),
        "i8" => typeof(System.SByte),
        "i16" => typeof(System.Int16),
        "i32" => typeof(System.Int32),
        "i64" => typeof(System.Int64),
        "i128" => typeof(System.Int128),
        "isize" => typeof(System.IntPtr),
        "u8" => typeof(System.Byte),
        "u16" => typeof(System.UInt16),
        "u32" => typeof(System.UInt32),
        "u64" => typeof(System.UInt64),
        "u128" => typeof(System.UInt128),
        "usize" => typeof(System.UIntPtr),
        "f16" => typeof(System.Half),
        "f32" => typeof(System.Single),
        "f64" => typeof(System.Double),
        "char8" => typeof(System.Byte),
        "char16" => typeof(System.Char),
        "object" => typeof(System.Object),
        "string" => typeof(System.String),
        _ => throw new Exception($"Unknown type '{typeName}'")
    };

    private static void EmitStmt(ILGenerator il, Stmt stmt,
        Dictionary<string, LocalBuilder> locals, Dictionary<string, MethodBuilder> methodTable)
    {
        switch (stmt)
        {
            case VarDeclStmt decl:
                LocalBuilder local = il.DeclareLocal(ResolveType(decl.Type));
                locals[decl.Name] = local;
                EmitExpr(il, decl.Initializer, locals, methodTable);
                il.Emit(OpCodes.Stloc, local);
                break;

            case AssignStmt assign:
                if (!locals.TryGetValue(assign.Name, out LocalBuilder? target))
                    throw new Exception($"Unknown variable '{assign.Name}'");
                EmitExpr(il, assign.Value, locals, methodTable);
                il.Emit(OpCodes.Stloc, target);
                break;

            case ReturnStmt ret:
                EmitExpr(il, ret.Value, locals, methodTable);
                il.Emit(OpCodes.Ret);
                break;

            case IfStmt ifStmt:
                var label = il.DefineLabel();
                EmitExpr(il, ifStmt.Condition, locals, methodTable);
                il.Emit(OpCodes.Brfalse, label);

                foreach (var inner in ifStmt.Then)
                {
                    EmitStmt(il, inner, locals, methodTable);
                }

                il.MarkLabel(label);
                break;

            case ExprStmt exprStmt:
                EmitExpr(il, exprStmt.Value, locals, methodTable);

                if (exprStmt.Value is CallExpr call && call.Name != "print"
                    && methodTable[call.Name].ReturnType != typeof(void))
                {
                    il.Emit(OpCodes.Pop);
                }
                break;
        }
    }

    private static void EmitExpr(ILGenerator il, Expr expr,
        Dictionary<string, LocalBuilder> locals, Dictionary<string, MethodBuilder> methodTable)
    {
        switch (expr)
        {
            case IntLiteralExpr lit:
                il.Emit(OpCodes.Ldc_I4, lit.Value);
                break;

            case NameExpr name:
                if (!locals.TryGetValue(name.Name, out LocalBuilder? local))
                    throw new Exception($"Unknown variable '{name.Name}'");
                il.Emit(OpCodes.Ldloc, local);
                break;

            case BinaryExpr bin:
                EmitExpr(il, bin.Left, locals, methodTable);
                EmitExpr(il, bin.Right, locals, methodTable);

                OpCode opcode = bin.Operator switch
                {
                    "+" => OpCodes.Add,
                    "-" => OpCodes.Sub,
                    "==" => OpCodes.Ceq,
                    _ => throw new NotImplementedException()
                };

                il.Emit(opcode);
                break;

            case CallExpr call:
                if (call.Name == "print")
                {
                    EmitExpr(il, call.Args[0], locals, methodTable);
                    MethodInfo writeLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) })!;
                    il.Emit(OpCodes.Call, writeLine);
                    break;
                }

                if (!methodTable.TryGetValue(call.Name, out MethodBuilder? callee))
                    throw new Exception($"Unknown function '{call.Name}'");
                il.Emit(OpCodes.Call, callee);
                break;
        }
    }
}
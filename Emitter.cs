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

        foreach (var function in functions)
        {
            Type returnType = ResolveType(function.ReturnType);

            MethodBuilder method = type.DefineMethod(
                function.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                returnType,
                Type.EmptyTypes);

            ILGenerator il = method.GetILGenerator();
            var locals = new Dictionary<string, LocalBuilder>();

            foreach (var stmt in function.Body)
            {
                EmitStmt(il, stmt, locals);
            }

            if (returnType == typeof(void))
            {
                // a void func with no explicit "return;" still needs a ret
                // harmless even if an earlier ReturnStmt already emitted one
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
        "i32" => typeof(int),
        "i64" => typeof(long),
        "u32" => typeof(uint),
        "u64" => typeof(ulong),
        _ => throw new Exception($"Unknown type '{typeName}'")
    };

    private static void EmitStmt(ILGenerator il, Stmt stmt, Dictionary<string, LocalBuilder> locals)
    {
        switch (stmt)
        {
            case VarDeclStmt decl:
                LocalBuilder local = il.DeclareLocal(ResolveType(decl.Type));
                locals[decl.Name] = local;
                EmitExpr(il, decl.Initializer, locals);
                il.Emit(OpCodes.Stloc, local);
                break;

            case AssignStmt assign:
                if (!locals.TryGetValue(assign.Name, out LocalBuilder? target))
                    throw new Exception($"Unknown variable '{assign.Name}'");
                EmitExpr(il, assign.Value, locals);
                il.Emit(OpCodes.Stloc, target);
                break;

            case ReturnStmt ret:
                EmitExpr(il, ret.Value, locals);
                il.Emit(OpCodes.Ret);
                break;
        }
    }

    private static void EmitExpr(ILGenerator il, Expr expr, Dictionary<string, LocalBuilder> locals)
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
                EmitExpr(il, bin.Left, locals);
                EmitExpr(il, bin.Right, locals);
                il.Emit(bin.Operator == "+" ? OpCodes.Add : OpCodes.Sub);
                break;
        }
    }
}
using RSharp;

bool debug = Environment.GetEnvironmentVariable("DEBUG") == "1";

string source = File.ReadAllText("input.rsx");

var tokens = new Lexer(source).Tokenize();
if (debug)
{
    foreach (var t in tokens)
        Console.WriteLine(t);
}

var functions = new Parser(tokens).ParseProgram();
if (debug)
{
    foreach (var fn in functions)
        Console.WriteLine($"Parsed: fn {fn.Name} -> {fn.ReturnType} ({fn.Body.Count} statement(s))");
}

TypeChecker.Check(functions);

int result = Emitter.EmitAndRun(functions);

Console.WriteLine($"Exit code: {result}");
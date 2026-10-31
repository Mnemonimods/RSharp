using RSharp;

string source = File.ReadAllText("hello.rsx");
var lexer = new Lexer(source);
var tokens = lexer.Tokenize();

foreach (var t in tokens)
    Console.WriteLine(t);
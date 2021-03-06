using System;
using System.Collections.Generic;
using System.IO;

public class GenerateAst
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: generate_ast <output directory>");
            System.Environment.Exit(64);
        }

        string outputDir = args[0];

        DefineAst(outputDir, "Expr", new List<string> {
            "Assign : Token name, Expr value",
            "Binary : Expr left, Token operation, Expr right",
            "Call : Expr callee, Token paren, List<Expr> arguments",
            "Get : Expr value, Token name",
            "Grouping : Expr expression",
            "Literal : object value",
            "Logical : Expr left, Token operation, Expr right",
            "Set : Expr expression, Token name, Expr value",
            "Super : Token keyword, Token method",
            "This : Token keyword",
            "Unary : Token operation, Expr right",
            "Variable : Token name"
        });

        DefineAst(outputDir, "Stmt", new List<string> {
            "Block : List<Stmt> statements",
            "Class : Token name, Expr.Variable superclass,"
                + " List<Stmt.Function> methods",
            "Expression : Expr value",
            "Function : Token name, List<Token> parameters, List<Stmt> body",
            "If : Expr condition, Stmt thenBranch, Stmt elseBranch",
            "Print : Expr value",
            "Return : Token keyword, Expr value",
            "Var : Token name, Expr initializer",
            "While : Expr condition, Stmt body"
        });
    }

    private static void DefineAst(
        string outputDir, string baseName, List<string> types)
    {
        string path = $"{outputDir}/{baseName}.cs";
        StreamWriter writer = new StreamWriter(path);

        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();
        writer.WriteLine($"public abstract class {baseName}");
        writer.WriteLine("{");

        DefineVisitor(writer, baseName, types);

        foreach (string type in types)
        {
            string className = type.Split(":")[0].Trim();
            string fields = type.Split(":")[1].Trim();

            DefineType(writer, baseName, className, fields);
        }

        writer.WriteLine(
            "    public abstract R Accept<R>(Visitor<R> visitor);");
        writer.WriteLine("}");
        writer.Close();
    }

    private static void DefineVisitor(
        TextWriter writer, string baseName, List<String> types)
    {
        writer.WriteLine("    public interface Visitor<R>");
        writer.WriteLine("    {");

        foreach (string type in types)
        {
            string typeName = type.Split(":")[0].Trim();
            string valName = baseName.ToLower();

            writer.WriteLine(
                $"        R Visit{typeName}{baseName}({typeName} {valName});");
        }

        writer.WriteLine("    }");
        writer.WriteLine();
    }

    private static void DefineType(
        TextWriter writer, string baseName,
        string className, string fieldList)
    {
        writer.WriteLine($"    public class {className} : {baseName}");
        writer.WriteLine("    {");
        writer.WriteLine($"        public {className}({fieldList})");
        writer.WriteLine("        {");

        string[] fields = fieldList.Split(", ");

        foreach (string field in fields)
        {
            string name = field.Split(" ")[1];
            writer.WriteLine($"            {ToTitleCase(name)} = {name};");
        }

        writer.WriteLine("        }");
        writer.WriteLine();

        writer.WriteLine(
            "        public override R Accept<R>(Visitor<R> visitor)");
        writer.WriteLine("        {");
        writer.WriteLine(
            $"            return visitor.Visit{className}{baseName}(this);");
        writer.WriteLine("        }");
        writer.WriteLine();

        foreach (string field in fields)
        {
            string[] parts = field.Split(" ");
            string type = parts[0];
            string name = ToTitleCase(parts[1]);

            writer.WriteLine($"        public readonly {type} {name};");
        }

        writer.WriteLine("    }");
        writer.WriteLine();
    }

    private static string ToTitleCase(string text)
    {
        return $"{char.ToUpper(text[0])}{text.Substring(1)}";
    }
}

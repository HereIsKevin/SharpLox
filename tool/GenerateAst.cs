using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class GenerateAst
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: generate_ast <output directory>");
            Environment.Exit(64);
        }

        string outputDir = args[0];

        DefineAst(outputDir, "Expr", new List<string> {
            "Binary : Expr left, Token operation, Expr right",
            "Grouping : Expr expression",
            "Literal : object value",
            "Unary : Token operation, Expr right"
        });
    }

    private static void DefineAst(
        string outputDir, string baseName, List<string> types)
    {
        string path = $"{outputDir}/{baseName}.cs";
        StreamWriter writer = new StreamWriter(path);

        writer.WriteLine($"public abstract class {baseName}");
        writer.WriteLine("{");

        DefineVisitor(writer, baseName, types);

        foreach (string type in types)
        {
            string className = type.Split(":")[0].Trim();
            string fields = type.Split(":")[1].Trim();

            DefineType(writer, baseName, className, fields);
        }

        writer.WriteLine("    public abstract R Accept<R>(Visitor<R> visitor);");
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
            string valueName = baseName.ToLower();

            writer.WriteLine($"        R Visit{typeName}{baseName}({typeName} {valueName});");
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

        writer.WriteLine("        public override R Accept<R>(Visitor<R> visitor)");
        writer.WriteLine("        {");
        writer.WriteLine($"            return visitor.Visit{className}{baseName}(this);");
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
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }
}

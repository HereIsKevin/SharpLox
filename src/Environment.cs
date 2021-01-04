using System.Collections.Generic;

public class Environment
{
    public readonly Environment Enclosing;
    private readonly Dictionary<string, object> Values =
        new Dictionary<string, object>();

    public Environment()
    {
        Enclosing = null;
    }

    public Environment(Environment enclosing)
    {
        Enclosing = enclosing;
    }

    public object Get(Token name)
    {
        if (Values.ContainsKey(name.Lexeme))
        {
            return Values[name.Lexeme];
        }

        if (Enclosing != null)
        {
            return Enclosing.Get(name);
        }

        throw new RuntimeException(
            name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object value)
    {
        if (Values.ContainsKey(name.Lexeme))
        {
            Values[name.Lexeme] = value;
            return;
        }

        if (Enclosing != null)
        {
            Enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeException(
            name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Define(string name, object value)
    {
        Values[name] = value;
    }
}

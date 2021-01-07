using System.Collections.Generic;

public class LoxInstance
{
    private LoxClass Klass;
    private readonly Dictionary<string, object> Fields =
        new Dictionary<string, object>();

    public LoxInstance(LoxClass klass)
    {
        Klass = klass;
    }

    public object Get(Token name)
    {
        object value;

        if (Fields.TryGetValue(name.Lexeme, out value))
        {
            return value;
        }

        LoxFunction method = Klass.FindMethod(name.Lexeme);

        if (method != null)
        {
            return method.Bind(this);
        }

        throw new RuntimeException(name,
            $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object value)
    {
        Fields[name.Lexeme] = value;
    }

    public override string ToString()
    {
        return $"{Klass.Name} instance";
    }
}

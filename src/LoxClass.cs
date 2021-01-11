using System.Collections.Generic;

public class LoxClass : LoxCallable
{
    public readonly string Name;
    public readonly LoxClass Superclass;
    private readonly Dictionary<string, LoxFunction> Methods;

    public LoxClass(string name, LoxClass superclass,
        Dictionary<string, LoxFunction> methods)
    {
        Superclass = superclass;
        Name = name;
        Methods = methods;
    }

    public LoxFunction FindMethod(string name)
    {
        LoxFunction method;

        if (Methods.TryGetValue(name, out method))
        {
            return method;
        }

        if (Superclass != null)
        {
            return Superclass.FindMethod(name);
        }

        return null;
    }

    public override string ToString()
    {
        return Name;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        LoxInstance instance = new LoxInstance(this);
        LoxFunction initializer = FindMethod("init");

        if (initializer != null)
        {
            initializer.Bind(instance).Call(interpreter, arguments);
        }

        return instance;
    }

    public int Arity()
    {
        LoxFunction initializer = FindMethod("init");

        if (initializer == null)
        {
            return 0;
        }

        return initializer.Arity();
    }
}

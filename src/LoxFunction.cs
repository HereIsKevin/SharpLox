using System.Collections.Generic;

public class LoxFunction : LoxCallable
{
    private readonly Stmt.Function Declaration;
    private readonly Environment Closure;
    private readonly bool IsInitializer;

    public LoxFunction(Stmt.Function declaration, Environment closure,
        bool isInitializer)
    {
        IsInitializer = isInitializer;
        Declaration = declaration;
        Closure = closure;
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        Environment environment = new Environment(Closure);
        environment.Define("this", instance);
        return new LoxFunction(Declaration, environment, IsInitializer);
    }

    public override string ToString()
    {
        return $"<fn {Declaration.Name.Lexeme}>";
    }

    public int Arity()
    {
        return Declaration.Parameters.Count;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        Environment environment = new Environment(Closure);

        for (int i = 0; i < Declaration.Parameters.Count; i++)
        {
            environment.Define(Declaration.Parameters[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(Declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            if (IsInitializer)
            {
                return Closure.GetAt(0, "this");
            }

            return returnValue.Value;
        }

        if (IsInitializer)
        {
            return Closure.GetAt(0, "this");
        }

        return null;
    }
}

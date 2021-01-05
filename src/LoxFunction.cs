using System.Collections.Generic;

public class LoxFunction : LoxCallable
{
    private readonly Stmt.Function Declaration;
    private readonly Environment Closure;

    public LoxFunction(Stmt.Function declaration, Environment closure)
    {
        Declaration = declaration;
        Closure = closure;
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
            return returnValue.Value;
        }

        return null;
    }
}

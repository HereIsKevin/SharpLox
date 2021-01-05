using System.Collections.Generic;

interface LoxCallable
{
    int Arity();
    object Call(Interpreter interpreter, List<object> arguments);
}

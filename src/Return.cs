using System;

public class Return : Exception
{
    public readonly object Value;

    public Return(object value) : base()
    {
        Value = value;
    }
}

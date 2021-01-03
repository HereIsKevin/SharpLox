public abstract class Expr
{
    public interface Visitor<R>
    {
        R VisitBinaryExpr(Binary expr);
        R VisitGroupingExpr(Grouping expr);
        R VisitLiteralExpr(Literal expr);
        R VisitUnaryExpr(Unary expr);
    }

    public class Binary : Expr
    {
        public Binary(Expr left, Token operation, Expr right)
        {
            Left = left;
            Operation = operation;
            Right = right;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitBinaryExpr(this);
        }

        public readonly Expr Left;
        public readonly Token Operation;
        public readonly Expr Right;
    }

    public class Grouping : Expr
    {
        public Grouping(Expr expression)
        {
            Expression = expression;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitGroupingExpr(this);
        }

        public readonly Expr Expression;
    }

    public class Literal : Expr
    {
        public Literal(object value)
        {
            Value = value;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitLiteralExpr(this);
        }

        public readonly object Value;
    }

    public class Unary : Expr
    {
        public Unary(Token operation, Expr right)
        {
            Operation = operation;
            Right = right;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitUnaryExpr(this);
        }

        public readonly Token Operation;
        public readonly Expr Right;
    }

    public abstract R Accept<R>(Visitor<R> visitor);
}

using System.Collections.Generic;

public abstract class Stmt
{
    public interface Visitor<R>
    {
        R VisitBlockStmt(Block stmt);
        R VisitExpressionStmt(Expression stmt);
        R VisitPrintStmt(Print stmt);
        R VisitVarStmt(Var stmt);
    }

    public class Block : Stmt
    {
        public Block(List<Stmt> statements)
        {
            Statements = statements;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }

        public readonly List<Stmt> Statements;
    }

    public class Expression : Stmt
    {
        public Expression(Expr value)
        {
            Value = value;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }

        public readonly Expr Value;
    }

    public class Print : Stmt
    {
        public Print(Expr value)
        {
            Value = value;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }

        public readonly Expr Value;
    }

    public class Var : Stmt
    {
        public Var(Token name, Expr initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitVarStmt(this);
        }

        public readonly Token Name;
        public readonly Expr Initializer;
    }

    public abstract R Accept<R>(Visitor<R> visitor);
}

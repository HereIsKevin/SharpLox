using System.Collections.Generic;

public abstract class Stmt
{
    public interface Visitor<R>
    {
        R VisitBlockStmt(Block stmt);
        R VisitExpressionStmt(Expression stmt);
        R VisitIfStmt(If stmt);
        R VisitPrintStmt(Print stmt);
        R VisitVarStmt(Var stmt);
        R VisitWhileStmt(While stmt);
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

    public class If : Stmt
    {
        public If(Expr condition, Stmt thenBranch, Stmt elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitIfStmt(this);
        }

        public readonly Expr Condition;
        public readonly Stmt ThenBranch;
        public readonly Stmt ElseBranch;
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

    public class While : Stmt
    {
        public While(Expr condition, Stmt body)
        {
            Condition = condition;
            Body = body;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }

        public readonly Expr Condition;
        public readonly Stmt Body;
    }

    public abstract R Accept<R>(Visitor<R> visitor);
}

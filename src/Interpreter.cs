using System;
using System.Collections.Generic;

using static TokenType;

public class RuntimeException : Exception
{
    public readonly Token Token;

    public RuntimeException(Token token, string message) : base(message)
    {
        Token = token;
    }
}

public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
{
    private Environment Environment = new Environment();

    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeException error)
        {
            Lox.RuntimeError(error);
        }
    }

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Value);
        return null;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        object value = Evaluate(stmt.Value);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object VisitVarStmt(Stmt.Var stmt)
    {
        object value = null;

        if (stmt.Initializer != null)
        {
            value = Evaluate(stmt.Initializer);
        }

        Environment.Define(stmt.Name.Lexeme, value);
        return null;
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(Environment));
        return null;
    }

    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.Value);
        Environment.Assign(expr.Name, value);
        return value;
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        object right = Evaluate(expr.Right);

        switch (expr.Operation.Type)
        {
            case Bang:
                return !IsTruthy(right);
            case Minus:
                CheckNumberOperand(expr.Operation, right);
                return -(double)right;
        }

        return null;
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        return Environment.Get(expr.Name);
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        object left = Evaluate(expr.Left);
        object right = Evaluate(expr.Right);

        switch (expr.Operation.Type)
        {
            case Greater:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left > (double)right;
            case GreaterEqual:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left >= (double)right;
            case Less:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left < (double)right;
            case LessEqual:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left <= (double)right;
            case BangEqual:
                return !IsEqual(left, right);
            case EqualEqual:
                return IsEqual(left, right);
            case Minus:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left - (double)right;
            case Plus:
                if (left is double && right is double)
                {
                    return (double)left + (double)right;
                }

                if (left is string && right is string)
                {
                    return (string)left + (string)right;
                }

                throw new RuntimeException(expr.Operation,
                    "Operands must be two numbers or two strings.");
            case Slash:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left / (double)right;
            case Star:
                CheckNumberOperands(expr.Operation, left, right);
                return (double)left * (double)right;
        }

        return null;
    }

    private void CheckNumberOperand(Token operation, object operand)
    {
        if (operand is double)
        {
            return;
        }

        throw new RuntimeException(operation, "Operand must be a number.");
    }

    private void CheckNumberOperands(
        Token operation, object left, object right)
    {
        if (left is double && right is double)
        {
            return;
        }

        throw new RuntimeException(operation, "Operands must be numbers.");
    }

    private bool IsTruthy(object value)
    {
        if (value == null)
        {
            return false;
        }

        if (value is bool)
        {
            return (bool)value;
        }

        return true;
    }

    private bool IsEqual(object a, object b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null)
        {
            return false;
        }

        return a.Equals(b);
    }

    private string Stringify(object value)
    {
        if (value == null)
        {
            return "nil";
        }

        if (value is double)
        {
            string text = value.ToString();

            if (text.EndsWith(".0"))
            {
                text = text.Substring(0, text.Length - 2);
            }

            return text;
        }

        return value.ToString();
    }

    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = Environment;

        try
        {
            Environment = environment;

            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            Environment = previous;
        }
    }
}

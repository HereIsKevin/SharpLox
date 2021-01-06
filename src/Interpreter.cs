using System;
using System.Collections.Generic;

using static TokenType;

class ClockFunction : LoxCallable
{
    private static readonly DateTime Start =
        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public int Arity()
    {
        return 0;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        return (double)(DateTime.UtcNow - Start).TotalMilliseconds / 1000.0;
    }

    public override string ToString()
    {
        return "<native fn>";
    }
}

public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
{
    public readonly Environment Globals = new Environment();
    private Environment Environment;
    private readonly Dictionary<Expr, int> Locals = new Dictionary<Expr, int>();

    public Interpreter()
    {
        Globals.Define("clock", new ClockFunction());
        Environment = Globals;
    }

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

    public object VisitFunctionStmt(Stmt.Function stmt)
    {
        LoxFunction function = new LoxFunction(stmt, Environment);
        Environment.Define(stmt.Name.Lexeme, function);
        return null;
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch != null)
        {
            Execute(stmt.ElseBranch);
        }

        return null;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        object value = Evaluate(stmt.Value);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object VisitReturnStmt(Stmt.Return stmt)
    {
        object value = null;

        if (stmt.Value != null)
        {
            value = Evaluate(stmt.Value);
        }

        throw new Return(value);
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

    public object VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }

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
        int distance;

        if (Locals.TryGetValue(expr, out distance))
        {
            Environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            Globals.Assign(expr.Name, value);
        }

        return value;
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        object left = Evaluate(expr.Left);

        if (expr.Operation.Type == TokenType.Or)
        {
            if (IsTruthy(left))
            {
                return left;
            }
        }
        else
        {
            if (!IsTruthy(left))
            {
                return left;
            }
        }

        return Evaluate(expr.Right);
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.Callee);
        List<object> arguments = new List<object>();

        foreach (Expr argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (!(callee is LoxCallable))
        {
            throw new RuntimeException(
                expr.Paren, "Can only call functions and classes.");
        }

        LoxCallable function = (LoxCallable)callee;
        int arity = function.Arity();

        if (arguments.Count != arity)
        {
            throw new RuntimeException(expr.Paren,
                $"Expected {arity} arguments but got {arguments.Count}.");
        }

        return function.Call(this, arguments);
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
        return LookUpVariable(expr.Name, expr);
    }

    private object LookUpVariable(Token name, Expr expr)
    {
        int distance;

        if (Locals.TryGetValue(expr, out distance))
        {
            return Environment.GetAt(distance, name.Lexeme);
        }
        else
        {
            return Globals.Get(name);
        }
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

    public void ExecuteBlock(List<Stmt> statements, Environment environment)
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

    public void Resolve(Expr expr, int depth)
    {
        Locals[expr] = depth;
    }
}

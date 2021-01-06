using System.Collections.Generic;
using System.Linq;

public class Resolver : Expr.Visitor<object>, Stmt.Visitor<object>
{
    private readonly Interpreter Interpreter;
    private readonly Stack<Dictionary<string, bool>> Scopes =
        new Stack<Dictionary<string, bool>>();
    private FunctionType CurrentFunction = FunctionType.None;

    private enum FunctionType
    {
        None,
        Function
    }

    public Resolver(Interpreter interpreter)
    {
        Interpreter = interpreter;
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return null;
    }

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Value);
        return null;
    }

    public object VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);

        if (stmt.Initializer != null)
        {
            Resolve(stmt.Initializer);
        }

        Define(stmt.Name);
        return null;
    }

    public object VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return null;
    }

    public object VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);

        foreach (Expr argument in expr.Arguments)
        {
            Resolve(argument);
        }

        return null;
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
        return null;
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return null;
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return null;
    }

    public object VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionType.Function);
        return null;
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);

        if (stmt.ElseBranch != null)
        {
            Resolve(stmt.ElseBranch);
        }

        return null;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Value);
        return null;
    }

    public object VisitReturnStmt(Stmt.Return stmt)
    {
        if (CurrentFunction == FunctionType.None)
        {
            Lox.Error(stmt.Keyword, "Can't return from top-level code.");
        }

        if (stmt.Value != null)
        {
            Resolve(stmt.Value);
        }

        return null;
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        if (Scopes.Count != 0 &&
            Scopes.Peek().ContainsKey(expr.Name.Lexeme) &&
            Scopes.Peek()[expr.Name.Lexeme] == false)
        {
            Lox.Error(expr.Name,
                "Can't read local variable in its own initializer");
        }

        ResolveLocal(expr, expr.Name);
        return null;
    }

    public void Resolve(List<Stmt> statements)
    {
        foreach (Stmt statement in statements)
        {
            Resolve(statement);
        }
    }

    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    private void BeginScope()
    {
        Scopes.Push(new Dictionary<string, bool>());
    }

    private void EndScope()
    {
        Scopes.Pop();
    }

    private void Declare(Token name)
    {
        if (Scopes.Count == 0)
        {
            return;
        }

        Dictionary<string, bool> scope = Scopes.Peek();

        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(name, "Already variable with this name in this scope.");
        }

        scope[name.Lexeme] = false;
    }

    private void Define(Token name)
    {
        if (Scopes.Count == 0)
        {
            return;
        }

        Scopes.Peek()[name.Lexeme] = true;
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFunction = CurrentFunction;
        CurrentFunction = type;

        BeginScope();

        foreach (Token param in function.Parameters)
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.Body);
        EndScope();

        CurrentFunction = enclosingFunction;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = Scopes.Count - 1; i >= 0; i--)
        {
            if (Scopes.ElementAt(i).ContainsKey(name.Lexeme))
            {
                Interpreter.Resolve(expr, Scopes.Count - 1 - i);
                return;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;

public class Resolver : Expr.Visitor<object>, Stmt.Visitor<object>
{
    private readonly Interpreter Interpreter;
    private readonly Stack<Dictionary<string, bool>> Scopes =
        new Stack<Dictionary<string, bool>>();
    private FunctionType CurrentFunction = FunctionType.None;
    private ClassType CurrentClass = ClassType.None;

    private enum FunctionType
    {
        None,
        Function,
        Method,
        Initializer
    }

    private enum ClassType
    {
        None,
        Class,
        Subclass
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

    public object VisitClassStmt(Stmt.Class stmt)
    {
        ClassType enclosingClass = CurrentClass;
        CurrentClass = ClassType.Class;

        Declare(stmt.Name);
        Define(stmt.Name);

        if (stmt.Superclass != null &&
            stmt.Name.Lexeme.Equals(stmt.Superclass.Name.Lexeme))
        {
            Lox.Error(stmt.Superclass.Name,
                "A class can't inherit from itself.");
        }

        if (stmt.Superclass != null)
        {
            CurrentClass = ClassType.Subclass;
            Resolve(stmt.Superclass);
        }

        if (stmt.Superclass != null)
        {
            BeginScope();
            Scopes.Peek()["super"] = true;
        }

        BeginScope();
        Scopes.Peek()["this"] = true;

        foreach (Stmt.Function method in stmt.Methods)
        {
            FunctionType declaration = FunctionType.Method;

            if (method.Name.Lexeme.Equals("init"))
            {
                declaration = FunctionType.Initializer;
            }

            ResolveFunction(method, declaration);
        }

        EndScope();

        if (stmt.Superclass != null)
        {
            EndScope();
        }

        CurrentClass = enclosingClass;
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

    public object VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Value);
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

    public object VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Expression);
        return null;
    }

    public object VisitSuperExpr(Expr.Super expr)
    {
        if (CurrentClass == ClassType.None)
        {
            Lox.Error(expr.Keyword, "Can't use 'super' outside of a class.");
        }
        else if (CurrentClass != ClassType.Subclass)
        {
            Lox.Error(expr.Keyword,
                "Can't use 'super' in a class with no superclass.");
        }

        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object VisitThisExpr(Expr.This expr)
    {
        if (CurrentClass == ClassType.None)
        {
            Lox.Error(expr.Keyword, "Can't use 'this' outside of a class.");
            return null;
        }

        ResolveLocal(expr, expr.Keyword);
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
            if (CurrentFunction == FunctionType.Initializer)
            {
                Lox.Error(stmt.Keyword,
                    "Can't return a value from an initializer.");
            }

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
            if (Scopes.Reverse().ElementAt(i).ContainsKey(name.Lexeme))
            {
                Interpreter.Resolve(expr, Scopes.Count - 1 - i);
                return;
            }
        }
    }
}

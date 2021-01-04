using System;
using System.Collections.Generic;

using static TokenType;

public class Parser
{
    private class ParseException : Exception { }

    private readonly List<Token> Tokens;
    private int Current = 0;

    public Parser(List<Token> tokens)
    {
        Tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> statements = new List<Stmt>();

        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }

        return statements;
    }

    private Stmt Declaration()
    {
        try
        {
            if (Match(Var))
            {
                return VarDeclaration();
            }

            return Statement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt Statement()
    {
        if (Match(For))
        {
            return ForStatement();
        }

        if (Match(If))
        {
            return IfStatement();
        }

        if (Match(Print))
        {
            return PrintStatement();
        }

        if (Match(While))
        {
            return WhileStatement();
        }

        if (Match(LeftBrace))
        {
            return new Stmt.Block(Block());
        }

        return ExpressionStatement();
    }

    private Stmt ForStatement()
    {
        Consume(LeftParen, "Expect '(' after 'for'.");

        Stmt initializer;

        if (Match(Semicolon))
        {
            initializer = null;
        }
        else if (Match(Var))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr condition = null;

        if (!Check(Semicolon))
        {
            condition = Expression();
        }

        Consume(Semicolon, "Expect ';' after loop condition.");

        Expr increment = null;

        if (!Check(RightParen))
        {
            increment = Expression();
        }

        Consume(RightParen, "Expect ')' after for clauses.");

        Stmt body = Statement();

        if (increment != null)
        {
            body = new Stmt.Block(
                new List<Stmt> { body, new Stmt.Expression(increment) });
        }

        if (condition == null)
        {
            condition = new Expr.Literal(true);
        }

        body = new Stmt.While(condition, body);

        if (initializer != null)
        {
            body = new Stmt.Block(new List<Stmt> { initializer, body });
        }

        return body;
    }

    private Stmt IfStatement()
    {
        Consume(LeftParen, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(RightParen, "Expect ')' after if condition.");

        Stmt thenBranch = Statement();
        Stmt elseBranch = null;

        if (Match(Else))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt PrintStatement()
    {
        Expr value = Expression();
        Consume(Semicolon, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt VarDeclaration()
    {
        Token name = Consume(Identifier, "Expect variable name.");
        Expr initializer = null;

        if (Match(Equal))
        {
            initializer = Expression();
        }

        Consume(Semicolon, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    private Stmt WhileStatement()
    {
        Consume(LeftParen, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(RightParen, "Expect ')' after condition.");
        Stmt body = Statement();

        return new Stmt.While(condition, body);
    }

    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(Semicolon, "Expect ';' after expression");
        return new Stmt.Expression(expr);
    }

    private List<Stmt> Block()
    {
        List<Stmt> statements = new List<Stmt>();

        while (!Check(RightBrace) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Or();

        if (Match(Equal))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable)
            {
                Token name = ((Expr.Variable)expr).Name;
                return new Expr.Assign(name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();

        while (Match(TokenType.Or))
        {
            Token operation = Previous();
            Expr right = And();

            expr = new Expr.Logical(expr, operation, right);
        }

        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();

        while (Match(TokenType.And))
        {
            Token operation = Previous();
            Expr right = Equality();

            expr = new Expr.Logical(expr, operation, right);
        }

        return expr;
    }

    private Expr Equality()
    {
        Expr expr = Comparison();

        while (Match(BangEqual, EqualEqual))
        {
            Token operation = Previous();
            Expr right = Comparison();

            expr = new Expr.Binary(expr, operation, right);
        }

        return expr;
    }

    private Expr Comparison()
    {
        Expr expr = Term();

        while (Match(Greater, GreaterEqual, Less, LessEqual))
        {
            Token operation = Previous();
            Expr right = Term();

            expr = new Expr.Binary(expr, operation, right);
        }

        return expr;
    }

    private Expr Term()
    {
        Expr expr = Factor();

        while (Match(Minus, Plus))
        {
            Token operation = Previous();
            Expr right = Factor();

            expr = new Expr.Binary(expr, operation, right);
        }

        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(Slash, Star))
        {
            Token operation = Previous();
            Expr right = Unary();

            expr = new Expr.Binary(expr, operation, right);
        }

        return expr;
    }

    private Expr Unary()
    {
        if (Match(Bang, Minus))
        {
            Token operation = Previous();
            Expr right = Unary();

            return new Expr.Unary(operation, right);
        }

        return Primary();
    }

    private Expr Primary()
    {
        if (Match(False))
        {
            return new Expr.Literal(false);
        }

        if (Match(True))
        {
            return new Expr.Literal(true);
        }

        if (Match(Nil))
        {
            return new Expr.Literal(null);
        }

        if (Match(Number, TokenType.String))
        {
            return new Expr.Literal(Previous().Literal);
        }

        if (Match(Identifier))
        {
            return new Expr.Variable(Previous());
        }

        if (Match(LeftParen))
        {
            Expr expr = Expression();
            Consume(RightParen, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), message);
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd())
        {
            return false;
        }

        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            Current++;
        }

        return Previous();
    }

    private bool IsAtEnd()
    {
        return this.Peek().Type == EOF;
    }

    private Token Peek()
    {
        return Tokens[Current];
    }

    private Token Previous()
    {
        return Tokens[Current - 1];
    }

    private ParseException Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseException();
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Type == Semicolon)
            {
                return;
            }

            switch (Peek().Type)
            {
                case Class:
                case Fun:
                case Var:
                case For:
                case If:
                case While:
                case Print:
                case Return:
                    return;
            }

            Advance();
        }
    }
}

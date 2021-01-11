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
            if (Match(Class))
            {
                return ClassDeclaration();
            }

            if (Match(Fun))
            {
                return Function("function");
            }

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

    private Stmt ClassDeclaration()
    {
        Token name = Consume(Identifier, "Expect class name.");
        Expr.Variable superclass = null;

        if (Match(Less))
        {
            Consume(Identifier, "Expect superclass name.");
            superclass = new Expr.Variable(Previous());
        }

        Consume(LeftBrace, "Expect '{' before class body.");
        List<Stmt.Function> methods = new List<Stmt.Function>();

        while (!Check(RightBrace) && !IsAtEnd())
        {
            methods.Add(Function("method"));
        }

        Consume(RightBrace, "Expect '}' after class body.");
        return new Stmt.Class(name, superclass, methods);
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

        if (Match(TokenType.Return))
        {
            return ReturnStatement();
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

    private Stmt ReturnStatement()
    {
        Token keyword = Previous();
        Expr value = null;

        if (!Check(Semicolon))
        {
            value = Expression();
        }

        Consume(Semicolon, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
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

    private Stmt.Function Function(string kind)
    {
        Token name = Consume(Identifier, $"Expect {kind} name.");
        Consume(LeftParen, $"Expect '(' after {kind} name.");
        List<Token> parameters = new List<Token>();

        if (!Check(RightParen))
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 parameters.");
                }

                parameters.Add(Consume(Identifier, "Expect parameter name."));
            } while (Match(Comma));
        }

        Consume(RightParen, "Expect ')' after parameters.");
        Consume(LeftBrace, $"Expect '{{' before {kind} body.");
        List<Stmt> body = Block();
        return new Stmt.Function(name, parameters, body);
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
            else if (expr is Expr.Get)
            {
                Expr.Get get = (Expr.Get)expr;
                return new Expr.Set(get.Value, get.Name, value);
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

        return Call();
    }

    private Expr FinishCall(Expr callee)
    {
        List<Expr> arguments = new List<Expr>();

        if (!Check(RightParen))
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 arguments.");
                }

                arguments.Add(Expression());
            } while (Match(Comma));
        }

        Token paren = Consume(RightParen, "Expect ')' after arguments.");

        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Call()
    {
        Expr expr = Primary();

        while (true)
        {
            if (Match(LeftParen))
            {
                expr = FinishCall(expr);
            }
            else if (Match(Dot))
            {
                Token name = Consume(Identifier, "Expect property after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else
            {
                break;
            }
        }

        return expr;
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

        if (Match(Super))
        {
            Token keyword = Previous();
            Consume(Dot, "Expect '.' after 'super'.");
            Token method = Consume(Identifier,
                "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        }

        if (Match(This))
        {
            return new Expr.This(Previous());
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
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }
}

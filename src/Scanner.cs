using System;
using System.Collections.Generic;

using static TokenType;

public class Scanner
{
    private readonly string Source;
    private readonly List<Token> Tokens = new List<Token>();
    private int Start = 0;
    private int Current = 0;
    private int Line = 1;

    private static readonly Dictionary<string, TokenType> Keywords =
        new Dictionary<string, TokenType>
        {
            ["and"] = And,
            ["class"] = Class,
            ["else"] = Else,
            ["false"] = False,
            ["for"] = For,
            ["fun"] = Fun,
            ["for"] = For,
            ["if"] = If,
            ["nil"] = Nil,
            ["or"] = Or,
            ["print"] = Print,
            ["return"] = Return,
            ["super"] = Super,
            ["this"] = This,
            ["true"] = True,
            ["var"] = Var,
            ["while"] = While,
        };

    public Scanner(string source)
    {
        Source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            Start = Current;
            ScanToken();
        }

        Tokens.Add(new Token(EOF, "", null, Line));
        return Tokens;
    }

    private void ScanToken()
    {
        char c = Advance();

        switch (c)
        {
            case '(':
                AddToken(LeftParen);
                break;
            case ')':
                AddToken(RightParen);
                break;
            case '{':
                AddToken(LeftBrace);
                break;
            case '}':
                AddToken(RightBrace);
                break;
            case ',':
                AddToken(Comma);
                break;
            case '.':
                AddToken(Dot);
                break;
            case '-':
                AddToken(Minus);
                break;
            case '+':
                AddToken(Plus);
                break;
            case ';':
                AddToken(Semicolon);
                break;
            case '*':
                AddToken(Star);
                break;
            case '!':
                AddToken(Match('=') ? BangEqual : Bang);
                break;
            case '=':
                AddToken(Match('=') ? EqualEqual : Equal);
                break;
            case '<':
                AddToken(Match('=') ? LessEqual : Less);
                break;
            case '>':
                AddToken(Match('=') ? GreaterEqual : Greater);
                break;
            case '/':
                if (Match('/'))
                {
                    while (Peek() != '\n' && !IsAtEnd())
                    {
                        Advance();
                    }
                }
                else
                {
                    AddToken(Slash);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                Line++;
                break;
            case '"':
                String();
                break;
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    Lox.Error(Line, "Unexpected character.");
                }
                break;
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        string text = Source.Substring(Start, Current - Start);
        TokenType type = TokenType.Identifier;
        Keywords.TryGetValue(text, out type);
        AddToken(type);
    }

    private void Number()
    {
        while (IsDigit(Peek()))
        {
            Advance();
        }

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();

            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        AddToken(TokenType.Number,
            Convert.ToDouble(Source.Substring(Start, Current - Start)));
    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                Line++;
            }

            Advance();
        }

        if (IsAtEnd())
        {
            Lox.Error(Line, "Unterminated string.");
            return;
        }

        Advance();

        string value = Source.Substring(Start + 1, Current - (Start + 1) - 1);
        AddToken(TokenType.String, value);
    }

    private bool Match(char expected)
    {
        if (IsAtEnd())
        {
            return false;
        }

        if (Source[Current] != expected)
        {
            return false;
        }

        Current++;
        return true;
    }

    private char Peek()
    {
        if (IsAtEnd())
        {
            return '\0';
        }

        return Source[Current];
    }

    private char PeekNext()
    {
        if (Current + 1 >= Source.Length)
        {
            return '\0';
        }

        return Source[Current + 1];
    }

    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') ||
            (c >= 'A' && c <= 'Z') ||
            c == '_';
    }

    private bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private bool IsAtEnd()
    {
        return Current >= Source.Length;
    }

    private char Advance()
    {
        Current++;
        return Source[Current - 1];
    }

    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    private void AddToken(TokenType type, object literal)
    {
        string text = Source.Substring(Start, Current - Start);
        Tokens.Add(new Token(type, text, literal, Line));
    }
}

namespace JG.WebKit.Views.Compilation;

/// <summary>
/// Defines the types of tokens recognized by the template tokenizer.
/// Each token type corresponds to a specific template syntax element.
/// </summary>
internal enum TokenType
{
    /// <summary>Raw HTML/text between template tags.</summary>
    Literal,
    
    /// <summary>Variable expression with HTML escaping: {{ expr }}</summary>
    Variable,
    
    /// <summary>Raw (unescaped) variable expression: {{{ expr }}}</summary>
    RawVariable,
    
    /// <summary>Comment that won't appear in output: {{-- text --}}</summary>
    Comment,
    
    /// <summary>Partial include: {{> name }}, {{> name "variant" }}, or {{> name context }}</summary>
    PartialStart,
    
    /// <summary>If block start: {{#if expr }}</summary>
    IfStart,
    
    /// <summary>Else-if branch: {{#elseif expr }}</summary>
    ElseIf,
    
    /// <summary>Else branch: {{#else}}</summary>
    Else,
    
    /// <summary>If block end: {{/if}}</summary>
    IfEnd,
    
    /// <summary>Loop block start: {{#each collection as item }}</summary>
    EachStart,
    
    /// <summary>Empty fallback in loop: {{#empty}}</summary>
    Empty,
    
    /// <summary>Loop block end: {{/each}}</summary>
    EachEnd,
    
    /// <summary>Layout declaration: {{#layout "name" }}</summary>
    LayoutDecl,
    
    /// <summary>Section definition start: {{#section "name" }}</summary>
    SectionStart,
    
    /// <summary>Section definition end: {{/section}}</summary>
    SectionEnd,
    
    /// <summary>Yield point in layout: {{#yield "name" }}</summary>
    YieldTag,
    
    /// <summary>Yield with default content start: {{#yield-default "name" }}</summary>
    YieldDefaultStart,
    
    /// <summary>Yield with default content end: {{/yield-default}}</summary>
    YieldDefaultEnd,
    
    /// <summary>Helper function call: {{ helperName arg1 arg2 }}</summary>
    HelperCall,
}

/// <summary>
/// Represents a single token extracted from a template during tokenization.
/// Contains the token type, content, and source location for error reporting.
/// </summary>
internal sealed class Token
{
    /// <summary>
    /// Gets or sets the type of this token.
    /// </summary>
    public TokenType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the token content (the text inside the template tags or the literal text).
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source line number where this token was found (1-based).
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// Gets or sets the source column number where this token starts (1-based).
    /// </summary>
    public int Column { get; set; }
}

/// <summary>
/// Tokenizes template source code into a stream of tokens for parsing.
/// Implements a forward-scanning lexer that recognizes all template syntax elements.
/// Tracks line and column numbers for accurate error reporting.
/// </summary>
internal sealed class Tokenizer
{
    private readonly string _input;
    private readonly List<Token> _tokens = new();
    private int _pos;
    private int _line = 1;
    private int _column = 1;

    /// <summary>
    /// Initializes a new instance of the Tokenizer class.
    /// </summary>
    /// <param name="input">The template source code to tokenize.</param>
    public Tokenizer(string input)
    {
        _input = input ?? string.Empty;
    }

    /// <summary>
    /// Tokenizes the entire input and returns a read-only list of tokens.
    /// Scans the input linearly, alternating between literal text and template tags.
    /// </summary>
    /// <returns>A read-only list of tokens representing the template structure.</returns>
    public IReadOnlyList<Token> Tokenize()
    {
        while (_pos < _input.Length)
        {
            if (Peek() == '{' && Peek(1) == '{')
            {
                TokenizeTag();
            }
            else
            {
                TokenizeLiteral();
            }
        }

        return _tokens;
    }

    /// <summary>
    /// Tokenizes a template tag starting with {{ and determines its type.
    /// Handles all tag variants:
    /// - {{{ }}} for raw variables
    /// - {{-- --}} for comments
    /// - {{> }} for partials
    /// - {{# }} for block starts
    /// - {{/ }} for block ends
    /// - {{ }} for variables and helpers
    /// </summary>
    private void TokenizeTag()
    {
        var startLine = _line;
        var startCol = _column;

        Consume('{');
        Consume('{');

        // Check for {{{ (raw variable)
        if (Peek() == '{')
        {
            Consume('{');
            var expr = ReadUntil("}}}");
            Consume('}');
            Consume('}');
            Consume('}');
            _tokens.Add(new Token
            {
                Type = TokenType.RawVariable,
                Content = expr.Trim(),
                Line = startLine,
                Column = startCol
            });
            return;
        }

        // Check for {{-- (comment)
        if (Peek() == '-' && Peek(1) == '-')
        {
            Consume('-');
            Consume('-');
            var comment = ReadUntil("--}}");
            Consume('-');
            Consume('-');
            Consume('}');
            Consume('}');
            _tokens.Add(new Token
            {
                Type = TokenType.Comment,
                Content = comment,
                Line = startLine,
                Column = startCol
            });
            return;
        }

        // Check for {{> (partial)
        if (Peek() == '>')
        {
            Consume('>');
            SkipWhitespace();
            var content = ReadUntil("}}").Trim();
            Consume('}');
            Consume('}');
            _tokens.Add(new Token
            {
                Type = TokenType.PartialStart,
                Content = content,
                Line = startLine,
                Column = startCol
            });
            return;
        }

        // Check for {{# (block tag)
        if (Peek() == '#')
        {
            Consume('#');
            ParseBlockTag(startLine, startCol);
            return;
        }

        // Check for {{/ (end tag)
        if (Peek() == '/')
        {
            Consume('/');
            var tagName = ReadWord();
            SkipWhitespace();
            Consume('}');
            Consume('}');

            TokenType endType = tagName switch
            {
                "if" => TokenType.IfEnd,
                "each" => TokenType.EachEnd,
                "section" => TokenType.SectionEnd,
                "yield-default" => TokenType.YieldDefaultEnd,
                _ => TokenType.Comment  // Unknown end tags are treated as comments (ignored)
            };

            // Skip unknown end tags
            if (endType == TokenType.Comment && tagName != "comment")
                return;

            _tokens.Add(new Token
            {
                Type = endType,
                Content = tagName,
                Line = startLine,
                Column = startCol
            });
            return;
        }

        // Otherwise it's a variable or helper call
        var expr2 = ReadUntil("}}").Trim();
        Consume('}');
        Consume('}');

        _tokens.Add(new Token
        {
            Type = TokenType.Variable,
            Content = expr2,
            Line = startLine,
            Column = startCol
        });
    }

    /// <summary>
    /// Parses a block tag (starts with {{#) and determines its specific type.
    /// Examples: if, elseif, else, each, empty, layout, section, yield, yield-default.
    /// </summary>
    /// <param name="startLine">The line number where the tag starts.</param>
    /// <param name="startCol">The column number where the tag starts.</param>
    private void ParseBlockTag(int startLine, int startCol)
    {
        var tagName = ReadWord();
        SkipWhitespace();

        var content = ReadUntil("}}");
        Consume('}');
        Consume('}');

        TokenType type = tagName switch
        {
            "if" => TokenType.IfStart,
            "elseif" => TokenType.ElseIf,
            "else" => TokenType.Else,
            "each" => TokenType.EachStart,
            "empty" => TokenType.Empty,
            "layout" => TokenType.LayoutDecl,
            "section" => TokenType.SectionStart,
            "yield" => TokenType.YieldTag,
            "yield-default" => TokenType.YieldDefaultStart,
            _ => throw new InvalidOperationException($"Unknown block tag: #{tagName}")
        };

        _tokens.Add(new Token
        {
            Type = type,
            Content = content.Trim(),
            Line = startLine,
            Column = startCol
        });
    }

    /// <summary>
    /// Tokenizes literal text content between template tags.
    /// Accumulates characters until the next {{ is encountered or end of input.
    /// </summary>
    private void TokenizeLiteral()
    {
        var sb = new StringBuilder();
        var startLine = _line;
        var startCol = _column;

        while (_pos < _input.Length && !(_input[_pos] == '{' && _pos + 1 < _input.Length && _input[_pos + 1] == '{'))
        {
            sb.Append(_input[_pos]);
            UpdatePosition(_input[_pos]);
            _pos++;
        }

        var content = sb.ToString();
        if (!string.IsNullOrEmpty(content))
        {
            _tokens.Add(new Token
            {
                Type = TokenType.Literal,
                Content = content,
                Line = startLine,
                Column = startCol
            });
        }
    }

    private string ReadUntil(string terminator)
    {
        var sb = new StringBuilder();

        while (_pos < _input.Length)
        {
            var remaining = _input.Substring(_pos);
            if (remaining.StartsWith(terminator, StringComparison.Ordinal))
            {
                break;
            }

            sb.Append(_input[_pos]);
            UpdatePosition(_input[_pos]);
            _pos++;
        }

        return sb.ToString();
    }

    private string ReadWord()
    {
        var sb = new StringBuilder();

        while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '-'))
        {
            sb.Append(_input[_pos]);
            _pos++;
            _column++;
        }

        return sb.ToString();
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
        {
            UpdatePosition(_input[_pos]);
            _pos++;
        }
    }

    private char Peek(int offset = 0)
    {
        var index = _pos + offset;
        return index < _input.Length ? _input[index] : '\0';
    }

    private void Consume(char expected)
    {
        if (_pos >= _input.Length || _input[_pos] != expected)
        {
            throw new InvalidOperationException($"Expected '{expected}' at line {_line}, column {_column}");
        }

        UpdatePosition(expected);
        _pos++;
    }

    private void UpdatePosition(char ch)
    {
        if (ch == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
    }
}

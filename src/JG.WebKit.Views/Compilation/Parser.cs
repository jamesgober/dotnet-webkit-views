namespace JG.WebKit.Views.Compilation;

using JG.WebKit.Views.Nodes;

/// <summary>
/// Parses a token stream into an abstract syntax tree (AST) of renderable nodes.
/// Implements a recursive descent parser with position tracking to prevent infinite loops.
/// </summary>
internal sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private readonly Dictionary<string, ITemplateHelper> _helpers;
    private readonly ViewEngineOptions _options;
    private int _pos;

    /// <summary>
    /// Initializes a new instance of the Parser class.
    /// </summary>
    /// <param name="tokens">The token stream to parse.</param>
    /// <param name="helpers">Optional dictionary of registered helpers for helper node creation.</param>
    /// <param name="options">Optional engine options for parser configuration.</param>
    public Parser(IReadOnlyList<Token> tokens, Dictionary<string, ITemplateHelper>? helpers = null, ViewEngineOptions? options = null)
    {
        _tokens = tokens;
        _helpers = helpers ?? new Dictionary<string, ITemplateHelper>();
        _options = options ?? new ViewEngineOptions();
    }

    /// <summary>
    /// Parses the token stream into a list of nodes representing the template's AST.
    /// </summary>
    /// <returns>A read-only list of parsed nodes ready for rendering.</returns>
    public IReadOnlyList<INode> Parse()
    {
        var nodes = new List<INode>();
        string? layoutName = null;

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];

            switch (token.Type)
            {
                case TokenType.Literal:
                    nodes.Add(new LiteralNode(token.Content));
                    _pos++;
                    break;

                case TokenType.Variable:
                    nodes.Add(new VariableNode(token.Content, _helpers));
                    _pos++;
                    break;

                case TokenType.RawVariable:
                    nodes.Add(new RawVariableNode(token.Content, _options.AllowRawOutput));
                    _pos++;
                    break;

                case TokenType.Comment:
                    nodes.Add(new CommentNode());
                    _pos++;
                    break;

                case TokenType.PartialStart:
                    nodes.Add(ParsePartial(token.Content));
                    _pos++;
                    break;

                case TokenType.IfStart:
                    nodes.Add(ParseIf(token.Content));
                    break;

                case TokenType.EachStart:
                    nodes.Add(ParseEach(token.Content));
                    break;

                case TokenType.LayoutDecl:
                    layoutName = ParseLayoutName(token.Content);
                    nodes.Add(new LayoutNode(layoutName));
                    _pos++;
                    break;

                case TokenType.SectionStart:
                    nodes.Add(ParseSection(token.Content));
                    break;

                case TokenType.YieldTag:
                    nodes.Add(new YieldNode(ParseYieldName(token.Content)));
                    _pos++;
                    break;

                case TokenType.YieldDefaultStart:
                    nodes.Add(ParseYieldDefault(token.Content));
                    break;

                default:
                    _pos++;
                    break;
            }
        }

        return nodes;
    }

    private VariableNode ParseVariable(string content)
    {
        return new VariableNode(content, _helpers);
    }

    private PartialNode ParsePartial(string content)
    {
        var parts = Tokenize(content);

        if (parts.Count == 0)
            throw new InvalidOperationException("Partial requires a name");

        var name = UnquoteString(parts[0]);
        string? variant = null;
        string? contextExpr = null;

        if (parts.Count > 1)
        {
            var second = parts[1];
            if (second.StartsWith('"') && second.EndsWith('"'))
            {
                variant = UnquoteString(second);
            }
            else
            {
                contextExpr = second;
            }
        }

        return new PartialNode(name, variant, contextExpr, null, _options);
    }

    private IfNode ParseIf(string content)
    {
        var condition = content;
        var trueNodes = new List<INode>();
        var elseIfNodes = new Dictionary<string, List<INode>>();
        List<INode>? elseNodes = null;

        _pos++;

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];

            if (token.Type == TokenType.ElseIf)
            {
                var elseIfCondition = token.Content;
                _pos++;

                var elseIfBody = new List<INode>();
                while (_pos < _tokens.Count && _tokens[_pos].Type != TokenType.ElseIf && _tokens[_pos].Type != TokenType.Else && _tokens[_pos].Type != TokenType.IfEnd)
                {
                    var node = ParseNode();
                    if (node != null)
                        elseIfBody.Add(node);
                }

                elseIfNodes[elseIfCondition] = elseIfBody;
            }
            else if (token.Type == TokenType.Else)
            {
                _pos++;
                elseNodes = new List<INode>();

                while (_pos < _tokens.Count && _tokens[_pos].Type != TokenType.IfEnd)
                {
                    var node = ParseNode();
                    if (node != null)
                        elseNodes.Add(node);
                }

                break;
            }
            else if (token.Type == TokenType.IfEnd)
            {
                _pos++;
                break;
            }
            else
            {
                var node = ParseNode();
                if (node != null)
                    trueNodes.Add(node);
            }
        }

        return new IfNode(condition, trueNodes, elseIfNodes, elseNodes);
    }

    /// <summary>
    /// Parses an each (loop) block with optional empty fallback.
    /// Syntax: {{#each collection as itemName }}...{{#empty}}...{{/empty}}{{/each}}
    /// </summary>
    private EachNode ParseEach(string content)
    {
        var parts = content.Split(" as ", StringSplitOptions.None);
        if (parts.Length != 2)
            throw new InvalidOperationException("Invalid each syntax");

        var collectionExpr = parts[0].Trim();
        var itemName = parts[1].Trim();

        var bodyNodes = new List<INode>();
        List<INode>? emptyNodes = null;

        _pos++; // Move past the EachStart token

        while (_pos < _tokens.Count)
        {
            var token = _tokens[_pos];

            if (token.Type == TokenType.Empty)
            {
                _pos++; // Move past the Empty token
                emptyNodes = new List<INode>();

                // Parse empty block body until EachEnd
                while (_pos < _tokens.Count && _tokens[_pos].Type != TokenType.EachEnd)
                {
                    var node = ParseNode();
                    if (node != null)
                        emptyNodes.Add(node);
                }

                break;
            }
            else if (token.Type == TokenType.EachEnd)
            {
                _pos++; // Move past the EachEnd token
                break;
            }
            else
            {
                // Parse loop body node (ParseNode handles position advancement)
                var node = ParseNode();
                if (node != null)
                    bodyNodes.Add(node);
            }
        }

        return new EachNode(collectionExpr, itemName, bodyNodes, emptyNodes);
    }

    private SectionNode ParseSection(string content)
    {
        var sectionName = UnquoteString(content);
        var nodes = new List<INode>();

        _pos++;

        while (_pos < _tokens.Count && _tokens[_pos].Type != TokenType.SectionEnd)
        {
            var node = ParseNode();
            if (node != null)
                nodes.Add(node);
        }

        if (_pos < _tokens.Count && _tokens[_pos].Type == TokenType.SectionEnd)
            _pos++;

        return new SectionNode(sectionName, nodes);
    }

    private YieldDefaultNode ParseYieldDefault(string content)
    {
        var sectionName = UnquoteString(content);
        var nodes = new List<INode>();

        _pos++;

        while (_pos < _tokens.Count && _tokens[_pos].Type != TokenType.YieldDefaultEnd)
        {
            var node = ParseNode();
            if (node != null)
                nodes.Add(node);
        }

        if (_pos < _tokens.Count && _tokens[_pos].Type == TokenType.YieldDefaultEnd)
            _pos++;

        return new YieldDefaultNode(sectionName, nodes);
    }

    /// <summary>
    /// Parses a single node from the current token position.
    /// CRITICAL: This method handles position advancement for single-token node types
    /// to prevent infinite loops. Block-type nodes (if, each, section, yield-default)
    /// handle their own position advancement internally.
    /// </summary>
    /// <returns>The parsed node, or null if the token type is not recognized.</returns>
    private INode? ParseNode()
    {
        if (_pos >= _tokens.Count)
            return null;

        var token = _tokens[_pos];

        INode? node = token.Type switch
        {
            TokenType.Literal => new LiteralNode(token.Content),
            TokenType.Variable => ParseVariable(token.Content),
            TokenType.RawVariable => new RawVariableNode(token.Content, _options.AllowRawOutput),
            TokenType.Comment => new CommentNode(),
            TokenType.PartialStart => ParsePartial(token.Content),
            TokenType.IfStart => ParseIf(token.Content),
            TokenType.EachStart => ParseEach(token.Content),
            TokenType.SectionStart => ParseSection(token.Content),
            TokenType.YieldTag => new YieldNode(ParseYieldName(token.Content)),
            TokenType.YieldDefaultStart => ParseYieldDefault(token.Content),
            _ => null
        };

        // Advance position for single-token node types (not block types that handle their own advancement)
        // This is critical to prevent infinite loops when unrecognized tokens appear
        if (node != null && token.Type != TokenType.IfStart && token.Type != TokenType.EachStart && 
            token.Type != TokenType.SectionStart && token.Type != TokenType.YieldDefaultStart)
        {
            _pos++;
        }

        return node;
    }

    private static string ParseLayoutName(string content)
    {
        return UnquoteString(content);
    }

    private static string ParseYieldName(string content)
    {
        return UnquoteString(content);
    }

    private static string UnquoteString(string value)
    {
        var trimmed = value.Trim();
        if ((trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
            (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
        {
            return trimmed.Substring(1, trimmed.Length - 2);
        }

        return trimmed;
    }

    private static List<string> Tokenize(string content)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';

        foreach (var ch in content)
        {
            if ((ch == '"' || ch == '\'') && !inQuote)
            {
                inQuote = true;
                quoteChar = ch;
                current.Append(ch);
            }
            else if (ch == quoteChar && inQuote)
            {
                inQuote = false;
                current.Append(ch);
            }
            else if (ch == ' ' && !inQuote)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}

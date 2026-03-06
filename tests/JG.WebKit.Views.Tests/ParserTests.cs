namespace JG.WebKit.Views.Tests;

using JG.WebKit.Views.Compilation;
using Xunit;

public class ParserTests
{
    [Fact]
    public void Parse_LiteralToken_CreatesLiteralNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.Literal, Content = "Hello World" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_VariableToken_CreatesVariableNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.Variable, Content = "title" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_RawVariableToken_CreatesRawVariableNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.RawVariable, Content = "html" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_CommentToken_CreatesCommentNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.Comment, Content = "comment" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_PartialToken_CreatesPartialNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.PartialStart, Content = "header" },
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_IfBlock_CreatesIfNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.IfStart, Content = "condition" },
            new() { Type = TokenType.Literal, Content = "content" },
            new() { Type = TokenType.IfEnd, Content = "if" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_IfElseBlock_CreatesIfNodeWithElse()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.IfStart, Content = "condition" },
            new() { Type = TokenType.Literal, Content = "true content" },
            new() { Type = TokenType.Else },
            new() { Type = TokenType.Literal, Content = "false content" },
            new() { Type = TokenType.IfEnd, Content = "if" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_EachBlock_CreatesEachNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.EachStart, Content = "items as item" },
            new() { Type = TokenType.Literal, Content = "item" },
            new() { Type = TokenType.EachEnd, Content = "each" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_EachWithEmpty_CreatesEachNodeWithEmpty()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.EachStart, Content = "items as item" },
            new() { Type = TokenType.Empty },
            new() { Type = TokenType.Literal, Content = "no items" },
            new() { Type = TokenType.EachEnd, Content = "each" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_SectionBlock_CreatesSectionNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.SectionStart, Content = "\"sidebar\"" },
            new() { Type = TokenType.Literal, Content = "sidebar content" },
            new() { Type = TokenType.SectionEnd, Content = "section" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_LayoutDecl_CreatesLayoutNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.LayoutDecl, Content = "\"main\"" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_YieldTag_CreatesYieldNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.YieldTag, Content = "\"content\"" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }

    [Fact]
    public void Parse_YieldDefaultBlock_CreatesYieldDefaultNode()
    {
        var tokens = new List<Token>
        {
            new() { Type = TokenType.YieldDefaultStart, Content = "\"sidebar\"" },
            new() { Type = TokenType.Literal, Content = "default sidebar" },
            new() { Type = TokenType.YieldDefaultEnd, Content = "yield-default" }
        };

        var parser = new Parser(tokens);
        var nodes = parser.Parse();

        Assert.Single(nodes);
    }
}

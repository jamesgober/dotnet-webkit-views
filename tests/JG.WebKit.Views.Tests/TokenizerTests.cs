namespace JG.WebKit.Views.Tests;

using JG.WebKit.Views.Compilation;
using Xunit;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_LiteralText_ReturnsLiteralToken()
    {
        var tokenizer = new Tokenizer("Hello World");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Literal, tokens[0].Type);
        Assert.Equal("Hello World", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_SimpleVariable_ReturnsVariableToken()
    {
        var tokenizer = new Tokenizer("{{ title }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Variable, tokens[0].Type);
        Assert.Equal("title", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_RawVariable_ReturnsRawVariableToken()
    {
        var tokenizer = new Tokenizer("{{{ html }}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.RawVariable, tokens[0].Type);
        Assert.Equal("html", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_Comment_ReturnsCommentToken()
    {
        var tokenizer = new Tokenizer("{{-- This is a comment --}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Comment, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_NestedPropertyPath_ReturnsCorrectContent()
    {
        var tokenizer = new Tokenizer("{{ post.author.name }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Variable, tokens[0].Type);
        Assert.Equal("post.author.name", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_ArrayIndex_ReturnsCorrectContent()
    {
        var tokenizer = new Tokenizer("{{ items[0] }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Variable, tokens[0].Type);
        Assert.Equal("items[0]", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_PartialTag_ReturnsPartialStartToken()
    {
        var tokenizer = new Tokenizer("{{> header }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.PartialStart, tokens[0].Type);
        Assert.Equal("header", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_PartialWithVariant_ReturnsCorrectContent()
    {
        var tokenizer = new Tokenizer("{{> header \"dark\" }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.PartialStart, tokens[0].Type);
        Assert.Contains("header", tokens[0].Content);
        Assert.Contains("dark", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_IfTag_ReturnsIfStartToken()
    {
        var tokenizer = new Tokenizer("{{#if user.isAdmin }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.IfStart, tokens[0].Type);
        Assert.Equal("user.isAdmin", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_ElseIfTag_ReturnsElseIfToken()
    {
        var tokenizer = new Tokenizer("{{#elseif condition }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.ElseIf, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_ElseTag_ReturnsElseToken()
    {
        var tokenizer = new Tokenizer("{{#else}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Else, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_IfEndTag_ReturnsIfEndToken()
    {
        var tokenizer = new Tokenizer("{{/if}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.IfEnd, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_EachTag_ReturnsEachStartToken()
    {
        var tokenizer = new Tokenizer("{{#each posts as post }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EachStart, tokens[0].Type);
        Assert.Contains("posts", tokens[0].Content);
        Assert.Contains("post", tokens[0].Content);
    }

    [Fact]
    public void Tokenize_EmptyTag_ReturnsEmptyToken()
    {
        var tokenizer = new Tokenizer("{{#empty}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Empty, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_EachEndTag_ReturnsEachEndToken()
    {
        var tokenizer = new Tokenizer("{{/each}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EachEnd, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_LayoutTag_ReturnsLayoutDeclToken()
    {
        var tokenizer = new Tokenizer("{{#layout \"main\" }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.LayoutDecl, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_SectionTag_ReturnsSectionStartToken()
    {
        var tokenizer = new Tokenizer("{{#section \"sidebar\" }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.SectionStart, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_SectionEndTag_ReturnsSectionEndToken()
    {
        var tokenizer = new Tokenizer("{{/section}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.SectionEnd, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_YieldTag_ReturnsYieldToken()
    {
        var tokenizer = new Tokenizer("{{#yield \"content\" }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.YieldTag, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_YieldDefaultStart_ReturnsYieldDefaultStartToken()
    {
        var tokenizer = new Tokenizer("{{#yield-default \"sidebar\" }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.YieldDefaultStart, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_YieldDefaultEnd_ReturnsYieldDefaultEndToken()
    {
        var tokenizer = new Tokenizer("{{/yield-default}}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.YieldDefaultEnd, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_MixedContent_ReturnsMixedTokens()
    {
        var template = "<h1>{{ title }}</h1><p>{{ content }}</p>";
        var tokenizer = new Tokenizer(template);
        var tokens = tokenizer.Tokenize();

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenType.Literal, tokens[0].Type);
        Assert.Equal(TokenType.Variable, tokens[1].Type);
        Assert.Equal(TokenType.Literal, tokens[2].Type);
        Assert.Equal(TokenType.Variable, tokens[3].Type);
        Assert.Equal(TokenType.Literal, tokens[4].Type);
    }

    [Fact]
    public void Tokenize_EmptyInput_ReturnsEmptyTokenList()
    {
        var tokenizer = new Tokenizer(string.Empty);
        var tokens = tokenizer.Tokenize();

        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_WhitespaceInVariable_RetrimsTrimmed()
    {
        var tokenizer = new Tokenizer("{{  title  }}");
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal("title", tokens[0].Content);
    }
}

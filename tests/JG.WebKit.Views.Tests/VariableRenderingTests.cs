namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class VariableRenderingTests
{
    [Fact]
    public async Task Render_SimpleVariable_ReturnsValue()
    {
        var data = new Dictionary<string, object?> { ["title"] = "Hello" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ title }}", context);

        result.Should().Be("Hello");
    }

    [Fact]
    public async Task Render_NestedProperty_ReturnsValue()
    {
        var post = new { author = new { name = "John" } };
        var data = new Dictionary<string, object?> { ["post"] = post };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ post.author.name }}", context);

        result.Should().Be("John");
    }

    [Fact]
    public async Task Render_ArrayIndex_ReturnsValue()
    {
        var data = new Dictionary<string, object?> { ["items"] = new[] { "first", "second", "third" } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ items[0] }}", context);

        result.Should().Be("first");
    }

    [Fact]
    public async Task Render_HtmlEscaping_EscapesSpecialCharacters()
    {
        var data = new Dictionary<string, object?> { ["text"] = "<script>alert('xss')</script>" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().Contain("&lt;");
        result.Should().Contain("&gt;");
        result.Should().NotContain("<script>");
    }

    [Fact]
    public async Task Render_RawVariable_DoesNotEscape()
    {
        var options = new ViewEngineOptions { AllowRawOutput = true };
        var data = new Dictionary<string, object?> { ["html"] = "<strong>Bold</strong>" };
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{{ html }}}", context);

        result.Should().Contain("<strong>Bold</strong>");
    }

    [Fact]
    public async Task Render_MissingVariable_ReturnsEmpty()
    {
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ missing }}", context);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Render_NullVariable_ReturnsEmpty()
    {
        var data = new Dictionary<string, object?> { ["value"] = null };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ value }}", context);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Render_GlobalVariable_ReturnsValue()
    {
        var data = new Dictionary<string, object?>();
        var globals = new Dictionary<string, object?> { ["siteName"] = "My Site" };
        var context = new TemplateContext(data, globals);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ siteName }}", context);

        result.Should().Be("My Site");
    }

    [Fact]
    public async Task Render_EscapesAmpersand()
    {
        var data = new Dictionary<string, object?> { ["text"] = "A & B" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().Be("A &amp; B");
    }

    [Fact]
    public async Task Render_EscapesQuote()
    {
        var data = new Dictionary<string, object?> { ["text"] = "Say \"Hello\"" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().Contain("&quot;");
    }

    [Fact]
    public async Task Render_EscapesApostrophe()
    {
        var data = new Dictionary<string, object?> { ["text"] = "It's great" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().Contain("&#x27;");
    }

    [Fact]
    public async Task Render_MultipleVariables_ReturnsEscapedValues()
    {
        var data = new Dictionary<string, object?> { ["first"] = "<tag>", ["second"] = "&stuff" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ first }} {{ second }}", context);

        result.Should().Contain("&lt;tag&gt;");
        result.Should().Contain("&amp;stuff");
    }

    [Fact]
    public async Task Render_BooleanTrue_ReturnsTrue()
    {
        var data = new Dictionary<string, object?> { ["flag"] = true };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ flag }}", context);

        result.Should().Be("true");
    }

    [Fact]
    public async Task Render_BooleanFalse_ReturnsFalse()
    {
        var data = new Dictionary<string, object?> { ["flag"] = false };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ flag }}", context);

        result.Should().Be("false");
    }

    [Fact]
    public async Task Render_Integer_ReturnsStringValue()
    {
        var data = new Dictionary<string, object?> { ["count"] = 42 };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ count }}", context);

        result.Should().Be("42");
    }

    private static IViewEngine CreateEngine(ViewEngineOptions? options = null)
    {
        options ??= new ViewEngineOptions();
        var provider = new InMemoryTemplateProvider();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

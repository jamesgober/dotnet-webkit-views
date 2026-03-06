namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class SecurityTests
{
    [Fact]
    public async Task Security_RawOutput_DisabledByDefault()
    {
        var options = new ViewEngineOptions { AllowRawOutput = false };
        var data = new Dictionary<string, object?> { ["html"] = "<script>alert('xss')</script>" };
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{{ html }}}", context);

        // Raw output should not contain the HTML even when {{{ }}} is used if disabled
        result.Should().NotContain("<script>");
    }

    [Fact]
    public async Task Security_ScriptInjection_EscapedInVariable()
    {
        var data = new Dictionary<string, object?> { ["text"] = "<script>alert('xss')</script>" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().NotContain("<script>");
        result.Should().Contain("&lt;");
    }

    [Fact]
    public async Task Security_AmpersandEscaped()
    {
        var data = new Dictionary<string, object?> { ["url"] = "/?a=1&b=2" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ url }}", context);

        result.Should().Be("/?a=1&amp;b=2");
    }

    [Fact]
    public async Task Security_QuoteEscaped()
    {
        var data = new Dictionary<string, object?> { ["text"] = "Say \"hello\"" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().Contain("&quot;");
        result.Should().NotContain("\"");
    }

    [Fact]
    public async Task Security_ApostropheEscaped()
    {
        var data = new Dictionary<string, object?> { ["text"] = "It's mine" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ text }}", context);

        result.Should().Contain("&#x27;");
        result.Should().NotContain("'");
    }

    private static IViewEngine CreateEngine(ViewEngineOptions? options = null)
    {
        options ??= new ViewEngineOptions();
        var provider = new InMemoryTemplateProvider();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

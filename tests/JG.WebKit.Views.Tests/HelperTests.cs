namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class HelperTests
{
    [Fact]
    public async Task Render_DateHelper_FormatsDate()
    {
        var data = new Dictionary<string, object?> 
        { 
            ["publishedAt"] = new DateTimeOffset(2026, 3, 6, 12, 0, 0, TimeSpan.Zero) 
        };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ date publishedAt \"yyyy-MM-dd\" }}", context);

        result.Should().Contain("2026-03-06");
    }

    [Fact]
    public async Task Render_TruncateHelper_TruncatesString()
    {
        var data = new Dictionary<string, object?> { ["text"] = "This is a long text" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ truncate text 10 }}", context);

        result.Should().Be("This is a ...");
    }

    [Fact]
    public async Task Render_UppercaseHelper_ConvertsToUppercase()
    {
        var data = new Dictionary<string, object?> { ["text"] = "hello" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ uppercase text }}", context);

        result.Should().Be("HELLO");
    }

    [Fact]
    public async Task Render_LowercaseHelper_ConvertsToLowercase()
    {
        var data = new Dictionary<string, object?> { ["text"] = "HELLO" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ lowercase text }}", context);

        result.Should().Be("hello");
    }

    [Fact]
    public async Task Render_JsonHelper_SerializesObject()
    {
        var obj = new { name = "John", age = 30 };
        var data = new Dictionary<string, object?> { ["data"] = obj };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{ json data }}", context);

        result.Should().Contain("\"name\"");
        result.Should().Contain("John");
    }

    private static IViewEngine CreateEngine(ViewEngineOptions? options = null)
    {
        options ??= new ViewEngineOptions();
        var provider = new InMemoryTemplateProvider();
        
        var helpers = new Dictionary<string, ITemplateHelper>
        {
            ["date"] = new DateHelper(),
            ["truncate"] = new TruncateHelper(),
            ["uppercase"] = new UppercaseHelper(),
            ["lowercase"] = new LowercaseHelper(),
            ["json"] = new JsonHelper()
        };

        return new Internal.ViewEngine(provider, options, helpers);
    }
}

public class AssetHelperTests
{
    [Fact]
    public async Task Render_AssetHelper_ResolvesPath()
    {
        var options = new ViewEngineOptions();
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ asset \"main.css\" }}", context);

        result.Should().Contain("/assets/main.css");
    }

    [Fact]
    public async Task Render_ImageHelper_ResolvesImagePath()
    {
        var options = new ViewEngineOptions();
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ image \"logo.png\" }}", context);

        result.Should().Contain("/assets/images/logo.png");
    }

    [Fact]
    public async Task Render_ScriptHelper_ResolvesScriptPath()
    {
        var options = new ViewEngineOptions();
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ script \"app.js\" }}", context);

        result.Should().Contain("/assets/js/app.js");
    }

    [Fact]
    public async Task Render_FontHelper_ResolvesFontPath()
    {
        var options = new ViewEngineOptions();
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ font \"Inter.woff2\" }}", context);

        result.Should().Contain("/assets/fonts/Inter.woff2");
    }

    [Fact]
    public async Task Render_MediaHelper_ResolvesMediaPath()
    {
        var options = new ViewEngineOptions();
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ media \"video.mp4\" }}", context);

        result.Should().Contain("/assets/media/video.mp4");
    }

    [Fact]
    public async Task Render_AssetHelper_WithCDN_PrependsCdnUrl()
    {
        var options = new ViewEngineOptions
        {
            Assets = { CdnBaseUrl = "https://cdn.example.com" }
        };
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ asset \"main.css\" }}", context);

        result.Should().StartWith("https://cdn.example.com");
    }

    [Fact]
    public async Task Render_AssetHelper_WithVersionHash_AppendsHash()
    {
        var options = new ViewEngineOptions
        {
            Assets = { AppendVersionHash = true }
        };
        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(options);
        var result = await engine.RenderStringAsync("{{ asset \"main.css\" }}", context);

        result.Should().Contain("?v=");
    }

    private static IViewEngine CreateEngine(ViewEngineOptions options)
    {
        var provider = new InMemoryTemplateProvider();
        
        var helpers = new Dictionary<string, ITemplateHelper>
        {
            ["asset"] = new AssetHelper(options),
            ["image"] = new ImageHelper(options),
            ["script"] = new ScriptHelper(options),
            ["font"] = new FontHelper(options),
            ["media"] = new MediaHelper(options)
        };

        return new Internal.ViewEngine(provider, options, helpers);
    }
}

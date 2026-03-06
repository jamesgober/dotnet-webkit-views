namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class PartialTests
{
    [Fact(Timeout = 5000)]
    public async Task Render_PartialInclude_LoadsAndRendersPartial()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("header", "<header>Site</header>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> header }}", context);

        result.Should().Contain("<header>Site</header>");
    }

    [Fact]
    public async Task Render_PartialWithVariant_LoadsVariantFile()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("header-dark", "<header class='dark'>Site</header>");
        provider.AddTemplate("header", "<header>Site</header>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> header \"dark\" }}", context);

        result.Should().Contain("dark");
    }

    [Fact]
    public async Task Render_PartialWithVariantFallback_UsesDefaultWhenVariantMissing()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("header", "<header>Default</header>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> header \"missing\" }}", context);

        result.Should().Contain("Default");
    }

    [Fact]
    public async Task Render_PartialWithContext_PassesDataToPartial()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("card", "<div>{{ title }}</div>");

        var data = new Dictionary<string, object?> { ["post"] = new { title = "My Post" } };
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> card post }}", context);

        result.Should().Contain("My Post");
    }

    [Fact]
    public async Task Render_MissingPartial_ReturnsEmpty()
    {
        var provider = new InMemoryTemplateProvider();

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> missing }}", context);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Render_NestedPartials_WorkCorrectly()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("layout", "<div>{{> inner }}</div>");
        provider.AddTemplate("inner", "<span>{{ text }}</span>");

        var data = new Dictionary<string, object?> { ["text"] = "Content" };
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> layout }}", context);

        result.Should().Contain("<span>Content</span>");
    }

    [Fact]
    public async Task Render_PartialPreservesGlobals()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("footer", "<footer>{{ siteName }}</footer>");

        var data = new Dictionary<string, object?>();
        var globals = new Dictionary<string, object?> { ["siteName"] = "My Site" };
        var context = new TemplateContext(data, globals);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{> footer }}", context);

        result.Should().Contain("My Site");
    }

    private static IViewEngine CreateEngine(ITemplateProvider? provider = null)
    {
        provider ??= new InMemoryTemplateProvider();
        var options = new ViewEngineOptions();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

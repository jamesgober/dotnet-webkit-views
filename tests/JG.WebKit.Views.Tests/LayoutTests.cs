namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class LayoutTests
{
    [Fact]
    public async Task Render_LayoutWithContent_RendersCombined()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("main", "<html><body>{{#yield \"content\" }}</body></html>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var result = await engine.RenderStringAsync("{{#layout \"main\" }}<h1>Page</h1>", context);

        result.Should().Contain("<body><h1>Page</h1></body>");
    }

    [Fact]
    public async Task Render_LayoutWithMultipleSections_RendersSections()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("main", 
            "<html><body>{{#yield \"content\" }}<aside>{{#yield \"sidebar\" }}</aside></body></html>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var template = "{{#layout \"main\" }}<h1>Page</h1>{{#section \"sidebar\" }}<p>Sidebar</p>{{/section}}";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Contain("<h1>Page</h1>");
        result.Should().Contain("<p>Sidebar</p>");
    }

    [Fact]
    public async Task Render_LayoutWithYieldDefault_RendersDefaultWhenNoSection()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("main", 
            "<html><body>{{#yield \"content\" }}<aside>{{#yield-default \"sidebar\" }}<p>Default Sidebar</p>{{/yield-default}}</aside></body></html>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var template = "{{#layout \"main\" }}<h1>Page</h1>";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Contain("<p>Default Sidebar</p>");
    }

    [Fact]
    public async Task Render_LayoutWithSectionOverride_RendersSection()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("main", 
            "<html><body><aside>{{#yield-default \"sidebar\" }}<p>Default Sidebar</p>{{/yield-default}}</aside></body></html>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var template = "{{#layout \"main\" }}{{#section \"sidebar\" }}<p>Custom Sidebar</p>{{/section}}";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Contain("<p>Custom Sidebar</p>");
        result.Should().NotContain("<p>Default Sidebar</p>");
    }

    [Fact]
    public async Task Render_MissingLayout_RenderWithoutLayout()
    {
        var provider = new InMemoryTemplateProvider();

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var template = "{{#layout \"missing\" }}<h1>Page</h1>";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Contain("<h1>Page</h1>");
    }

    [Fact]
    public async Task Render_LayoutWithoutYieldSection_RendersNothing()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("main", "<html><body></body></html>");

        var data = new Dictionary<string, object?>();
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var template = "{{#layout \"main\" }}<h1>Page</h1>";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Be("<html><body></body></html>");
    }

    [Fact]
    public async Task Render_LayoutWithData_PassesDataToLayout()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("main", "<html><title>{{ title }}</title><body>{{#yield \"content\" }}</body></html>");

        var data = new Dictionary<string, object?> { ["title"] = "My Page" };
        var context = new TemplateContext(data);

        var engine = CreateEngine(provider);
        var template = "{{#layout \"main\" }}<h1>Page</h1>";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Contain("<title>My Page</title>");
    }

    private static IViewEngine CreateEngine(ITemplateProvider? provider = null)
    {
        provider ??= new InMemoryTemplateProvider();
        var options = new ViewEngineOptions();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

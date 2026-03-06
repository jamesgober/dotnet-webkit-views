namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class LoopTests
{
    [Fact]
    public async Task Render_EachArray_IteratesOverItems()
    {
        var data = new Dictionary<string, object?> { ["items"] = new[] { "a", "b", "c" } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#each items as item }}{{ item }}{{/each}}", context);

        result.Should().Be("abc");
    }

    [Fact]
    public async Task Render_EachWithIndex_InjectsIndex()
    {
        var data = new Dictionary<string, object?> { ["items"] = new[] { "a", "b", "c" } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#each items as item }}{{ item.index }}{{/each}}", context);

        result.Should().Be("012");
    }

    [Fact]
    public async Task Render_EachEmpty_RendersEmptyBlock()
    {
        var data = new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#each items as item }}Item{{#empty}}No items{{/each}}", context);

        result.Should().Be("No items");
    }

    [Fact]
    public async Task Render_EachNonEmpty_SkipsEmptyBlock()
    {
        var data = new Dictionary<string, object?> { ["items"] = new[] { 1 } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#each items as item }}Item{{#empty}}No items{{/each}}", context);

        result.Should().Be("Item");
    }

    [Fact]
    public async Task Render_NestedLoops_WorkCorrectly()
    {
        var items = new[]
        {
            new { id = 1, tags = new[] { "a", "b" } },
            new { id = 2, tags = new[] { "c" } }
        };
        var data = new Dictionary<string, object?> { ["items"] = items };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var template = "{{#each items as item }}[{{#each item.tags as tag }}{{ tag }}{{/each}}]{{/each}}";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Be("[ab][c]");
    }

    [Fact]
    public async Task Render_EachWithLiteral_RendersLiteralsCorrectly()
    {
        var data = new Dictionary<string, object?> { ["items"] = new[] { "one", "two" } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#each items as item }}<li>{{ item }}</li>{{/each}}", context);

        result.Should().Be("<li>one</li><li>two</li>");
    }

    [Fact]
    public async Task Render_EachWithConditional_WorksCorrectly()
    {
        var items = new[] { new { active = true }, new { active = false }, new { active = true } };
        var data = new Dictionary<string, object?> { ["items"] = items };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var template = "{{#each items as item }}{{#if item.active }}A{{/if}}{{/each}}";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Be("AA");
    }

    private static IViewEngine CreateEngine(ViewEngineOptions? options = null)
    {
        options ??= new ViewEngineOptions();
        var provider = new InMemoryTemplateProvider();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

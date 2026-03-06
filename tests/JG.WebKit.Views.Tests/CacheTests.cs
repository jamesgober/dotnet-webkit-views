namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class CacheTests
{
    [Fact]
    public async Task Cache_SecondRender_UsesCachedDelegate()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("test", "{{ title }}");

        var data = new Dictionary<string, object?> { ["title"] = "Hello" };
        var context = new TemplateContext(data);

        var options = new ViewEngineOptions { CacheCompiledTemplates = true };
        var engine = CreateEngine(provider, options);

        var result1 = await engine.RenderAsync("test", context);
        var result2 = await engine.RenderAsync("test", context);

        result1.Should().Be(result2);
    }

    [Fact]
    public async Task Cache_InvalidateAll_ClearsCache()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("test", "{{ value }}");

        var data1 = new Dictionary<string, object?> { ["value"] = "First" };
        var context1 = new TemplateContext(data1);

        var options = new ViewEngineOptions { CacheCompiledTemplates = true };
        var engine = CreateEngine(provider, options);

        var result1 = await engine.RenderAsync("test", context1);
        
        engine.InvalidateCache();
        
        var data2 = new Dictionary<string, object?> { ["value"] = "Second" };
        var context2 = new TemplateContext(data2);
        var result2 = await engine.RenderAsync("test", context2);

        result1.Should().Be("First");
        result2.Should().Be("Second");
    }

    [Fact]
    public async Task Cache_InvalidatePath_RemovesSpecificTemplate()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("test1", "{{ value }}");
        provider.AddTemplate("test2", "{{ value }}");

        var data = new Dictionary<string, object?> { ["value"] = "Test" };
        var context = new TemplateContext(data);

        var options = new ViewEngineOptions { CacheCompiledTemplates = true };
        var engine = CreateEngine(provider, options);

        var result1a = await engine.RenderAsync("test1", context);
        var result2a = await engine.RenderAsync("test2", context);
        
        engine.InvalidateCache("test1");
        
        var result1b = await engine.RenderAsync("test1", context);
        var result2b = await engine.RenderAsync("test2", context);

        result1a.Should().Be(result1b);
        result2a.Should().Be(result2b);
    }

    [Fact]
    public async Task Cache_CacheDisabled_DoesNotCache()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("test", "{{ id }}");

        var data = new Dictionary<string, object?> { ["id"] = 1 };
        var context = new TemplateContext(data);

        var options = new ViewEngineOptions { CacheCompiledTemplates = false };
        var engine = CreateEngine(provider, options);

        var result1 = await engine.RenderAsync("test", context);
        
        data["id"] = 2;
        var result2 = await engine.RenderAsync("test", context);

        result1.Should().Be("1");
        result2.Should().Be("2");
    }

    [Fact]
    public async Task Cache_InMemoryProvider_Caches()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("test", "{{ count }}");

        var data = new Dictionary<string, object?> { ["count"] = 10 };
        var context = new TemplateContext(data);

        var options = new ViewEngineOptions { CacheCompiledTemplates = true };
        var engine = CreateEngine(provider, options);

        var result1 = await engine.RenderAsync("test", context);
        var result2 = await engine.RenderAsync("test", context);

        result1.Should().Be(result2);
    }

    private static IViewEngine CreateEngine(ITemplateProvider provider, ViewEngineOptions options)
    {
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

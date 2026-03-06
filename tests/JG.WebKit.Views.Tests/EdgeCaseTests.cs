using JG.WebKit.Views;
using JG.WebKit.Views.Providers;
using Xunit;

namespace JG.WebKit.Views.Tests;

public class EdgeCaseTests
{
    private IViewEngine CreateEngine(Dictionary<string, string>? templates = null)
    {
        var provider = new InMemoryTemplateProvider();
        if (templates != null)
        {
            foreach (var (path, content) in templates)
                provider.AddTemplate(path, content);
        }
        return new Internal.ViewEngine(provider, new ViewEngineOptions(), new());
    }

    [Fact]
    public async Task Render_EmptyTemplate_ReturnsEmpty()
    {
        var engine = CreateEngine(new() { ["test"] = "" });
        var context = new TemplateContext(new Dictionary<string, object?>());
        var result = await engine.RenderStringAsync("", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Render_OnlyWhitespace_ReturnsWhitespace()
    {
        var engine = CreateEngine(new() { ["test"] = "   \n\t  " });
        var context = new TemplateContext(new Dictionary<string, object?>());
        var result = await engine.RenderStringAsync("   \n\t  ", context);
        Assert.Equal("   \n\t  ", result);
    }

    [Fact]
    public async Task Render_NullVariable_RendersEmpty()
    {
        var engine = CreateEngine(new() { ["test"] = "{{ nullVar }}" });
        var context = new TemplateContext(new Dictionary<string, object?> { ["nullVar"] = null });
        var result = await engine.RenderStringAsync("{{ nullVar }}", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Render_MissingVariable_RendersEmpty()
    {
        var engine = CreateEngine(new() { ["test"] = "{{ missingVar }}" });
        var context = new TemplateContext(new Dictionary<string, object?>());
        var result = await engine.RenderStringAsync("{{ missingVar }}", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Render_VeryLongVariable_Renders()
    {
        var longValue = new string('a', 10000);
        var engine = CreateEngine(new() { ["test"] = "{{ value }}" });
        var context = new TemplateContext(new Dictionary<string, object?> { ["value"] = longValue });
        var result = await engine.RenderStringAsync("{{ value }}", context);
        Assert.Equal(longValue, result);
    }

    [Fact]
    public async Task Render_EscapeAllCharacters_AllEscaped()
    {
        var engine = CreateEngine();
        var data = new Dictionary<string, object?> { ["html"] = "<div>&'\"</div>" };
        var context = new TemplateContext(data);
        var result = await engine.RenderStringAsync("{{ html }}", context);
        Assert.Contains("&lt;", result);
        Assert.Contains("&gt;", result);
        Assert.Contains("&amp;", result);
        Assert.Contains("&quot;", result);
        Assert.Contains("&#x27;", result);
    }

    [Fact]
    public async Task Render_LoopEmptyCollection_RendersEmpty()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["items"] = Array.Empty<object>() });
        var result = await engine.RenderStringAsync("{{#each items as item }}X{{/each}}", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Render_LoopSingleItem_RendersSingleItem()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["items"] = new[] { "a" } });
        var result = await engine.RenderStringAsync("{{#each items as item }}{{ item }}{{/each}}", context);
        Assert.Equal("a", result);
    }

    [Fact]
    public async Task Render_LoopLargeCollection_RendersAll()
    {
        var items = Enumerable.Range(0, 10000).ToList();
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["items"] = items });
        var result = await engine.RenderStringAsync("{{#each items as item }}x{{/each}}", context);
        Assert.Equal(10000, result.Length);
    }

    [Fact]
    public async Task Render_DeepNesting_RendersCorrectly()
    {
        var engine = CreateEngine();
        var data = new Dictionary<string, object?>
        {
            ["a"] = new { b = new { c = new { d = new { e = "value" } } } }
        };
        var context = new TemplateContext(data);
        var result = await engine.RenderStringAsync("{{ a.b.c.d.e }}", context);
        Assert.Equal("value", result);
    }

    [Fact]
    public async Task Render_ZeroIsFalsy_ConditionFalse()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["zero"] = 0 });
        var result = await engine.RenderStringAsync("{{#if zero }}yes{{#else}}no{{/if}}", context);
        Assert.Equal("no", result);
    }

    [Fact]
    public async Task Render_EmptyStringIsFalsy_ConditionFalse()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["empty"] = "" });
        var result = await engine.RenderStringAsync("{{#if empty }}yes{{#else}}no{{/if}}", context);
        Assert.Equal("no", result);
    }

    [Fact]
    public async Task Render_FalseIsFalsy_ConditionFalse()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["false"] = false });
        var result = await engine.RenderStringAsync("{{#if false }}yes{{#else}}no{{/if}}", context);
        Assert.Equal("no", result);
    }

    [Fact]
    public async Task Render_ZeroPointZeroIsFalsy_ConditionFalse()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["zero"] = 0.0 });
        var result = await engine.RenderStringAsync("{{#if zero }}yes{{#else}}no{{/if}}", context);
        Assert.Equal("no", result);
    }

    [Fact]
    public async Task Render_NegativeNumberIsTruthy_ConditionTrue()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["num"] = -1 });
        var result = await engine.RenderStringAsync("{{#if num }}yes{{#else}}no{{/if}}", context);
        Assert.Equal("yes", result);
    }

    [Fact]
    public async Task Render_StringZeroIsTruthy_ConditionTrue()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["str"] = "0" });
        var result = await engine.RenderStringAsync("{{#if str }}yes{{#else}}no{{/if}}", context);
        Assert.Equal("yes", result);
    }

    [Fact]
    public async Task Render_CaseSensitiveComparison_Distinguishes()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["str"] = "Hello" });
        var result1 = await engine.RenderStringAsync("{{#if str == \"Hello\" }}yes{{/if}}", context);
        var result2 = await engine.RenderStringAsync("{{#if str == \"hello\" }}yes{{/if}}", context);
        Assert.Equal("yes", result1);
        Assert.Empty(result2);
    }

    [Fact]
    public async Task Render_UnicodeCharacters_Renders()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["emoji"] = "😀🎉" });
        var result = await engine.RenderStringAsync("{{ emoji }}", context);
        Assert.Equal("😀🎉", result);
    }

    [Fact]
    public async Task Render_MultipleConsecutiveVariables_Renders()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> 
        { 
            ["a"] = "x", 
            ["b"] = "y", 
            ["c"] = "z" 
        });
        var result = await engine.RenderStringAsync("{{ a }}{{ b }}{{ c }}", context);
        Assert.Equal("xyz", result);
    }

    [Fact]
    public async Task Render_ManyElseIfBranches_EvaluatesCorrect()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["val"] = 3 });
        var template = "{{#if val == 1 }}one{{#elseif val == 2 }}two{{#elseif val == 3 }}three{{#elseif val == 4 }}four{{#else}}other{{/if}}";
        var result = await engine.RenderStringAsync(template, context);
        Assert.Equal("three", result);
    }

    [Fact]
    public async Task Render_StringWithVariableDelimiters_Rendered()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["str"] = "{{ not a variable }}" });
        var result = await engine.RenderStringAsync("{{ str }}", context);
        Assert.Equal("{{ not a variable }}", result);
    }

    [Fact]
    public async Task Render_GlobalsAndDataSameName_DataTakesPriority()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(
            new Dictionary<string, object?> { ["name"] = "data" },
            new Dictionary<string, object?> { ["name"] = "global" }
        );
        var result = await engine.RenderStringAsync("{{ name }}", context);
        Assert.Equal("data", result);
    }

    [Fact]
    public async Task Render_NestedLoops_RendersCorrectly()
    {
        var engine = CreateEngine();
        var data = new Dictionary<string, object?>
        {
            ["outer"] = new[] 
            { 
                new { items = new[] { "a", "b" } }, 
                new { items = new[] { "c", "d" } } 
            }
        };
        var context = new TemplateContext(data);
        var result = await engine.RenderStringAsync(
            "{{#each outer as o }}{{#each o.items as i }}{{ i }}{{/each}}-{{/each}}",
            context
        );
        Assert.Equal("ab-cd-", result);
    }

    [Fact]
    public async Task Render_CommentWithDelimiters_Stripped()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>());
        var result = await engine.RenderStringAsync("before{{-- {{ variable }} --}}after", context);
        Assert.Equal("beforeafter", result);
    }

    [Fact]
    public async Task Render_DateTimeNull_RendersEmpty()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["date"] = null });
        var result = await engine.RenderStringAsync("{{ date date \"yyyy-MM-dd\" }}", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Render_LoopIndexInsideLoop_IsCorrect()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["items"] = new[] { "a", "b", "c" } });
        var result = await engine.RenderStringAsync("{{#each items as item }}{{ item.index }}{{/each}}", context);
        Assert.Equal("012", result);
    }

    [Fact]
    public async Task Render_ArrayIndexAccess_Works()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["arr"] = new[] { "first", "second", "third" } });
        var result = await engine.RenderStringAsync("{{ arr[1] }}", context);
        Assert.Equal("second", result);
    }

    [Fact]
    public async Task Render_ArrayIndexOutOfBounds_RendersEmpty()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["arr"] = new[] { "a" } });
        var result = await engine.RenderStringAsync("{{ arr[5] }}", context);
        Assert.Empty(result);
    }
}

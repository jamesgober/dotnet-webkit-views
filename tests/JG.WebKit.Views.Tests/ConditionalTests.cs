namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class ConditionalTests
{
    [Fact]
    public async Task Render_IfTrue_RendersTrueBlock()
    {
        var data = new Dictionary<string, object?> { ["isAdmin"] = true };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if isAdmin }}Admin{{/if}}", context);

        result.Should().Be("Admin");
    }

    [Fact]
    public async Task Render_IfFalse_RendersNothing()
    {
        var data = new Dictionary<string, object?> { ["isAdmin"] = false };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if isAdmin }}Admin{{/if}}", context);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Render_IfElse_RendersElseBlockWhenConditionFalse()
    {
        var data = new Dictionary<string, object?> { ["isAdmin"] = false };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if isAdmin }}Admin{{#else}}User{{/if}}", context);

        result.Should().Be("User");
    }

    [Fact]
    public async Task Render_IfElseIf_RendersElseIfBlockWhenConditionMatches()
    {
        var data = new Dictionary<string, object?> { ["role"] = "editor" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var template = "{{#if role == \"admin\" }}Admin{{#elseif role == \"editor\" }}Editor{{/if}}";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Be("Editor");
    }

    [Fact]
    public async Task Render_Negation_InvertsCondition()
    {
        var data = new Dictionary<string, object?> { ["isAdmin"] = false };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if !isAdmin }}Not Admin{{/if}}", context);

        result.Should().Be("Not Admin");
    }

    [Fact]
    public async Task Render_EqualityOperator_ComparesTruthfully()
    {
        var data = new Dictionary<string, object?> { ["status"] = "active" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if status == \"active\" }}Active{{/if}}", context);

        result.Should().Be("Active");
    }

    [Fact]
    public async Task Render_InequalityOperator_ComparesTruthfully()
    {
        var data = new Dictionary<string, object?> { ["status"] = "inactive" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if status != \"active\" }}Not Active{{/if}}", context);

        result.Should().Be("Not Active");
    }

    [Fact]
    public async Task Render_GreaterThanOperator_ComparesTruthfully()
    {
        var data = new Dictionary<string, object?> { ["count"] = 10 };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if count > 5 }}High{{/if}}", context);

        result.Should().Be("High");
    }

    [Fact]
    public async Task Render_LessThanOperator_ComparesTruthfully()
    {
        var data = new Dictionary<string, object?> { ["count"] = 3 };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if count < 5 }}Low{{/if}}", context);

        result.Should().Be("Low");
    }

    [Fact]
    public async Task Render_GreaterThanOrEqualOperator_ComparesTruthfully()
    {
        var data = new Dictionary<string, object?> { ["count"] = 5 };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if count >= 5 }}Valid{{/if}}", context);

        result.Should().Be("Valid");
    }

    [Fact]
    public async Task Render_LessThanOrEqualOperator_ComparesTruthfully()
    {
        var data = new Dictionary<string, object?> { ["count"] = 5 };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if count <= 5 }}Valid{{/if}}", context);

        result.Should().Be("Valid");
    }

    [Fact]
    public async Task Render_TruthyNull_IsFalsy()
    {
        var data = new Dictionary<string, object?> { ["value"] = null };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if value }}Truthy{{#else}}Falsy{{/if}}", context);

        result.Should().Be("Falsy");
    }

    [Fact]
    public async Task Render_TruthyZero_IsFalsy()
    {
        var data = new Dictionary<string, object?> { ["value"] = 0 };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if value }}Truthy{{#else}}Falsy{{/if}}", context);

        result.Should().Be("Falsy");
    }

    [Fact]
    public async Task Render_TruthyEmptyString_IsFalsy()
    {
        var data = new Dictionary<string, object?> { ["value"] = "" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if value }}Truthy{{#else}}Falsy{{/if}}", context);

        result.Should().Be("Falsy");
    }

    [Fact]
    public async Task Render_TruthyNonEmptyString_IsTruthy()
    {
        var data = new Dictionary<string, object?> { ["value"] = "text" };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if value }}Truthy{{#else}}Falsy{{/if}}", context);

        result.Should().Be("Truthy");
    }

    [Fact]
    public async Task Render_TruthyEmptyArray_IsFalsy()
    {
        var data = new Dictionary<string, object?> { ["items"] = Array.Empty<object>() };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if items }}Has Items{{#else}}Empty{{/if}}", context);

        result.Should().Be("Empty");
    }

    [Fact]
    public async Task Render_TruthyNonEmptyArray_IsTruthy()
    {
        var data = new Dictionary<string, object?> { ["items"] = new[] { 1, 2, 3 } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var result = await engine.RenderStringAsync("{{#if items }}Has Items{{#else}}Empty{{/if}}", context);

        result.Should().Be("Has Items");
    }

    [Fact]
    public async Task Render_NestedConditionals_WorkCorrectly()
    {
        var data = new Dictionary<string, object?> { ["user"] = new { isAdmin = true, isBanned = false } };
        var context = new TemplateContext(data);

        var engine = CreateEngine();
        var template = "{{#if user }}{{#if user.isAdmin }}Admin{{/if}}{{/if}}";
        var result = await engine.RenderStringAsync(template, context);

        result.Should().Be("Admin");
    }

    private static IViewEngine CreateEngine(ViewEngineOptions? options = null)
    {
        options ??= new ViewEngineOptions();
        var provider = new InMemoryTemplateProvider();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

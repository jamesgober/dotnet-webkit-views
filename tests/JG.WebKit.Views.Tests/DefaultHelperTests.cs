using JG.WebKit.Views;
using JG.WebKit.Views.Helpers;
using Xunit;

namespace JG.WebKit.Views.Tests;

public class DefaultHelperTests
{
    private static IViewEngine CreateEngine()
    {
        var provider = new Providers.InMemoryTemplateProvider();
        var options = new ViewEngineOptions();
        var helpers = new Dictionary<string, ITemplateHelper>
        {
            ["default"] = new DefaultHelper(),
            ["ifval"] = new IfValHelper(),
            ["concat"] = new ConcatHelper(),
            ["replace"] = new ReplaceHelper(),
            ["count"] = new CountHelper()
        };
        return new Internal.ViewEngine(provider, options, helpers);
    }

    #region Default Helper Tests

    [Fact]
    public async Task Default_ValueExists_ReturnsValue()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["title"] = "My Page" });
        var result = await engine.RenderStringAsync("{{ default title \"Untitled\" }}", context);
        Assert.Equal("My Page", result);
    }

    [Fact]
    public async Task Default_ValueIsNull_ReturnsFallback()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["title"] = null });
        var result = await engine.RenderStringAsync("{{ default title \"Untitled\" }}", context);
        Assert.Equal("Untitled", result);
    }

    [Fact]
    public async Task Default_ValueIsEmptyString_ReturnsFallback()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["title"] = "" });
        var result = await engine.RenderStringAsync("{{ default title \"Untitled\" }}", context);
        Assert.Equal("Untitled", result);
    }

    [Fact]
    public async Task Default_ValueIsWhitespace_ReturnsFallback()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["title"] = "   " });
        var result = await engine.RenderStringAsync("{{ default title \"Untitled\" }}", context);
        Assert.Equal("Untitled", result);
    }

    [Fact]
    public async Task Default_ValueIsZero_ReturnsZero()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["count"] = 0 });
        var result = await engine.RenderStringAsync("{{ default count \"None\" }}", context);
        Assert.Equal("0", result);
    }

    [Fact]
    public async Task Default_ValueIsFalse_ReturnsFalse()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["flag"] = false });
        var result = await engine.RenderStringAsync("{{ default flag \"Not Set\" }}", context);
        Assert.Equal("False", result);
    }

    [Fact]
    public async Task Default_NoFallbackProvided_ReturnsEmptyString()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["title"] = null });
        var result = await engine.RenderStringAsync("{{ default title }}", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Default_NestedPropertyNull_ReturnsFallback()
    {
        var engine = CreateEngine();
        var post = new { title = (string?)null };
        var context = new TemplateContext(new Dictionary<string, object?> { ["post"] = post });
        var result = await engine.RenderStringAsync("{{ default post.title \"No Title\" }}", context);
        Assert.Equal("No Title", result);
    }

    [Fact]
    public async Task Default_ObjectItselfNull_ReturnsFallback()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["post"] = null });
        var result = await engine.RenderStringAsync("{{ default post.title \"No Title\" }}", context);
        Assert.Equal("No Title", result);
    }

    #endregion

    #region IfVal Helper Tests

    [Fact]
    public async Task IfVal_TruthyValue_ReturnsSecondArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["isAdmin"] = true });
        var result = await engine.RenderStringAsync("{{ ifval isAdmin \"Admin\" \"User\" }}", context);
        Assert.Equal("Admin", result);
    }

    [Fact]
    public async Task IfVal_FalsyValue_ReturnsThirdArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["isAdmin"] = false });
        var result = await engine.RenderStringAsync("{{ ifval isAdmin \"Admin\" \"User\" }}", context);
        Assert.Equal("User", result);
    }

    [Fact]
    public async Task IfVal_NullValue_ReturnsThirdArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["value"] = null });
        var result = await engine.RenderStringAsync("{{ ifval value \"Yes\" \"No\" }}", context);
        Assert.Equal("No", result);
    }

    [Fact]
    public async Task IfVal_BooleanTrue_ReturnsSecondArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["flag"] = true });
        var result = await engine.RenderStringAsync("{{ ifval flag \"Enabled\" \"Disabled\" }}", context);
        Assert.Equal("Enabled", result);
    }

    [Fact]
    public async Task IfVal_Zero_ReturnsThirdArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["count"] = 0 });
        var result = await engine.RenderStringAsync("{{ ifval count \"Has Items\" \"Empty\" }}", context);
        Assert.Equal("Empty", result);
    }

    [Fact]
    public async Task IfVal_NonZeroNumber_ReturnsSecondArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["count"] = 5 });
        var result = await engine.RenderStringAsync("{{ ifval count \"Has Items\" \"Empty\" }}", context);
        Assert.Equal("Has Items", result);
    }

    [Fact]
    public async Task IfVal_NonEmptyString_ReturnsSecondArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["name"] = "John" });
        var result = await engine.RenderStringAsync("{{ ifval name \"Named\" \"Anonymous\" }}", context);
        Assert.Equal("Named", result);
    }

    [Fact]
    public async Task IfVal_EmptyString_ReturnsThirdArg()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["name"] = "" });
        var result = await engine.RenderStringAsync("{{ ifval name \"Named\" \"Anonymous\" }}", context);
        Assert.Equal("Anonymous", result);
    }

    #endregion

    #region Concat Helper Tests

    [Fact]
    public async Task Concat_TwoStrings_Concatenated()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        });
        var result = await engine.RenderStringAsync("{{ concat firstName lastName }}", context);
        Assert.Equal("JohnDoe", result);
    }

    [Fact]
    public async Task Concat_ThreeStringsWithSeparator_Concatenated()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        });
        var result = await engine.RenderStringAsync("{{ concat firstName \" \" lastName }}", context);
        Assert.Equal("John Doe", result);
    }

    [Fact]
    public async Task Concat_NullArgument_TreatedAsEmpty()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>
        {
            ["firstName"] = "John",
            ["middleName"] = null,
            ["lastName"] = "Doe"
        });
        var result = await engine.RenderStringAsync("{{ concat firstName middleName lastName }}", context);
        Assert.Equal("JohnDoe", result);
    }

    [Fact]
    public async Task Concat_SingleArgument_ReturnsAsIs()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["name"] = "John" });
        var result = await engine.RenderStringAsync("{{ concat name }}", context);
        Assert.Equal("John", result);
    }

    [Fact]
    public async Task Concat_NoArguments_ReturnsEmpty()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>());
        var result = await engine.RenderStringAsync("{{ concat }}", context);
        Assert.Empty(result);
    }

    #endregion

    #region Replace Helper Tests

    [Fact]
    public async Task Replace_BasicReplacement_Works()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["title"] = "Hello-World" });
        var result = await engine.RenderStringAsync("{{ replace title \"-\" \" \" }}", context);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task Replace_MultipleOccurrences_Replaced()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["text"] = "a-b-c-d" });
        var result = await engine.RenderStringAsync("{{ replace text \"-\" \"_\" }}", context);
        Assert.Equal("a_b_c_d", result);
    }

    [Fact]
    public async Task Replace_NoMatch_OriginalReturned()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["text"] = "Hello World" });
        var result = await engine.RenderStringAsync("{{ replace text \"x\" \"y\" }}", context);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task Replace_NullValue_ReturnsEmpty()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["text"] = null });
        var result = await engine.RenderStringAsync("{{ replace text \"-\" \" \" }}", context);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Replace_EmptySearch_ReturnsOriginal()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["text"] = "Hello" });
        var result = await engine.RenderStringAsync("{{ replace text \"\" \"x\" }}", context);
        Assert.Equal("Hello", result);
    }

    #endregion

    #region Count Helper Tests

    [Fact]
    public async Task Count_ArrayWithItems_ReturnsCount()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>
        {
            ["items"] = new[] { "a", "b", "c" }
        });
        var result = await engine.RenderStringAsync("{{ count items }}", context);
        Assert.Equal("3", result);
    }

    [Fact]
    public async Task Count_EmptyArray_ReturnsZero()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>
        {
            ["items"] = Array.Empty<string>()
        });
        var result = await engine.RenderStringAsync("{{ count items }}", context);
        Assert.Equal("0", result);
    }

    [Fact]
    public async Task Count_Null_ReturnsZero()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["items"] = null });
        var result = await engine.RenderStringAsync("{{ count items }}", context);
        Assert.Equal("0", result);
    }

    [Fact]
    public async Task Count_NonCollection_ReturnsZero()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["value"] = 42 });
        var result = await engine.RenderStringAsync("{{ count value }}", context);
        Assert.Equal("0", result);
    }

    [Fact]
    public async Task Count_String_ReturnsZero()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?> { ["text"] = "Hello" });
        var result = await engine.RenderStringAsync("{{ count text }}", context);
        Assert.Equal("0", result);
    }

    [Fact]
    public async Task Count_List_ReturnsCount()
    {
        var engine = CreateEngine();
        var context = new TemplateContext(new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3, 4, 5 }
        });
        var result = await engine.RenderStringAsync("{{ count items }}", context);
        Assert.Equal("5", result);
    }

    #endregion
}

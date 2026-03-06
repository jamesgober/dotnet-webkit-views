namespace JG.WebKit.Views.Tests;

using FluentAssertions;
using Xunit;

public class IntegrationTests
{
    [Fact]
    public async Task Integration_PartialWithContextChange_RendersFull()
    {
        var provider = new InMemoryTemplateProvider();
        provider.AddTemplate("header", "<header>{{ siteName }}</header>");
        provider.AddTemplate("footer", "<footer>© 2026</footer>");

        var data = new Dictionary<string, object?>();
        var globals = new Dictionary<string, object?> { ["siteName"] = "My Site" };
        var context = new TemplateContext(data, globals);

        var engine = CreateEngine(provider);
        var template = "{{> header }}<main>Content</main>{{> footer }}";

        var result = await engine.RenderStringAsync(template, context);

        result.Should().Contain("<header>My Site</header>");
        result.Should().Contain("<footer>© 2026</footer>");
    }

    private static IViewEngine CreateEngine(ITemplateProvider? provider = null)
    {
        provider ??= new InMemoryTemplateProvider();
        var options = new ViewEngineOptions();
        var helpers = new Dictionary<string, ITemplateHelper>();
        return new Internal.ViewEngine(provider, options, helpers);
    }
}

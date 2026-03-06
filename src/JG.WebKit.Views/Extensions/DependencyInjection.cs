namespace Microsoft.Extensions.DependencyInjection;

using global::JG.WebKit.Views;
using global::JG.WebKit.Views.Abstractions;
using global::JG.WebKit.Views.Helpers;
using global::JG.WebKit.Views.Internal;
using global::JG.WebKit.Views.Providers;

/// <summary>
/// Extension methods for registering JG.WebKit.Views services.
/// </summary>
public static class WebKitViewsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the WebKit Views template engine to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddWebKitViews(
        this IServiceCollection services,
        Action<ViewEngineOptions>? configure = null)
    {
        var options = new ViewEngineOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        
        var basePath = Path.GetFullPath(options.TemplatePath);
        services.AddSingleton<ITemplateProvider>(new FileTemplateProvider(basePath, options));

        services.AddSingleton<ITemplateHelper>(new DateHelper());
        services.AddSingleton<ITemplateHelper>(new TruncateHelper());
        services.AddSingleton<ITemplateHelper>(new UppercaseHelper());
        services.AddSingleton<ITemplateHelper>(new LowercaseHelper());
        services.AddSingleton<ITemplateHelper>(new JsonHelper());
        services.AddSingleton<ITemplateHelper>(new AssetHelper(options));
        services.AddSingleton<ITemplateHelper>(new ImageHelper(options));
        services.AddSingleton<ITemplateHelper>(new ScriptHelper(options));
        services.AddSingleton<ITemplateHelper>(new FontHelper(options));
        services.AddSingleton<ITemplateHelper>(new MediaHelper(options));
        services.AddSingleton<ITemplateHelper>(new DefaultHelper());
        services.AddSingleton<ITemplateHelper>(new IfValHelper());
        services.AddSingleton<ITemplateHelper>(new ConcatHelper());
        services.AddSingleton<ITemplateHelper>(new ReplaceHelper());
        services.AddSingleton<ITemplateHelper>(new CountHelper());

        services.AddSingleton<IViewEngine>(sp => CreateViewEngine(sp, options));

        return services;
    }

    /// <summary>
    /// Registers a custom template provider.
    /// </summary>
    /// <typeparam name="T">The template provider type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTemplateProvider<T>(this IServiceCollection services)
        where T : class, ITemplateProvider
    {
        services.AddSingleton<ITemplateProvider, T>();
        return services;
    }

    /// <summary>
    /// Registers a custom template helper.
    /// </summary>
    /// <typeparam name="T">The helper type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTemplateHelper<T>(this IServiceCollection services)
        where T : class, ITemplateHelper
    {
        services.AddSingleton<ITemplateHelper, T>();
        return services;
    }

    private static ViewEngine CreateViewEngine(IServiceProvider sp, ViewEngineOptions options)
    {
        var provider = sp.GetRequiredService<ITemplateProvider>();
        var helpers = sp.GetServices<ITemplateHelper>();
        var helperDict = helpers.ToDictionary(h => h.Name);

        return new ViewEngine(provider, options, helperDict);
    }
}

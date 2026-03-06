# API Reference 
 
_Documentation will be generated when the library is built._

# JG.WebKit.Views API Reference

## Core Interfaces

### IViewEngine

The main interface for rendering templates.

```csharp
public interface IViewEngine : IDisposable
{
    ValueTask<string> RenderAsync(string templatePath, TemplateContext context, CancellationToken ct = default);
    ValueTask<string> RenderStringAsync(string templateContent, TemplateContext context, CancellationToken ct = default);
    void InvalidateCache(string? templatePath = null);
}
```

**Usage:**

```csharp
var context = new TemplateContext(
    new Dictionary<string, object?> { ["title"] = "Hello" }
);

var html = await engine.RenderAsync("index", context);
```

### ITemplateProvider

Abstraction for template source (file, database, memory, etc.).

```csharp
public interface ITemplateProvider
{
    ValueTask<TemplateSource?> GetTemplateAsync(string path, CancellationToken ct = default);
    bool SupportsHotReload { get; }
}
```

**Built-in Implementations:**

- **FileTemplateProvider** - Reads from disk, supports hot-reload
- **InMemoryTemplateProvider** - Stores templates in dictionary

**Custom Implementation Example:**

```csharp
public class DatabaseTemplateProvider : ITemplateProvider
{
    private readonly IDbContext _db;

    public bool SupportsHotReload => false;

    public async ValueTask<TemplateSource?> GetTemplateAsync(string path, CancellationToken ct)
    {
        var template = await _db.Templates
            .Where(t => t.Path == path && t.Active)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (template == null) return null;

        return new TemplateSource
        {
            Path = path,
            Content = template.Content,
            LastModified = template.UpdatedAt
        };
    }
}

services.AddTemplateProvider<DatabaseTemplateProvider>();
```

### ITemplateHelper

Custom helper functions in templates.

```csharp
public interface ITemplateHelper
{
    string Name { get; }
    string Execute(object?[] arguments, TemplateContext context);
}
```

**Example:**

```csharp
public class SlugifyHelper : ITemplateHelper
{
    public string Name => "slugify";

    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1) return string.Empty;
        var text = arguments[0]?.ToString() ?? string.Empty;
        return text.ToLowerInvariant().Trim().Replace(" ", "-");
    }
}

services.AddTemplateHelper<SlugifyHelper>();
```

Use in template:
```html
<h1>{{ slugify post.title }}</h1>
```

## Core Types

### TemplateContext

```csharp
public sealed class TemplateContext
{
    public IReadOnlyDictionary<string, object?> Data { get; }
    public IReadOnlyDictionary<string, object?> Globals { get; }
    public HttpContext? HttpContext { get; }

    public TemplateContext(
        IReadOnlyDictionary<string, object?> data,
        IReadOnlyDictionary<string, object?>? globals = null,
        HttpContext? httpContext = null);
}
```

- **Data**: Template-specific model
- **Globals**: Site-wide data (config, constants)
- **HttpContext**: Available in ASP.NET Core scenarios

### TemplateSource

```csharp
public sealed class TemplateSource
{
    public required string Content { get; init; }
    public required string Path { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}
```

### ViewEngineOptions

```csharp
public sealed class ViewEngineOptions
{
    public string TemplatePath { get; set; } = "templates";
    public string LayoutPath { get; set; } = "templates/layouts";
    public string PartialPath { get; set; } = "templates/partials";
    public bool CacheCompiledTemplates { get; set; } = true;
    public bool EnableHotReload { get; set; } = false;
    public string DefaultLayout { get; set; } = "main";
    public string TemplateExtension { get; set; } = ".tpl";
    public bool AllowRawOutput { get; set; } = false;
    public int MaxIncludeDepth { get; set; } = 10;
    public AssetOptions Assets { get; } = new();
}
```

### AssetOptions

```csharp
public sealed class AssetOptions
{
    public string BasePath { get; set; } = "/assets";
    public string Images { get; set; } = "/assets/images";
    public string Styles { get; set; } = "/assets/css";
    public string Scripts { get; set; } = "/assets/js";
    public string Fonts { get; set; } = "/assets/fonts";
    public string Media { get; set; } = "/assets/media";
    public string? CdnBaseUrl { get; set; }
    public bool AppendVersionHash { get; set; } = false;
}
```

## Dependency Injection

### Registration

```csharp
services.AddWebKitViews(options =>
{
    options.TemplatePath = "views";
    options.EnableHotReload = env.IsDevelopment();
    options.Assets.CdnBaseUrl = "https://cdn.example.com";
});

services.AddTemplateProvider<DatabaseTemplateProvider>();
services.AddTemplateHelper<SlugifyHelper>();
```

### Usage in Controller

```csharp
public class PageController : Controller
{
    private readonly IViewEngine _engine;

    public PageController(IViewEngine engine)
    {
        _engine = engine;
    }

    public async Task<IActionResult> Index(string slug)
    {
        var page = await _db.Pages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page == null) return NotFound();

        var context = new TemplateContext(
            new Dictionary<string, object?> { ["page"] = page },
            new Dictionary<string, object?> { ["siteName"] = "My Site" }
        );

        var html = await _engine.RenderAsync("page", context);
        return Content(html, "text/html");
    }
}
```

## Built-in Helpers

### date

Formats a DateTime or DateTimeOffset.

```html
{{ date createdAt "yyyy-MM-dd" }}
{{ date now "dddd, MMMM d, yyyy" }}
```

### truncate

Truncates string to max length with ellipsis.

```html
{{ truncate description 100 }}
```

### uppercase / lowercase

```html
{{ uppercase title }}
{{ lowercase slug }}
```

### json

JSON serialization for script tags.

```html
<script>
const data = {{ json payload }};
</script>
```

### Asset Helpers

```html
{{ asset "main.css" }}        <!-- /assets/main.css -->
{{ image "logo.png" }}        <!-- /assets/images/logo.png -->
{{ script "app.js" }}         <!-- /assets/js/app.js -->
{{ font "Inter.woff2" }}      <!-- /assets/fonts/Inter.woff2 -->
{{ media "video.mp4" }}       <!-- /assets/media/video.mp4 -->
```

With CDN:
```html
{{ asset "main.css" }}  <!-- https://cdn.example.com/assets/main.css -->
```

With version hashing:
```html
{{ asset "main.css" }}  <!-- /assets/main.css?v=a1b2c3d4 -->
```

## Template Syntax

### Variables

```html
{{ title }}              <!-- HTML-escaped -->
{{{ html }}}             <!-- Raw output (only if allowed) -->
{{ post.title }}         <!-- Property access -->
{{ items[0] }}           <!-- Array indexing -->
{{ config.api.url }}     <!-- Nested properties -->
```

Variable resolution order:
1. Template data
2. Globals
3. Null if not found

### Conditionals

```html
{{#if isPublished }}
  <article>{{ content }}</article>
{{#elseif isDraft }}
  <article class="draft">{{ content }}</article>
{{#else}}
  <p>Archived</p>
{{/if}}
```

Comparison operators:
- `==` - equality (string or numeric)
- `!=` - inequality
- `>` - greater than (numeric)
- `<` - less than (numeric)
- `>=` - greater or equal
- `<=` - less or equal
- `!` - negation prefix

```html
{{#if status == "active" }}
{{#if count > 5 }}
{{#if !archived }}
```

### Loops

```html
{{#each posts as post }}
  <article>
    <h2>{{ post.title }}</h2>
    <span>{{ post.index }}</span>  <!-- auto-injected index -->
  </article>
{{#empty}}
  <p>No posts</p>
{{/each}}
```

The `post.index` variable is automatically available (zero-based).

### Partials

```html
{{> header }}                    <!-- partials/header.tpl -->
{{> header "dark" }}             <!-- partials/header-dark.tpl with fallback -->
{{> card post }}                 <!-- partials/card.tpl with post as root -->
```

Variant fallback: `{{> name "variant"}}` tries `name-variant.tpl` first, falls back to `name.tpl`.

### Layouts

**page.tpl:**
```html
{{#layout "main" }}

<h1>{{ title }}</h1>

{{#section "sidebar" }}
  <nav>Navigation</nav>
{{/section}}
```

**layouts/main.tpl:**
```html
<!DOCTYPE html>
<html>
  <body>
    {{> header }}
    <main>
      {{#yield "content" }}
    </main>
    <aside>
      {{#yield "sidebar" }}
      {{#yield-default "sidebar" }}
        <p>Default sidebar</p>
      {{/yield-default}}
    </aside>
  </body>
</html>
```

The page body becomes the implicit "content" section. Named sections are rendered where `{{#yield}}` appears.

### Comments

```html
{{-- This is stripped from output --}}
```

## Truthiness

A value is falsy if it is:
- `null`
- `false`
- `0` (any numeric type)
- Empty string `""`
- Empty collection

Everything else is truthy.

```html
{{#if user }}              <!-- truthy if not null -->
{{#if count }}             <!-- truthy if count > 0 -->
{{#if items }}             <!-- truthy if has elements -->
{{#if !archived }}         <!-- negation -->
```

## Security

### HTML Escaping

All variables are HTML-escaped by default:

```html
{{ userInput }}  <!-- < becomes &lt; -->
```

Escaped characters:
- `&` → `&amp;`
- `<` → `&lt;`
- `>` → `&gt;`
- `"` → `&quot;`
- `'` → `&#x27;`

### Raw Output

Raw output `{{{ }}}` is disabled by default:

```csharp
options.AllowRawOutput = true;  // Only if needed
```

Only use for trusted content.

### Include Depth

Maximum nesting level prevents recursion:

```csharp
options.MaxIncludeDepth = 10;  // Default
```

## Caching

Compiled templates are cached by default.

### Manual Invalidation

```csharp
// Invalidate specific template
engine.InvalidateCache("page");

// Invalidate all templates
engine.InvalidateCache();
```

### Hot-Reload

Enable in development:

```csharp
options.EnableHotReload = env.IsDevelopment();
```

FileTemplateProvider automatically watches for changes.

## Error Handling

### Graceful Degradation

These render as empty (no error):
- Missing variable
- Missing partial
- Null value
- Type mismatch

### Exceptions Thrown

These throw exceptions:
- Malformed template syntax (parse time)
- Include depth exceeded
- Null template path or context

## Performance

Typical benchmarks:
- First render (compilation + execution): ~50ms
- Cached renders: ~3-5ms

Optimizations:
- Compiled to cached delegates
- StringBuilder pre-sized from literals
- No reflection in render path (PropertyInfo cached)
- No LINQ in hot paths
- Span-based HTML escaping

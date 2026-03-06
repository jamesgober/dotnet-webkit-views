# JG.WebKit.Views Getting Started Guide

## Installation

```bash
dotnet add package JG.WebKit.Views
```

## Basic Setup

### 1. Configure Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebKitViews(options =>
{
    options.TemplatePath = "views";
    options.EnableHotReload = builder.Environment.IsDevelopment();
});

var app = builder.Build();
```

### 2. Create Template Directory

```
project/
├── views/
│   ├── page.tpl
│   ├── post.tpl
│   ├── layouts/
│   │   └── main.tpl
│   └── partials/
│       ├── header.tpl
│       ├── footer.tpl
│       └── sidebar.tpl
```

### 3. Create a Template

**views/page.tpl:**
```html
{{#layout "main" }}

<article>
  <h1>{{ title }}</h1>
  <time>{{ date createdAt "MMMM d, yyyy" }}</time>
  <div>{{ description }}</div>
</article>
```

**views/layouts/main.tpl:**
```html
<!DOCTYPE html>
<html>
<head>
  <title>{{ title }} - {{ siteName }}</title>
</head>
<body>
  {{> header }}
  <main>
    {{#yield "content" }}
  </main>
  {{> footer }}
</body>
</html>
```

**views/partials/header.tpl:**
```html
<header>
  <h1>{{ siteName }}</h1>
  <nav>
    <a href="/">Home</a>
    <a href="/about">About</a>
  </nav>
</header>
```

### 4. Use in Controller

```csharp
using JG.WebKit.Views;

public class PageController : ControllerBase
{
    private readonly IViewEngine _engine;

    public PageController(IViewEngine engine)
    {
        _engine = engine;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        var post = await _db.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (post == null) return NotFound();

        var context = new TemplateContext(
            new Dictionary<string, object?>
            {
                ["title"] = post.Title,
                ["description"] = post.Description,
                ["createdAt"] = post.CreatedAt,
                ["author"] = post.Author
            },
            new Dictionary<string, object?>
            {
                ["siteName"] = "My Blog",
                ["siteUrl"] = "https://myblog.com"
            }
        );

        var html = await _engine.RenderAsync("post", context);
        return Content(html, "text/html");
    }
}
```

## Common Patterns

### Blog Post List

**Controller:**
```csharp
public async Task<IActionResult> Index()
{
    var posts = await _db.Posts
        .Where(p => p.IsPublished)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    var context = new TemplateContext(
        new Dictionary<string, object?> { ["posts"] = posts },
        new Dictionary<string, object?> { ["siteName"] = "My Blog" }
    );

    var html = await _engine.RenderAsync("posts/index", context);
    return Content(html, "text/html");
}
```

**Template (views/posts/index.tpl):**
```html
{{#layout "main" }}

<h1>Posts</h1>

{{#each posts as post }}
  <article>
    <h2>{{ post.Title }}</h2>
    <p>{{ post.Description }}</p>
    <time>{{ date post.CreatedAt "MMM d" }}</time>
    <a href="/posts/{{ post.Slug }}">Read more</a>
  </article>
{{#empty}}
  <p>No posts yet.</p>
{{/each}}
```

### Conditional Content

**Template:**
```html
{{#if isAdmin }}
  <a href="/admin">Admin Panel</a>
  <a href="/settings">Settings</a>
{{#elseif isEditor }}
  <a href="/editor">Editor</a>
{{#else}}
  <p>Welcome back!</p>
{{/if}}
```

**Controller:**
```csharp
var context = new TemplateContext(
    new Dictionary<string, object?>
    {
        ["isAdmin"] = user.Role == "Admin",
        ["isEditor"] = user.Role == "Editor"
    }
);
```

### Nested Data

**Template:**
```html
<div class="user">
  <h3>{{ user.name }}</h3>
  <p>{{ user.email }}</p>
  
  {{#each user.posts as post }}
    <article>
      <h4>{{ post.title }}</h4>
      <p>{{ post.excerpt }}</p>
    </article>
  {{#empty}}
    <p>No posts</p>
  {{/each}}
</div>
```

**Controller:**
```csharp
var user = new
{
    name = "Jane Doe",
    email = "jane@example.com",
    posts = new[]
    {
        new { title = "First Post", excerpt = "..." },
        new { title = "Second Post", excerpt = "..." }
    }
};

var context = new TemplateContext(
    new Dictionary<string, object?> { ["user"] = user }
);
```

### Reusable Partials

**views/partials/card.tpl:**
```html
<div class="card">
  <h3>{{ title }}</h3>
  <p>{{ excerpt }}</p>
  <a href="{{ url }}">View</a>
</div>
```

**Template (views/grid.tpl):**
```html
{{#layout "main" }}

<div class="grid">
  {{#each items as item }}
    {{> card item }}
  {{/each}}
</div>
```

**Variant fallback:**
```html
<!-- Try featured-card.tpl first, fall back to card.tpl -->
{{> card "featured" }}
```

### Responsive Partial Variants

**views/partials/image.tpl:**
```html
<img src="{{ image path }}" alt="{{ alt }}" />
```

**views/partials/image-responsive.tpl:**
```html
<picture>
  <source srcset="{{ image path }}-large.webp" media="(min-width: 1024px)" />
  <source srcset="{{ image path }}-medium.webp" media="(min-width: 640px)" />
  <img src="{{ image path }}.webp" alt="{{ alt }}" />
</picture>
```

**Template:**
```html
<!-- Uses image.tpl -->
{{> image hero }}

<!-- Uses image-responsive.tpl with fallback to image.tpl -->
{{> image "responsive" }}
```

## Custom Helpers

### Markdown Helper

```csharp
using Markdig;

public class MarkdownHelper : ITemplateHelper
{
    public string Name => "markdown";

    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1) return string.Empty;

        var markdown = arguments[0]?.ToString() ?? string.Empty;
        return Markdown.ToHtml(markdown);
    }
}

services.AddTemplateHelper<MarkdownHelper>();
```

**Usage:**
```html
<div class="content">
  {{{ markdown post.body }}}
</div>
```

### URL Helper

```csharp
public class UrlHelper : ITemplateHelper
{
    public string Name => "url";

    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1) return string.Empty;

        var route = arguments[0]?.ToString() ?? string.Empty;
        var parts = route.Split('/');

        // Simple example - use real routing in production
        return "/" + string.Join("/", parts);
    }
}

services.AddTemplateHelper<UrlHelper>();
```

**Usage:**
```html
<a href="{{ url posts/list }}">All Posts</a>
<a href="{{ url posts/@post.slug }}">{{ post.title }}</a>
```

### Time Ago Helper

```csharp
public class TimeAgoHelper : ITemplateHelper
{
    public string Name => "timeago";

    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments[0] is not DateTimeOffset dto)
            return string.Empty;

        var elapsed = DateTimeOffset.UtcNow - dto;

        return elapsed.TotalSeconds < 60 ? "just now" :
               elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes}m ago" :
               elapsed.TotalHours < 24 ? $"{(int)elapsed.TotalHours}h ago" :
               elapsed.TotalDays < 30 ? $"{(int)elapsed.TotalDays}d ago" :
               dto.ToString("MMM d, yyyy");
    }
}

services.AddTemplateHelper<TimeAgoHelper>();
```

**Usage:**
```html
<time>{{ timeago createdAt }}</time>
```

## Advanced Configuration

### CDN with Version Hashing

```csharp
services.AddWebKitViews(options =>
{
    options.Assets.CdnBaseUrl = "https://cdn.example.com";
    options.Assets.AppendVersionHash = true;
});
```

**Template:**
```html
<link rel="stylesheet" href="{{ asset "main.css" }}" />
<!-- Output: https://cdn.example.com/assets/main.css?v=a1b2c3d4 -->
```

### Database Templates

```csharp
public class DbTemplateProvider : ITemplateProvider
{
    private readonly IDbContext _db;

    public bool SupportsHotReload => false;

    public async ValueTask<TemplateSource?> GetTemplateAsync(string path, CancellationToken ct)
    {
        var template = await _db.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Path == path, ct)
            .ConfigureAwait(false);

        if (template?.IsActive != true) return null;

        return new TemplateSource
        {
            Path = path,
            Content = template.Content,
            LastModified = template.UpdatedAt
        };
    }
}

services.AddTemplateProvider<DbTemplateProvider>();
```

## Troubleshooting

### Template Not Found

If a template renders as empty:
1. Check the path (should be relative to TemplatePath)
2. Check the file extension (default: `.tpl`)
3. Verify the file exists
4. Check file permissions

### Expression Not Evaluating

Expressions use dot notation and are case-sensitive for POCO properties.

```html
<!-- Data must have "user" key -->
{{ user }}

<!-- user must have "Name" property (case-sensitive) -->
{{ user.Name }}

<!-- Dictionaries are case-insensitive -->
{{ user["name"] }}
```

### Missing Variable Renders Empty

By design, missing variables don't throw errors. Check:
- Variable name spelling
- Data structure in controller
- Property casing

### Performance Issues

Enable caching (default):
```csharp
options.CacheCompiledTemplates = true;
```

Disable hot-reload in production:
```csharp
options.EnableHotReload = env.IsDevelopment();
```

Monitor compiled template count if rendering many unique templates.

## Testing

### Unit Testing

```csharp
[Fact]
public async Task RenderPage_WithPostData_ContainsTitle()
{
    var provider = new InMemoryTemplateProvider();
    provider.AddTemplate("test", "{{ title }}");

    var engine = new ViewEngine(provider, new ViewEngineOptions(), new());
    var context = new TemplateContext(
        new Dictionary<string, object?> { ["title"] = "Hello" }
    );

    var result = await engine.RenderStringAsync("{{ title }}", context);

    Assert.Equal("Hello", result);
}
```

### Integration Testing

```csharp
[Fact]
public async Task RenderLayout_WithPartialAndSection_OutputsComplete()
{
    var provider = new InMemoryTemplateProvider();
    provider.AddTemplate("layout", 
        "{{> header }}<main>{{#yield \"content\"}}</main>");
    provider.AddTemplate("header", "<header>Site</header>");
    provider.AddTemplate("page",
        "{{#layout \"layout\"}}<h1>Page</h1>{{/layout}}");

    var engine = new ViewEngine(provider, new ViewEngineOptions(), new());
    var context = new TemplateContext(new Dictionary<string, object?>());

    var result = await engine.RenderAsync("page", context);

    Assert.Contains("<header>Site</header>", result);
    Assert.Contains("<h1>Page</h1>", result);
}
```

## Best Practices

1. **Keep templates simple** - Complex logic belongs in controllers
2. **Use helpers for formatting** - date, truncate, uppercase, etc.
3. **Organize partials** - Group by feature (components/, products/, etc.)
4. **Set defaults** - Use layouts and {{#yield-default}} for consistency
5. **Cache in production** - CacheCompiledTemplates = true
6. **Hot-reload in dev** - EnableHotReload = IsDevelopment()
7. **Validate input** - Controllers should validate before passing to templates
8. **Use globals** - Site-wide data (name, url, config) in Globals
9. **Keep layouts shallow** - Avoid deep nesting for performance
10. **Test edge cases** - Empty data, null values, missing partials

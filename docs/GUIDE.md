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

## Built-in Helper Examples

### Default Values

Use `default` to provide fallback values for missing or empty data:

```html
<!-- Page with optional title -->
<title>{{ default pageTitle "My Website" }}</title>

<!-- User profile with optional bio -->
<p>{{ default user.bio "This user hasn't written a bio yet." }}</p>

<!-- Product with optional description -->
<div>{{ default product.description "No description available." }}</div>
```

### Conditional Values (Ternary)

Use `ifval` for inline conditional values:

```html
<!-- User badge -->
<span class="badge {{ ifval user.isPremium "premium" "standard" }}">
  {{ ifval user.isPremium "Premium Member" "Standard Member" }}
</span>

<!-- Stock status -->
<div class="{{ ifval inStock "text-success" "text-danger" }}">
  {{ ifval inStock "In Stock" "Out of Stock" }}
</div>

<!-- Visibility -->
<button {{ ifval isPublic "" "disabled" }}>
  {{ ifval isPublic "Publish" "Draft" }}
</button>
```

### String Concatenation

Use `concat` to build strings from multiple parts:

```html
<!-- Full name -->
<h1>{{ concat "Hello, " user.firstName " " user.lastName "!" }}</h1>

<!-- Address -->
<address>
  {{ concat address.street ", " address.city ", " address.state " " address.zip }}
</address>

<!-- File path -->
<a href="{{ concat "/downloads/" file.year "/" file.month "/" file.name }}">
  Download
</a>
```

### String Replacement

Use `replace` to transform strings:

```html
<!-- URL-friendly title -->
<a href="/posts/{{ replace post.title " " "-" }}">{{ post.title }}</a>

<!-- Clean up file names -->
{{ replace fileName "_" " " }}

<!-- Format phone numbers -->
{{ replace phone "." "-" }}
```

### Collection Counting

Use `count` to display collection sizes:

```html
<!-- Cart items -->
<span class="badge">{{ count cart.items }} items</span>

<!-- Notification count -->
{{#if notifications }}
  <span class="count">{{ count notifications }}</span>
{{/if}}

<!-- Empty check -->
{{#if items }}
  <p>Showing {{ count items }} results</p>
{{#else}}
  <p>No results found</p>
{{/if}}
```

### Real-World Combination

Combining multiple helpers for complex templates:

```html
{{#each products as product }}
  <div class="product">
    <h3>{{ default product.name "Unnamed Product" }}</h3>
    <p class="price">
      ${{ product.price }}
      <span class="{{ ifval product.onSale "sale" "" }}">
        {{ ifval product.onSale "ON SALE!" "" }}
      </span>
    </p>
    <p>{{ truncate (default product.description "No description") 100 }}</p>
    <p class="stock {{ ifval product.inStock "available" "unavailable" }}">
      {{ ifval product.inStock (concat "In Stock (" (count product.variants) " variants)") "Out of Stock" }}
    </p>
    <a href="/products/{{ replace product.name " " "-" }}">View Details</a>
  </div>
{{/each}}
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
            LastModified = template.

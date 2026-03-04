# dotnet-webkit-views

[![NuGet](https://img.shields.io/nuget/v/JG.WebKit.Views?logo=nuget)](https://www.nuget.org/packages/JG.WebKit.Views)
[![Downloads](https://img.shields.io/nuget/dt/JG.WebKit.Views?color=%230099ff&logo=nuget)](https://www.nuget.org/packages/JG.WebKit.Views)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](./LICENSE)
[![CI](https://github.com/jamesgober/dotnet-webkit-views/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesgober/dotnet-webkit-views/actions)

---

A high-performance template engine for .NET with multiple rendering modes. File-based templates for development, compiled templates for production, and database-stored templates for CMS integration. Layouts, partials, loops, conditionals, and custom helpers — designer-friendly with zero C# in templates.

Part of the **JG WebKit** collection.

## Features

- **Three rendering modes** — file-based (dev), compiled (production), database-stored (CMS)
- **Layouts & sections** — define page structure with named content sections
- **Partials with parameters** — reusable components with scoped data passing
- **Template inheritance** — child templates override parent sections
- **Loops & conditionals** — `@each`, `@if`/`@else` with clean syntax
- **Named variants** — `@header("minimal")` loads `header-minimal.tpl` automatically
- **Auto-escaping** — HTML output escaped by default, `@raw()` for trusted content
- **Compiled caching** — parse once, execute many with delegate chains in memory
- **Hot-reload** — `FileSystemWatcher` in dev mode for instant feedback
- **Custom helpers** — register `@helper("name")` functions for template-callable logic
- **Cycle detection** — catches circular partial includes at parse time
- **Zero C# in templates** — intentionally designer-friendly, not Razor

## Installation

```bash
dotnet add package JG.WebKit.Views
```

## Quick Start

```csharp
builder.Services.AddWebKitViews(options =>
{
    options.TemplateDirectory = "templates";
    options.Mode = ViewMode.Compiled; // or File, Database
    options.CacheDuration = TimeSpan.FromMinutes(30);
});

// Render a template
var html = await viewEngine.RenderAsync("blog/post", new { Post = post, Comments = comments });
```

### Template Example
```html
@layout("main")
@section("content")
  <h1>@post.Title</h1>
  @each(comment in comments)
    @partial("comment-card", { comment: comment })
  @end
@end
```

## Documentation

- **[API Reference](./docs/API.md)** — Full API documentation and examples

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

Licensed under the Apache License 2.0. See [LICENSE](./LICENSE) for details.

---

**Ready to get started?** Install via NuGet and check out the [API reference](./docs/API.md).

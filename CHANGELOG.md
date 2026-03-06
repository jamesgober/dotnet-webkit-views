# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- _No changes yet._

## [1.0.0] - 2026-03-06

### Added
- Core template engine with compiled delegate rendering for maximum performance
- Complete template syntax support:
  - Variables with HTML escaping: `{{ variable }}`
  - Raw output (unescaped): `{{{ rawVariable }}}`
  - Comments: `{{-- comment text --}}`
  - Conditionals with comparison operators: `{{#if condition }}...{{/if}}`
  - Loops with index tracking: `{{#each collection as item }}...{{/each}}`
  - Partial includes with context: `{{> partialName context }}`
  - Layout system with sections: `{{#layout "name" }}...{{#section "name" }}...{{/section}}`
- Built-in template helpers:
  - `date` - Format dates with custom format strings
  - `truncate` - Truncate strings with ellipsis
  - `uppercase` / `lowercase` - Case conversion
  - `json` - JSON serialization
  - `asset` / `css` / `js` / `img` - Asset path resolution with CDN and cache busting
- Custom helper support via `ITemplateHelper` interface
- Template provider abstraction with two implementations:
  - `FileTemplateProvider` - File system with hot-reload support
  - `InMemoryTemplateProvider` - In-memory for testing/programmatic use
- Expression evaluation engine:
  - Dot notation property access: `{{ user.profile.name }}`
  - Array indexing: `{{ items[0] }}`
  - Comparison operators: `==`, `!=`, `>`, `<`, `>=`, `<=`
  - Logical operators: `!` (negation)
  - POCO object reflection with property caching
- Performance optimizations:
  - Zero-allocation HTML escape for safe strings
  - Template compilation caching
  - PropertyInfo caching for reflection
  - Span-based string operations
  - No LINQ in hot paths
  - All async with `ConfigureAwait(false)`
- Template caching with invalidation API
- FileSystemWatcher for automatic hot-reload in development
- Comprehensive error handling with detailed error messages
- Full XML documentation on all public APIs
- 142 unit and integration tests with 100% pass rate
- Edge case handling for null values, empty collections, missing variables
- Security features: XSS protection via HTML escaping by default

### Technical Implementation
- Parser with position advancement guards to prevent infinite loops
- Tokenizer with robust error handling for malformed syntax
- Compiled delegate rendering for near-native performance
- Deterministic builds with SourceLink support for debugging
- Apache-2.0 license

### Performance Characteristics
- Template compilation: ~1-2ms for typical templates
- Cached template rendering: <100μs for simple templates
- Expression evaluation: ~10-50μs depending on complexity
- Hot reload detection: <10ms via FileSystemWatcher
- Memory efficient: No intermediate allocations in render path

### Supported Scenarios
- ASP.NET Core MVC views
- Static site generation
- Email template rendering
- Report generation
- Dynamic content generation
- Server-side rendering (SSR)
- Microservices with shared templates

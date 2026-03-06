# JG.WebKit.Views Template Syntax Reference

## Variables

### Basic Variable

```html
{{ variable }}
```

Outputs the variable value, HTML-escaped.

```html
{{ title }}
<!-- Output: <p>Hello World</p> becomes &lt;p&gt;Hello World&lt;/p&gt; -->
```

### Nested Properties

```html
{{ post.title }}
{{ post.author.name }}
{{ config.api.key }}
```

Supports arbitrary depth using dot notation.

### Array Indexing

```html
{{ items[0] }}
{{ items[1].name }}
{{ matrix[row][col] }}
```

### Raw Output

```html
{{{ html }}}
```

Outputs unescaped. Only available if `AllowRawOutput = true`. Use only for trusted content.

### Property Priority

Variables are resolved in this order:
1. Template data
2. Globals
3. Not found (renders as empty)

```html
<!-- Looks first in data, then globals -->
{{ siteName }}
```

## HTML Escaping

All `{{ }}` variables are automatically escaped.

### Escaped Characters

```
& → &amp;
< → &lt;
> → &gt;
" → &quot;
' → &#x27;
```

### Example

```html
{{ userInput }}
<!-- Input: <script>alert('xss')</script> -->
<!-- Output: &lt;script&gt;alert(&#x27;xss&#x27;)&lt;/script&gt; -->
```

## Comments

```html
{{-- This comment is removed --}}
{{-- 
  Multi-line comments 
  just work normally
--}}
```

Comments are completely stripped from output.

## Conditionals

### If / Else If / Else

```html
{{#if condition }}
  <p>Condition is true</p>
{{/if}}
```

```html
{{#if status == "active" }}
  <span class="active">Active</span>
{{#elseif status == "pending" }}
  <span class="pending">Pending</span>
{{#else}}
  <span class="inactive">Inactive</span>
{{/if}}
```

Multiple `{{#elseif}}` blocks are supported.

### Truthiness

Falsy values (render false):
- `null`
- `false`
- `0` (integer, long, double, decimal, etc.)
- `""` (empty string)
- Empty collections

Everything else is truthy.

```html
<!-- Falsy examples -->
{{#if null }}              <!-- false -->
{{#if false }}             <!-- false -->
{{#if 0 }}                 <!-- false -->
{{#if "" }}                <!-- false -->
{{#if emptyList }}         <!-- false -->

<!-- Truthy examples -->
{{#if true }}              <!-- true -->
{{#if 1 }}                 <!-- true -->
{{#if "hello" }}           <!-- true -->
{{#if listWithItems }}     <!-- true -->
```

### Negation

```html
{{#if !archived }}
  <p>Active content</p>
{{/if}}
```

## Comparisons

### Equality

```html
{{#if status == "active" }}
{{#if count == 0 }}
{{#if userName == globalUser }}
```

### Inequality

```html
{{#if status != "draft" }}
{{#if count != 0 }}
```

### Numeric Comparisons

```html
{{#if rating > 5 }}
{{#if score < 100 }}
{{#if amount >= 50 }}
{{#if remaining <= 10 }}
```

Both sides are parsed as double for numeric comparison.

### String Comparisons

String comparisons are ordinal (case-sensitive, culture-invariant).

```html
{{#if role == "Admin" }}    <!-- case-sensitive -->
{{#if role == "admin" }}    <!-- different from above -->
```

## Loops

### Each with Index

```html
{{#each items as item }}
  <li>{{ item.name }} ({{ item.index }})</li>
{{/each}}
```

The variable `item.index` is automatically available (zero-based).

### Empty Block

```html
{{#each posts as post }}
  <article>{{ post.title }}</article>
{{#empty}}
  <p>No posts found</p>
{{/empty}}
```

The {{#empty}} block renders if the collection is empty.

### Nested Loops

```html
{{#each categories as category }}
  <h3>{{ category.name }}</h3>
  <ul>
    {{#each category.items as item }}
      <li>{{ item.name }}</li>
    {{/each}}
  </ul>
{{/each}}
```

The inner loop variable `item` shadows the outer `category` within its scope.

### Loop Variable Scope

Loop variables are only available within the loop block.

```html
{{#each items as item }}
  <!-- item is available here -->
  {{ item.name }}
{{/each}}
<!-- item is NOT available here -->
```

## Partials

### Basic Include

```html
{{> header }}
```

Includes `partials/header.tpl` at that location.

### With Data Context

```html
{{> card product }}
```

Includes `partials/card.tpl` with `product` as the root context.

Inside `card.tpl`, reference `product` properties directly:
```html
<!-- In partials/card.tpl -->
<h3>{{ name }}</h3>
<p>{{ price }}</p>
```

### Variant Fallback

```html
{{> card "featured" }}
```

Tries `partials/card-featured.tpl` first, falls back to `partials/card.tpl` if not found.

### Combining Context and Variant

```html
{{> item post "featured" }}
```

Tries `partials/item-featured.tpl` with `post` as context, falls back to `partials/item.tpl`.

### Partial Naming

Partial path is relative to PartialPath configuration:

```csharp
options.PartialPath = "templates/partials";  // default
```

```html
{{> header }}              <!-- templates/partials/header.tpl -->
{{> products/card }}       <!-- templates/partials/products/card.tpl -->
{{> shared/layout/nav }}   <!-- templates/partials/shared/layout/nav.tpl -->
```

## Layouts

### Layout Declaration

```html
{{#layout "main" }}

<!-- Page content here -->
<h1>{{ title }}</h1>

{{/layout}}
```

Declares that this page uses the `layouts/main.tpl` layout.

### Sections

Define named content sections:

```html
{{#section "sidebar" }}
  <nav>Sidebar navigation</nav>
{{/section}}

{{#section "scripts" }}
  <script src="{{ script 'page.js' }}"></script>
{{/section}}
```

Sections are rendered where the layout specifies them.

### Yield Points

In the layout, output sections using yield:

```html
<!-- layouts/main.tpl -->
<!DOCTYPE html>
<html>
  <body>
    <main>
      {{#yield "content" }}
    </main>
    <aside>
      {{#yield "sidebar" }}
    </aside>
    {{#yield "scripts" }}
  </body>
</html>
```

The page body becomes the implicit "content" section.

### Default Section Content

Provide default content if a section is not defined:

```html
{{#yield-default "sidebar" }}
  <p>No sidebar content</p>
{{/yield-default}}
```

If the page doesn't define {{#section "sidebar"}}, this default renders instead.

### Complete Layout Example

**page.tpl:**
```html
{{#layout "main" }}

<h1>{{ title }}</h1>
<article>
  {{ content }}
</article>

{{#section "meta" }}
  <meta name="author" content="{{ author }}" />
{{/section}}

{{#section "sidebar" }}
  <h3>Related</h3>
  {{#each related as item }}
    <a href="{{ item.url }}">{{ item.title }}</a>
  {{/each}}
{{/section}}
```

**layouts/main.tpl:**
```html
<!DOCTYPE html>
<html>
  <head>
    <title>{{ title }}</title>
    {{#yield "meta" }}
    {{#yield-default "meta" }}
      <meta name="description" content="{{ description }}" />
    {{/yield-default}}
  </head>
  <body>
    {{> header }}
    <main>
      {{#yield "content" }}
    </main>
    <aside>
      {{#yield "sidebar" }}
      {{#yield-default "sidebar" }}
        <!-- no sidebar -->
      {{/yield-default}}
    </aside>
    {{> footer }}
  </body>
</html>
```

## Built-in Functions

### date

```html
{{ date createdAt "yyyy-MM-dd" }}
{{ date now "dddd, MMMM d, yyyy HH:mm" }}
```

Formats DateTime or DateTimeOffset using .NET format strings.

### truncate

```html
{{ truncate text 100 }}
{{ truncate description 50 }}
```

Truncates to max length with "..." suffix. If already shorter, returns as-is.

### uppercase

```html
{{ uppercase title }}
{{ uppercase slug }}
```

Converts to uppercase using invariant culture.

### lowercase

```html
{{ lowercase title }}
{{ lowercase slug }}
```

Converts to lowercase using invariant culture.

### json

```html
<script>
  const data = {{ json payload }};
</script>
```

JSON-serializes the value. Use for embedding data in script tags.

### asset

```html
{{ asset "main.css" }}
{{ asset "images/logo.png" }}
```

Resolves asset path. Default: `/assets/main.css`

With CDN:
```html
<!-- /assets/main.css → https://cdn.example.com/assets/main.css -->
```

With version hash:
```html
<!-- /assets/main.css → /assets/main.css?v=a1b2c3d4 -->
```

### image

```html
{{ image "logo.png" }}
```

Resolves image path. Default: `/assets/images/logo.png`

### script

```html
{{ script "app.js" }}
```

Resolves script path. Default: `/assets/js/app.js`

### font

```html
{{ font "Inter.woff2" }}
```

Resolves font path. Default: `/assets/fonts/Inter.woff2`

### media

```html
{{ media "video.mp4" }}
```

Resolves media path. Default: `/assets/media/video.mp4`

## Common Patterns

### List with Fallback

```html
<ul>
  {{#each items as item }}
    <li>{{ item.name }}</li>
  {{#empty}}
    <li>No items</li>
  {{/empty}}
</ul>
```

### Conditional Class

```html
<div class="{{#if isActive }}active{{/if}}">
  Content
</div>
```

### Showing Author or Date

```html
{{#if showAuthor }}
  by {{ author.name }}
{{#else}}
  {{ date createdAt "MMM d" }}
{{/if}}
```

### Nested Data Display

```html
{{#each posts as post }}
  <h2>{{ post.title }}</h2>
  {{#each post.comments as comment }}
    <p>{{ comment.author }}: {{ comment.text }}</p>
  {{#empty}}
    <p>No comments</p>
  {{/empty}}
{{/each}}
```

### Pagination Links

```html
{{#if previousPage }}
  <a href="?page={{ previousPage }}">Previous</a>
{{/if}}

{{#each pages as page }}
  {{#if page == currentPage }}
    <strong>{{ page }}</strong>
  {{#else}}
    <a href="?page={{ page }}">{{ page }}</a>
  {{/if}}
{{/each}}

{{#if nextPage }}
  <a href="?page={{ nextPage }}">Next</a>
{{/if}}
```

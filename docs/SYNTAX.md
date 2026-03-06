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

## Built-in Helpers

Helpers are functions callable from templates using `{{ helperName arguments }}` syntax.

### Formatting Helpers

#### date

Format dates and times with custom patterns.

**Syntax:**
```html
{{ date value "format" }}
```

**Examples:**
```html
{{ date createdAt "yyyy-MM-dd" }}            <!-- 2026-03-06 -->
{{ date publishedAt "MMMM d, yyyy" }}        <!-- March 6, 2026 -->
{{ date now "MMM dd" }}                      <!-- Mar 06 -->
{{ date timestamp "hh:mm tt" }}              <!-- 08:30 AM -->
```

#### truncate

Truncate strings to maximum length with ellipsis.

**Syntax:**
```html
{{ truncate string maxLength }}
```

**Examples:**
```html
{{ truncate description 100 }}
{{ truncate title 50 }}
```

### String Helpers

#### uppercase

Convert string to uppercase.

**Syntax:**
```html
{{ uppercase string }}
```

**Examples:**
```html
{{ uppercase title }}                        <!-- HELLO WORLD -->
{{ uppercase "hello" }}                      <!-- HELLO -->
```

#### lowercase

Convert string to lowercase.

**Syntax:**
```html
{{ lowercase string }}
```

**Examples:**
```html
{{ lowercase email }}                        <!-- user@example.com -->
{{ lowercase "HELLO" }}                      <!-- hello -->
```

#### concat

Concatenate multiple strings.

**Syntax:**
```html
{{ concat string1 string2 ... }}
```

**Examples:**
```html
{{ concat firstName " " lastName }}          <!-- John Doe -->
{{ concat "Hello, " user.name "!" }}         <!-- Hello, John! -->
{{ concat path "/" file }}                   <!-- /uploads/file.pdf -->
```

#### replace

Replace all occurrences of substring.

**Syntax:**
```html
{{ replace string search replacement }}
```

**Examples:**
```html
{{ replace title "-" " " }}                  <!-- Hello World -->
{{ replace slug "_" "-" }}                   <!-- my-awesome-post -->
{{ replace phone "." "-" }}                  <!-- 555-123-4567 -->
```

### Conditional Helpers

#### default

Provide fallback value for null/empty values.

**Syntax:**
```html
{{ default value fallback }}
```

**Returns fallback if value is:**
- `null`
- Empty string `""`
- Whitespace-only

**Returns value (not fallback) for:**
- `0` (zero)
- `false`
- Any non-empty value

**Examples:**
```html
{{ default title "Untitled Page" }}
{{ default user.bio "No bio" }}
{{ default description "..." }}
```

#### ifval

Conditional value selection (ternary operator).

**Syntax:**
```html
{{ ifval condition trueValue falseValue }}
```

**Truthy values:**
- Non-empty strings
- Non-zero numbers
- `true`
- Objects

**Falsy values:**
- `null`
- Empty strings `""`
- `0` (zero)
- `false`

**Examples:**
```html
{{ ifval user.isAdmin "Administrator" "User" }}
{{ ifval inStock "Available" "Sold Out" }}
{{ ifval hasErrors "danger" "success" }}
```

### Collection Helpers

#### count

Return count of items in collection.

**Syntax:**
```html
{{ count collection }}
```

**Returns "0" for:**
- `null`
- Empty collections
- Non-collection values
- Strings (not counted as collections)

**Examples:**
```html
{{ count items }}                            <!-- 5 -->
{{ count cart.products }}                    <!-- 12 -->
<span>{{ count notifications }} new</span>   <!-- 3 new -->
```

### Serialization Helpers

#### json

Serialize value to JSON.

**Syntax:**
```html
{{ json value }}
```

**Examples:**
```html
<script>
const data = {{ json payload }};
const settings = {{ json config }};
</script>
```

### Asset Helpers

Generate asset URLs with CDN and versioning support.

#### asset

Generic asset helper.

**Syntax:**
```html
{{ asset "path" }}
```

**Example:**
```html
{{ asset "main.css" }}                       <!-- /assets/main.css -->
```

#### css

CSS file helper.

**Syntax:**
```html
{{ css "file.css" }}
```

**Example:**
```html
<link rel="stylesheet" href="{{ css "styles.css" }}">
<!-- /assets/css/styles.css -->
```

#### js

JavaScript file helper.

**Syntax:**
```html
{{ js "file.js" }}
```

**Example:**
```html
<script src="{{ js "app.js" }}"></script>
<!-- /assets/js/app.js -->
```

#### img

Image file helper.

**Syntax:**
```html
{{ img "file.png" }}
```

**Example:**
```html
<img src="{{ img "logo.png" }}" alt="Logo">
<!-- /assets/images/logo.png -->
```

#### font

Font file helper.

**Syntax:**
```html
{{ font "file.woff2" }}
```

**Example:**
```html
{{ font "Inter.woff2" }}
<!-- /assets/fonts/Inter.woff2 -->
```

#### media

Media file helper.

**Syntax:**
```html
{{ media "file.mp4" }}
```

**Example:**
```html
<video src="{{ media "intro.mp4" }}"></video>
<!-- /assets/media/intro.mp4 -->
```

### Asset Configuration

Asset helpers respect `ViewEngineOptions.Assets` settings:

**CDN Base URL:**
```csharp
options.Assets.CdnBaseUrl = "https://cdn.example.com";
```
```html
{{ css "styles.css" }}
<!-- https://cdn.example.com/assets/css/styles.css -->
```

**Version Hashing:**
```csharp
options.Assets.AppendVersionHash = true;
options.Assets.AssetVersionHash = "abc123";
```
```html
{{ css "styles.css" }}
<!-- /assets/css/styles.css?v=abc123 -->
```

## Custom Helpers

Create custom helpers by implementing `ITemplateHelper`:

```csharp
public class MyHelper : ITemplateHelper
{
    public string Name => "myhelper";
    
    public string Execute(object?[] arguments, TemplateContext context)
    {
        // Your logic here
        return "result";
    }
}
```

Register in DI:
```csharp
services.AddTemplateHelper<MyHelper>();
```

Use in templates:
```html
{{ myhelper arg1 arg2 }}

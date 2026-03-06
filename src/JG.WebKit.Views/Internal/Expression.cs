namespace JG.WebKit.Views.Internal;

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Evaluates template expressions with dot notation, array indexing, and property access.
/// Implements caching for PropertyInfo lookups to optimize POCO object reflection.
/// </summary>
internal sealed class Expression
{
    private readonly string _expression;
    private readonly List<string> _parts;

    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyCache = new();

    /// <summary>
    /// Initializes a new instance of the Expression class.
    /// </summary>
    /// <param name="expression">The expression string to evaluate (e.g., "user.profile.name").</param>
    public Expression(string expression)
    {
        _expression = expression;
        _parts = ParseExpression(expression);
    }

    /// <summary>
    /// Factory method to create a new Expression instance.
    /// </summary>
    /// <param name="expression">The expression string to parse.</param>
    /// <returns>A new Expression instance ready for evaluation.</returns>
    public static Expression Parse(string expression)
    {
        return new Expression(expression);
    }

    /// <summary>
    /// Evaluates the expression against the provided context.
    /// Supports dot notation (user.name), array indexing (items[0]), and deep nesting.
    /// Returns null for missing variables without throwing exceptions.
    /// </summary>
    /// <param name="context">The template context containing data and globals.</param>
    /// <returns>The evaluated value, or null if the expression cannot be resolved.</returns>
    public object? Evaluate(TemplateContext context)
    {
        if (_parts.Count == 0)
            return null;

        // Try the full expression as a single key first (e.g., "item.index")
        if (_parts.Count > 1 && context.Data.TryGetValue(_expression, out var fullKeyValue))
            return fullKeyValue;

        object? current = ResolveRoot(context, _parts[0]);

        for (int i = 1; i < _parts.Count; i++)
        {
            var part = _parts[i];

            if (current == null)
                return null;

            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                var indexStr = part.Substring(1, part.Length - 2);
                current = ResolveIndex(current, indexStr);
            }
            else
            {
                current = ResolveProperty(current, part);
            }
        }

        return current;
    }

    private static object? ResolveRoot(TemplateContext context, string name)
    {
        if (context.Data.TryGetValue(name, out var value))
            return value;

        if (context.Globals.TryGetValue(name, out var globalValue))
            return globalValue;

        return null;
    }

    private static object? ResolveProperty(object? obj, string propName)
    {
        if (obj == null)
            return null;

        if (obj is IReadOnlyDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(propName, out var value))
                return value;
            return null;
        }

        if (obj is IDictionary<string, object?> mutableDict)
        {
            if (mutableDict.TryGetValue(propName, out var value))
                return value;
            return null;
        }

        var type = obj.GetType();
        var cacheKey = $"{type.FullName}.{propName}";

        var prop = PropertyCache.GetOrAdd(cacheKey, _ =>
        {
            return type.GetProperty(propName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        });

        if (prop != null)
        {
            try
            {
                return prop.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static object? ResolveIndex(object? obj, string indexStr)
    {
        if (obj == null)
            return null;

        if (obj is IList list && int.TryParse(indexStr, out var index))
        {
            try
            {
                if (index >= 0 && index < list.Count)
                    return list[index];
            }
            catch
            {
                return null;
            }
        }

        if (obj is IDictionary dict)
        {
            return dict[indexStr];
        }

        return null;
    }

    private static List<string> ParseExpression(string expression)
    {
        var parts = new List<string>();
        var current = new StringBuilder();

        foreach (var ch in expression)
        {
            if (ch == '.' && current.Length > 0 && !current.ToString().StartsWith('['))
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else if (ch == '[')
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }

                var bracketContent = new StringBuilder("[");
                var depth = 1;
                var i = expression.IndexOf('[') + 1;
                while (i < expression.Length && depth > 0)
                {
                    if (expression[i] == '[') depth++;
                    if (expression[i] == ']') depth--;
                    bracketContent.Append(expression[i]);
                    i++;
                }

                parts.Add(bracketContent.ToString());
                return parts;
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts;
    }
}

/// <summary>
/// Evaluates conditional expressions for template if/elseif blocks.
/// Supports comparison operators (==, !=, &gt;, &lt;, &gt;=, &lt;=), negation (!), and truthy checks.
/// String comparisons are case-sensitive (ordinal).
/// Numeric comparisons work across int, long, float, double types.
/// </summary>
internal sealed class ConditionEvaluator
{
    private readonly string _condition;

    /// <summary>
    /// Initializes a new instance of the ConditionEvaluator class.
    /// </summary>
    /// <param name="condition">The condition expression to evaluate (e.g., "count >= 10").</param>
    public ConditionEvaluator(string condition)
    {
        _condition = condition;
    }

    /// <summary>
    /// Evaluates the condition against the provided context.
    /// </summary>
    /// <param name="context">The template context for variable resolution.</param>
    /// <returns>True if the condition is satisfied, otherwise false.</returns>
    public bool Evaluate(TemplateContext context)
    {
        var trimmed = _condition.Trim();

        // Check for negation
        if (trimmed.StartsWith('!'))
        {
            return !EvaluateCondition(trimmed.Substring(1).Trim(), context);
        }

        return EvaluateCondition(trimmed, context);
    }

    private static bool EvaluateCondition(string condition, TemplateContext context)
    {
        // Check for comparison operators
        foreach (var op in new[] { ">=", "<=", "==", "!=", ">", "<" })
        {
            var index = condition.IndexOf(op, StringComparison.Ordinal);
            if (index > 0)
            {
                var left = condition.Substring(0, index).Trim();
                var right = condition.Substring(index + op.Length).Trim();

                return EvaluateComparison(left, op, right, context);
            }
        }

        // Simple truthy check
        var expr = Expression.Parse(condition);
        var value = expr.Evaluate(context);

        return IsTruthy(value);
    }

    private static bool EvaluateComparison(string left, string op, string right, TemplateContext context)
    {
        var leftExpr = Expression.Parse(left.Trim());
        var leftValue = leftExpr.Evaluate(context);

        right = right.Trim();

        object? rightValue;
        if (right.StartsWith('"') && right.EndsWith('"'))
        {
            rightValue = right.Substring(1, right.Length - 2);
        }
        else if (right.StartsWith('\'') && right.EndsWith('\''))
        {
            rightValue = right.Substring(1, right.Length - 2);
        }
        else if (int.TryParse(right, out var intVal))
        {
            rightValue = intVal;
        }
        else if (double.TryParse(right, out var doubleVal))
        {
            rightValue = doubleVal;
        }
        else
        {
            rightValue = Expression.Parse(right).Evaluate(context);
        }

        return op switch
        {
            "==" => AreEqual(leftValue, rightValue),
            "!=" => !AreEqual(leftValue, rightValue),
            ">" => IsGreaterThan(leftValue, rightValue),
            "<" => IsLessThan(leftValue, rightValue),
            ">=" => IsGreaterThanOrEqual(leftValue, rightValue),
            "<=" => IsLessThanOrEqual(leftValue, rightValue),
            _ => false
        };
    }

    private static bool AreEqual(object? left, object? right)
    {
        // Handle null cases
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        // String comparison is case-sensitive (ordinal)
        if (left is string leftStr && right is string rightStr)
            return leftStr.Equals(rightStr, StringComparison.Ordinal);

        // For other types, use default equality
        return left.Equals(right);
    }

    private static bool IsGreaterThan(object? left, object? right)
    {
        if (TryParseDouble(left, out var leftNum) && TryParseDouble(right, out var rightNum))
            return leftNum > rightNum;
        return false;
    }

    private static bool IsLessThan(object? left, object? right)
    {
        if (TryParseDouble(left, out var leftNum) && TryParseDouble(right, out var rightNum))
            return leftNum < rightNum;
        return false;
    }

    private static bool IsGreaterThanOrEqual(object? left, object? right)
    {
        if (TryParseDouble(left, out var leftNum) && TryParseDouble(right, out var rightNum))
            return leftNum >= rightNum;
        return false;
    }

    private static bool IsLessThanOrEqual(object? left, object? right)
    {
        if (TryParseDouble(left, out var leftNum) && TryParseDouble(right, out var rightNum))
            return leftNum <= rightNum;
        return false;
    }

    private static bool TryParseDouble(object? obj, out double result)
    {
        if (obj is double d)
        {
            result = d;
            return true;
        }

        if (obj is int i)
        {
            result = i;
            return true;
        }

        if (obj is long l)
        {
            result = l;
            return true;
        }

        if (obj is float f)
        {
            result = f;
            return true;
        }

        if (obj is string str && double.TryParse(str, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = 0;
        return false;
    }

    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            false => false,
            0 => false,
            0.0 => false,
            0L => false,
            0f => false,
            "" => false,
            IEnumerable<object?> enumerable => HasElements(enumerable),
            IEnumerable e => HasElements(e),
            _ => true
        };
    }

    private static bool HasElements(IEnumerable enumerable)
    {
        foreach (var _ in enumerable)
        {
            return true;
        }

        return false;
    }

    private static bool HasElements(IEnumerable<object?> enumerable)
    {
        foreach (var _ in enumerable)
        {
            return true;
        }

        return false;
    }
}

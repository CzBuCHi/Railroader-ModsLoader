using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NSubstitute.Core;
using Shouldly;

// ReSharper disable once CheckNamespace
namespace NSubstitute;

[ExcludeFromCodeCoverage]
public static class SubstituteExtensions {
    public static void ShouldReceiveNoCalls<T>(this T substitute) where T : class =>
        substitute.ReceivedCalls().ShouldBeEmpty();

    public static void ShouldReceiveCallCount<T>(this T substitute, int count) where T : class =>
        substitute.ReceivedCalls().Count().ShouldBe(count);
}

[ExcludeFromCodeCoverage]
public static class SubstituteDebugExtensions {
    
    public static string PrintReceivedCalls<T>(this T substitute, string callerName) where T : class {
        var sb = new StringBuilder();
        foreach (var call in substitute.ReceivedCalls()) {
            PrintCall(call);
        }

        return sb.ToString();

        void PrintCall(ICall call) {
            var method = call.GetMethodInfo()!;
            sb.Append(callerName).Append(".Received().");
            sb.Append(method.Name);
            sb.Append('(');

            var args  = call.GetArguments()!;
            var first = true;
            foreach (var arg in args) {
                if (!first) {
                    sb.Append(", ");
                }

                first = false;

                sb.Append(ArgToString(arg));
            }

            sb.AppendLine(");");
        }
    }

    private static string ArgToString(object? arg) {
        if (arg is null) {
            return "null";
        }

        var type = arg.GetType();

        // --------------------------------------------------------------------
        // 1. Primitive / well-known value types
        // --------------------------------------------------------------------
        if (type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid)) {
            return PrimitiveToSource(arg, type);
        }

        // --------------------------------------------------------------------
        // 2. String – choose the best literal style
        // --------------------------------------------------------------------
        if (arg is string s) {
            return s.Contains("\\") ? $"@\"{s}\"" : $"\"{EscapeNormal(s)}\"";
        }

        // --------------------------------------------------------------------
        // 3. Char
        // --------------------------------------------------------------------
        if (arg is char c) {
            return CharToSource(c);
        }

        // --------------------------------------------------------------------
        // 4. Enumerable / Array (including multi-dimensional & jagged)
        // --------------------------------------------------------------------
        if (arg is IEnumerable enumerable && arg is not string) {
            return EnumerableToSource(enumerable);
        }

        // --------------------------------------------------------------------
        // 5. Fallback – Arg.Any<T>() style (for complex objects)
        // --------------------------------------------------------------------
        return $"Arg.Any<{type.Name}>()";
    }

    private static string PrimitiveToSource(object value, Type type) {
        // bool
        if (type == typeof(bool)) {
            return (bool)value ? "true" : "false";
        }

        // No suffix for byte, sbyte, short, ushort
        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int)) {
            return value.ToString();
        }

        // Suffix for uint, long, ulong
        if (type == typeof(uint)) {
            return value + "u";
        }

        if (type == typeof(long)) {
            return value + "L";
        }

        if (type == typeof(ulong)) {
            return value + "UL";
        }

        // float, double, decimal
        if (type == typeof(float)) {
            return value + "f";
        }

        if (type == typeof(double)) {
            return value.ToString()!;
        }

        if (type == typeof(decimal)) {
            return value + "m";
        }

        // DateTime / Guid
        if (value is DateTime dt) {
            return $"DateTime.Parse(\"{dt:o}\")";
        }

        if (value is Guid g) {
            return $"new Guid(\"{g:D}\")";
        }

        // Fallback
        return value.ToString()!;
    }

    private static string CharToSource(char c) {
        return c switch {
            '\'' => @"'\''",
            '\\' => @"'\\'",
            '\0' => @"'\0'",
            '\a' => @"'\a'",
            '\b' => @"'\b'",
            '\f' => @"'\f'",
            '\n' => @"'\n'",
            '\r' => @"'\r'",
            '\t' => @"'\t'",
            '\v' => @"'\v'",
            _ => c < 32 || c > 126
                ? $"\'\\u{(int)c:x4}\'"
                : $"'{c}'"
        };
    }
    
    private static string EscapeNormal(string s)
        => s.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\0", "\\0")
            .Replace("\a", "\\a")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\v", "\\v");

    private static string EnumerableToSource(IEnumerable enumerable) {
        var elements = new List<string>();

        foreach (var item in enumerable) {
            elements.Add(ArgToString(item));
        }

        // Detect array type to emit the proper cast
        var arr    = enumerable as Array;
        var prefix = "new[]";

        if (arr != null) {
            var rank        = arr.Rank;
            var elementType = arr.GetType().GetElementType()!;

            if (rank == 1) {
                prefix = $"new {elementType.Name}[]";
            } else {
                // Multi-dimensional: new int[2,3] { {…}, {…} }
                var dims = string.Join(",", Enumerable.Range(0, rank).Select(_ => arr.GetLength(_)));
                prefix = $"new {elementType.Name}[{dims}]";
            }
        }

        // For non-array IEnumerable we just emit new[] { … }
        var inner = string.Join(", ", elements);
        return $"{prefix} {{ {inner} }}";
    }
}
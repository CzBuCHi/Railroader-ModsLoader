using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;

namespace Railroader.ModManager.Tests.TestExtensions;

[ExcludeFromCodeCoverage]
public static class SubstituteExtensions
{
    public static void ShouldReceiveNoCalls<T>(this T substitute) where T : class =>
        substitute.ReceivedCalls().ShouldBeEmpty();

    public static void ShouldReceiveCallCount<T>(this T substitute, int count) where T : class =>
        substitute.ReceivedCalls().Count().ShouldBe(count);

    public static string PrintReceivedCalls<T>(this T substitute) where T : class {
        var sb = new StringBuilder();
        foreach (var call in substitute.ReceivedCalls()) {
            PrintCall(call);
        }

        return sb.ToString();

        void PrintCall(ICall call) {
            var method = call.GetMethodInfo()!;
            sb.Append("o.Received().");
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

        string ArgToString(object? arg) {
            switch (arg) {
                case null:
                    return "null";

                case string s:
                    return StringLiteral(s);

                case string[] strArray:
                    return $"[{string.Join(", ", strArray.Select(ArgToString))}]";

                case Exception ex:
                    return $"new {ex.GetType().FullName}({ArgToString(ex.Message)})";

                case ModdingContext ctx:
                    var mods = JsonConvert.SerializeObject(ctx.Mods);
                    return mods; // or $"JsonConvert.DeserializeObject<{ctx.Mods.GetType().FullName}>({StringLiteral(mods)})";

                default:
                    // add more known types here as needed
                    return Convert.ToString(arg, CultureInfo.InvariantCulture)
                           ?? $"/* null from {arg.GetType()} */";
            }

            string StringLiteral(string value) {
                // Prefer verbatim if there are backslashes and no quotes
                if (value.Contains('\\') && !value.Contains('"') && !value.Contains("\r") && !value.Contains("\n")) {
                    return @$"@""{value}""";
                }

                // Use raw string if it has both backslashes and quotes or newlines
                if (NeedsRaw(value)) {
                    var quoteRun = MaxQuoteRun(value);
                    var quotes   = new string('"', Math.Max(3, quoteRun + 1));
                    return $"{quotes}{value}{quotes}";
                }

                // Otherwise normal escaped string
                return $"\"{Escape(value)}\"";

                static bool NeedsRaw(string s) => s.Contains('"') || s.Contains('\n') || s.Contains('\r') || s.Contains('\\');

                static string Escape(string s)
                    => s
                       .Replace("\\", @"\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");

                static int MaxQuoteRun(string s) {
                    int maxRun = 0, current = 0;
                    foreach (var c in s) {
                        if (c == '"') {
                            current++;
                        } else {
                            maxRun = Math.Max(maxRun, current);
                            current = 0;
                        }
                    }

                    return Math.Max(maxRun, current);
                }
            }
        }
    }
}

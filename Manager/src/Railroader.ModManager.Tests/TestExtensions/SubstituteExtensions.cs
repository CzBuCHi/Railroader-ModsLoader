using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core;

namespace Railroader.ModManager.Tests.TestExtensions;

public static class SubstituteExtensions
{
    public static void ShouldReceiveOnly<T>(this T substitute, Action<T> received) where T : class {
        var dummy = Substitute.For<T>();
        received(dummy);
        substitute.ReceivedCalls().Should().BeEquivalentTo(dummy.ReceivedCalls()!);
    }

    public static void ShouldReceiveNoCalls<T>(this T substitute) where T : class =>
        substitute.ReceivedCalls().Should().BeEmpty();

    public static string ToString(this IEnumerable<ICall>? calls, string prefix) {
        var sb = new StringBuilder();
        sb.Append(prefix).AppendLine("ShouldReceiveOnly(o => {");
        if (calls != null) {
            foreach (var call in calls) {
                PrintCall(call);
            }
        }

        sb.AppendLine("});");
        return sb.ToString();

        void PrintCall(ICall call) {
            var method = call.GetMethodInfo()!;
            sb.Append("    o.");
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
                case string str:
                    if (str.Contains("\"")) {
                        return $"\"\"\"{arg}\"\"\"";
                    }

                    return $"\"{arg}\"";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

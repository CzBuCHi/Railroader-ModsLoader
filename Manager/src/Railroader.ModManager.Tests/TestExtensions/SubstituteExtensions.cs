using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
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

    public static string PrintReceivedCalls<T>(this T substitute) where T : class {
        var sb = new StringBuilder();
        sb.Append("substitute.").AppendLine("ShouldReceiveOnly(o => {");
        foreach (var call in substitute.ReceivedCalls()!) {
            PrintCall(call);
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

                case ModdingContext moddingContext:
                    var mods = JsonConvert.SerializeObject(moddingContext.Mods);
                    return string.Join(", ", mods);

                default:
                    throw new NotImplementedException("Arg type: " + arg.GetType());
            }
        }
    }
}

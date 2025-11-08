using System;
using System.Diagnostics.CodeAnalysis;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IConsoleStatic
{
    void Write(object value);
    void WriteLine(object value, ConsoleColor? color = null);
    void SetTitle(string title);
    ConsoleKeyInfo ReadKey();
}

[ExcludeFromCodeCoverage]
public sealed class ConsoleStatic : IConsoleStatic
{
    public void Write(object value) => Console.Write(value);

    public void WriteLine(object value, ConsoleColor? color = null) {
        if (color != null) {
            Console.ForegroundColor = color.Value;
        }

        Console.Error.WriteLine(value);

        if (color != null) {
            Console.ResetColor();
        }
    }

    public void SetTitle(string title) => Console.Title = title;

    public ConsoleKeyInfo ReadKey() => Console.ReadKey();
}

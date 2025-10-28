namespace Railroader.ModManagerInstaller;

public static class ConsoleEx
{
    public static void WriteWarning(string message) => WriteLine(message, ConsoleColor.DarkYellow);

    public static void WriteError(string message) => WriteLine(message, ConsoleColor.Red);

    public static void WriteFatal(string message) {
        WriteError(message);
        Environment.Exit(1);
    }

    private static void WriteLine(string message, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }
}

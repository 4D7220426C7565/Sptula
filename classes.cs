using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/* --- Class Console_Colors --- */
class Class_Color
{
    public static class Console_Colors
    {
        public static void WriteLineWithColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteWithColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }
    }
}

/* --- Save Emails --- */
class EmailStorage
{
    public static void SaveEmailsToFile(HashSet<string> emails, string filePath)
    {
        try
        {
            string fullPath = Path.GetFullPath(filePath);
            using (StreamWriter writer = new(fullPath))
            {
                foreach (var email in emails)
                {
                    writer.WriteLine(email);
                }
            }
            Class_Color.Console_Colors.WriteLineWithColor($"Emails successfully saved to {fullPath}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            Class_Color.Console_Colors.WriteLineWithColor($"An error occurred while saving emails to file: {ex.Message}", ConsoleColor.Red);
        }
    }
}

class Banner
{
    public static void Show()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("      _____");
        Console.WriteLine("╔═╗┌─┐|\\|/|┬ ┬┬  ┌─┐");
        Console.WriteLine("╚═╗├─┘ |@| │ ││  ├─┤");
        Console.WriteLine("╚═╝┴   |_| └─┘┴─┘┴ ┴");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("    \\ v.1.0.0 /");
        Console.ResetColor();
    }
}

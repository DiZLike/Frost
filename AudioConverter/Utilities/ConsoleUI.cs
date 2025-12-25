namespace OpusConverter.Utilities
{
    public static class ConsoleUI
    {
        public static void PrintSeparator(int length = 60)
        {
            Console.WriteLine(new string('=', length));
        }

        public static void PrintSectionHeader(string title, int length = 60)
        {
            PrintSeparator(length);
            Console.WriteLine(title.ToUpper());
            PrintSeparator(length);
        }

        public static void PrintInfo(string label, object value, int indent = 4)
        {
            Console.WriteLine($"{new string(' ', indent)}{label}: {value}");
        }

        public static void PrintSuccess(string message, int indent = 8)
        {
            Console.WriteLine($"{new string(' ', indent)}✓ {message}");
        }

        public static void PrintError(string message, int indent = 8)
        {
            Console.WriteLine($"{new string(' ', indent)}✗ {message}");
        }

        public static void PrintWarning(string message, int indent = 8)
        {
            Console.WriteLine($"{new string(' ', indent)}⚠ {message}");
        }
    }
}
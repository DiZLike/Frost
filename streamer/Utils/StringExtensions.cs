namespace Strimer.Utils
{
    public static class StringExtensions
    {
        public static string[] Substrings(this string str, string left, string right)
        {
            if (string.IsNullOrEmpty(str))
                return Array.Empty<string>();

            List<string> results = new();
            int startIndex = 0;

            while (true)
            {
                // Ищем левую границу
                int leftIndex = str.IndexOf(left, startIndex, StringComparison.Ordinal);
                if (leftIndex == -1)
                    break;

                leftIndex += left.Length;

                // Ищем правую границу
                int rightIndex = str.IndexOf(right, leftIndex, StringComparison.Ordinal);
                if (rightIndex == -1)
                    break;

                // Извлекаем подстроку
                string substring = str.Substring(leftIndex, rightIndex - leftIndex);
                results.Add(substring);

                // Продолжаем поиск
                startIndex = rightIndex + right.Length;
            }

            return results.ToArray();
        }

        public static int ToInt(this string value, int defaultValue = 0)
        {
            if (int.TryParse(value, out int result))
                return result;

            return defaultValue;
        }

        public static bool ToBool(this string value)
        {
            return value.ToLower() == "yes" || value.ToLower() == "true" || value == "1";
        }
    }
}
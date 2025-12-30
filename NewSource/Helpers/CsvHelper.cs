namespace AIOverhaul.Helpers
{
    /// <summary>
    /// Helper methods for CSV formatting
    /// </summary>
    public static class CsvHelper
    {
        /// <summary>
        /// Escapes a string for CSV output by wrapping in quotes and escaping internal quotes
        /// </summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}

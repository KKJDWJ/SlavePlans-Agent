namespace SlaveSplit.Infrastructure
{
    public static class KoreanParticleHelper
    {
        public static string Object(string value)
        {
            string text = (value ?? "").Trim();
            if (text.Length == 0) return text;
            return text + (HasBatchim(text) ? "을" : "를");
        }
        public static string Topic(string value) { string text = (value ?? "").Trim(); return text.Length == 0 ? text : text + (HasBatchim(text) ? "은" : "는"); }
        private static bool HasBatchim(string text) { char last = text[text.Length - 1]; return last >= 0xAC00 && last <= 0xD7A3 && ((last - 0xAC00) % 28) != 0; }
    }
}

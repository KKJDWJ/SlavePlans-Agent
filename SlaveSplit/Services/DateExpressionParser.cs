using System;
using System.Globalization;
using System.Text.RegularExpressions;
using SlaveSplit.Models;
namespace SlaveSplit.Services
{
    public interface IDateExpressionParser { bool TryParse(string expression, DateTime referenceDate, out DateRange range); DateRange GetWeek(DateTime date); }
    public sealed class DateExpressionParser : IDateExpressionParser
    {
        public DateRange GetWeek(DateTime date) { DateTime day = date.Date; int offset = ((int)day.DayOfWeek + 6) % 7; DateTime monday = day.AddDays(-offset); return new DateRange { StartDate = monday, EndDate = monday.AddDays(6) }; }
        public bool TryParse(string expression, DateTime referenceDate, out DateRange range)
        {
            range = null; string text = (expression ?? "").Trim(); DateTime today = referenceDate.Date;
            Match weekday = Regex.Match(text, "(다음\\s*주|이번\\s*주)\\s*(월|화|수|목|금|토|일)요일"); if (weekday.Success) { DateRange week = GetWeek(today); int day = "월화수목금토일".IndexOf(weekday.Groups[2].Value, StringComparison.Ordinal); DateTime target = week.StartDate.AddDays(day + (weekday.Groups[1].Value.Replace(" ", "") == "다음주" ? 7 : 0)); return Single(target, out range); }
            if (text.Contains("지난 주") || text.Contains("지난주")) { DateRange current = GetWeek(today); range = new DateRange { StartDate = current.StartDate.AddDays(-7), EndDate = current.EndDate.AddDays(-7) }; return true; }
            if (text.Contains("이번 주") || text.Contains("이번주")) { range = GetWeek(today); return true; }
            if (text.Contains("그제")) return Single(today.AddDays(-2), out range); if (text.Contains("어제")) return Single(today.AddDays(-1), out range); if (text.Contains("오늘")) return Single(today, out range); if (text.Contains("모레")) return Single(today.AddDays(2), out range); if (text.Contains("내일")) return Single(today.AddDays(1), out range);
            Match iso = Regex.Match(text, "(?<!\\d)(\\d{4})[-./](\\d{1,2})[-./](\\d{1,2})(?!\\d)"); DateTime value; if (iso.Success) return DateTime.TryParseExact(iso.Value.Replace('.', '-').Replace('/', '-'), "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out value) && Single(value, out range);
            Match monthDay = Regex.Match(text, "(?<!\\d)(\\d{1,2})\\s*(?:월|/)\\s*(\\d{1,2})\\s*일?"); if (monthDay.Success) return TryDate(referenceDate.Year, int.Parse(monthDay.Groups[1].Value), int.Parse(monthDay.Groups[2].Value), out value) && Single(value, out range);
            Match dayOnly = Regex.Match(text, "(?<!\\d)(\\d{1,2})\\s*일"); if (dayOnly.Success && TryDate(referenceDate.Year, referenceDate.Month, int.Parse(dayOnly.Groups[1].Value), out value)) return Single(value, out range); return false;
        }
        private static bool Single(DateTime date, out DateRange range) { range = new DateRange { StartDate = date.Date, EndDate = date.Date }; return true; }
        private static bool TryDate(int year, int month, int day, out DateTime value) { try { value = new DateTime(year, month, day); return true; } catch { value = default(DateTime); return false; } }
    }
}

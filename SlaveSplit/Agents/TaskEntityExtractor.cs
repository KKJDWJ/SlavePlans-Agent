using System;
using System.Text.RegularExpressions;

namespace SlaveSplit.Agents
{
    public sealed class TaskEntityExtractionResult
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool HasTitle { get { return !string.IsNullOrWhiteSpace(Title); } }
        public bool HasDescription { get { return !string.IsNullOrWhiteSpace(Description); } }
    }

    public sealed class TaskEntityExtractor
    {
        private static readonly Regex TitleLabel = new Regex("(?:^|[.\\r\\n])\\s*제목(?:은|으로|:)\\s*(.+?)(?=(?:\\s+(?:내용|설명|description|본문)(?:은|으로|:|에)?\\s*)|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex DescriptionLabel = new Regex("(?:^|[.\\r\\n]|\\s)(?:업무\\s*)?(?:내용|설명|description|본문)(?:은|으로|:|에)?\\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public TaskEntityExtractionResult ExtractForCreate(string input)
        {
            string text = input ?? "";
            Match title = TitleLabel.Match(text);
            Match description = DescriptionLabel.Match(text);
            int divider=text.IndexOf('<');string titleValue = divider>=0?text.Substring(0,divider):title.Success ? title.Groups[1].Value : StripCreateCommand(description.Success ? text.Substring(0, description.Index) : text);
            return new TaskEntityExtractionResult { Title = CleanTitle(titleValue), Description = description.Success ? CleanDescription(description.Groups[1].Value) : null };
        }

        public string ExtractDescription(string input)
        {
            Match match = DescriptionLabel.Match(input ?? "");
            if (match.Success) return CleanDescription(match.Groups[1].Value);
            match = Regex.Match(input ?? "", "(?:업무\\s*)?(?:내용|설명|description|본문)(?:에|을|은|:)\\s*(.+)라고\\s*(?:적어|써|작성|바꿔|수정|추가|넣어).*$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success) return CleanDescription(match.Groups[1].Value);
            match = Regex.Match(input ?? "", "(?:업무\\s*)?(?:내용|설명|description|본문)(?:에|을|은|:)\\s*(.+?)\\s*(?:적어|써|작성|바꿔|수정|추가|넣어)(?:해|해줘|해 주세요|줘)?[.!? ]*$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? CleanDescription(match.Groups[1].Value) : null;
        }

        private static string StripCreateCommand(string value)
        {
            string text = Regex.Replace(value ?? "", "^(?:오늘|내일)?\\s*(?:업무|할\\s*일|작업)?\\s*(?:하나)?\\s*(?:등록|추가|생성|만들)(?:해보자|하자|해|해줘|자)?[. :] *", "", RegexOptions.IgnoreCase);
            return text;
        }

        private static string CleanTitle(string value)
        {
            string text = (value ?? "").Trim();
            text = Regex.Replace(text, "^(?:일정|날짜)\\s*(?:없이|없음)\\s*", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "^(?:오늘|내일|어제|이번\\s*주|\\d{1,2}\\s*월\\s*\\d{1,2}\\s*일)\\s*", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s*(?:도\\s*)?(?:등록|추가|생성)(?:해)?하고\\s*\\d+\\s*(?:분|시간)\\s*전에\\s*(?:알려|알림).*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s*(?:도\\s*)?(?:등록|추가|생성)(?:해\\s*줘|해줘|해|하자)?\\s*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s*(?:이거|이걸|이것)?\\s*(?:오늘|내일)?\\s*업무로\\s*(?:등록|추가).*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s*(?:이라고|라고|제목으로|업무로|등록해|등록하자)\\s*$", "", RegexOptions.IgnoreCase);
            return text.Trim(' ', '.', '!', '?', '\r', '\n');
        }

        private static string CleanDescription(string value)
        {
            string text = (value ?? "").Trim();
            text = Regex.Replace(text, "\\s*<\\s*(?:이거야|이거|이렇게|이걸로|이것으로)\\s*[.!?]*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s+이거로\\s+하면.*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s*(?:(?:이거|이걸|이것)로?\\s*)?하면\\s*되.*$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s*(?:라고)?\\s*(?:적어|써|작성해|작성해줘|바꿔|수정해|추가해|추가해줘|넣어|넣어줘)\\s*[.!?]*$", "", RegexOptions.IgnoreCase);
            return text.Trim();
        }
    }
}

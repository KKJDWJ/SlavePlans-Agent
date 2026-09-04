using System.Text.RegularExpressions;
namespace SlaveSplit.Agents
{
    public sealed class IntentEvidence { public bool HasReadEvidence { get; set; } public bool HasCreateEvidence { get; set; } public bool HasUpdateEvidence { get; set; } public bool HasDeleteEvidence { get; set; } public bool HasStartEvidence { get; set; } public bool HasCompleteEvidence { get; set; } public string ReadEvidence { get; set; } public string WriteEvidence { get; set; } }
    public sealed class IntentSafetyEvaluator
    {
        public IntentEvidence Evaluate(string input)
        {
            string text = (input ?? "").Trim(); bool read = Regex.IsMatch(text, "(보여|알려|조회|확인(?:해|해줘|하고|$)|뭐야|뭐지|몇\\s*개|목록|현황|시간\\s*보여|제목.+(?:보여|알려))", RegexOptions.IgnoreCase); bool create = Regex.IsMatch(text, "(등록(?:해|하자|해보자|해줘|$)|추가(?:해|하자|해줘|$)|생성(?:해|하자|$)|만들(?:어|자|어줘)|할\\s*일(?:로|에)\\s*넣)", RegexOptions.IgnoreCase) && !Regex.IsMatch(text, "등록\\s*(?:시간|일시|날짜|된|돼)", RegexOptions.IgnoreCase);
            return new IntentEvidence { HasReadEvidence = read, HasCreateEvidence = create && !read, HasUpdateEvidence = Regex.IsMatch(text, "(변경해|수정해|바꿔|고쳐|내용에.+(?:넣어|추가해)|설명에.+(?:넣어|추가해))", RegexOptions.IgnoreCase), HasDeleteEvidence = Regex.IsMatch(text, "(삭제해|지워|없애|비워)", RegexOptions.IgnoreCase), HasStartEvidence = Regex.IsMatch(text, "(시작해|시작했|진행\\s*시작|진행\\s*중으로)", RegexOptions.IgnoreCase), HasCompleteEvidence = Regex.IsMatch(text, "(완료해|완료했|끝났|끝냈|처리\\s*완료|다\\s*했)", RegexOptions.IgnoreCase), ReadEvidence = read ? "SHOW_OR_QUERY" : "NONE", WriteEvidence = create ? "CREATE_ACTION" : "NONE" };
        }
    }
}

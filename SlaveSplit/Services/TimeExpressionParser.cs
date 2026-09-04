using System;
using System.Text.RegularExpressions;

namespace SlaveSplit.Services
{
    public sealed class ParsedTaskTime { public TimeSpan Time { get; set; } public int? ReminderOffsetMinutes { get; set; } }
    public sealed class TimeExpressionParser
    {
        public bool TryParse(string input,out ParsedTaskTime result)
        {
            result=null;string text=input??"";Match match=Regex.Match(text,"(?:(오전|오후)\\s*)?(?:(\\d{1,2}):(\\d{2})|(\\d{1,2})\\s*시(?:\\s*(?:(\\d{1,2})\\s*분|(반)))?)",RegexOptions.IgnoreCase);if(!match.Success)return false;
            int hour=int.Parse(match.Groups[2].Success?match.Groups[2].Value:match.Groups[4].Value);int minute=match.Groups[3].Success?int.Parse(match.Groups[3].Value):match.Groups[5].Success?int.Parse(match.Groups[5].Value):match.Groups[6].Success?30:0;string meridiem=match.Groups[1].Value;if(string.IsNullOrWhiteSpace(meridiem)&&!match.Groups[2].Success&&hour>=1&&hour<=12)return false;if(meridiem=="오후"&&hour<12)hour+=12;if(meridiem=="오전"&&hour==12)hour=0;if(hour<0||hour>23||minute<0||minute>59)return false;
            int? offset=null;Match reminder=Regex.Match(text,"(\\d+)\\s*(분|시간)\\s*전에\\s*(?:알려|알림)",RegexOptions.IgnoreCase);if(reminder.Success){int value=int.Parse(reminder.Groups[1].Value);offset=reminder.Groups[2].Value=="시간"?value*60:value;}
            result=new ParsedTaskTime{Time=new TimeSpan(hour,minute,0),ReminderOffsetMinutes=offset};return true;
        }
    }
}

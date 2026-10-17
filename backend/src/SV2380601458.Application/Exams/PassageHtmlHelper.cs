using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SV2380601458.Exams
{
    /// <summary>
    /// Chuyển đổi qua lại giữa HTML bài đọc (lưu trong CSDL) và dạng chữ để giảng viên sửa.
    /// Ảnh và bảng được tách riêng và giữ nguyên.
    /// </summary>
    public static class PassageHtmlHelper
    {
        private static readonly Regex PreservedRegex = new Regex(
            "<p class=\"passage-image\">.*?</p>|<table class=\"passage-table\">.*?</table>",
            RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ParagraphRegex = new Regex("<p>(.*?)</p>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex BlankSpanRegex = new Regex("<span class=\"blank\" data-q=\"(\\d+)\">.*?</span>", RegexOptions.Compiled);
        private static readonly Regex BlankTagRegex = new Regex(@"\[\s*BLANK\s+(\d+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<string> GetPreservedBlocks(string html)
        {
            return PreservedRegex.Matches(html ?? "").Cast<Match>().Select(m => m.Value).ToList();
        }

        public static string ToEditableText(string html)
        {
            var withoutPreserved = PreservedRegex.Replace(html ?? "", "");
            var lines = ParagraphRegex.Matches(withoutPreserved).Cast<Match>()
                .Select(m => BlankSpanRegex.Replace(m.Groups[1].Value, "[BLANK $1]"))
                .Select(WebUtility.HtmlDecode);
            return string.Join("\n", lines);
        }

        public static string TextToHtml(string text, out List<int> blankNumbers)
        {
            blankNumbers = new List<int>();
            var sb = new StringBuilder();
            foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var html = new StringBuilder();
                int last = 0;
                foreach (Match m in BlankTagRegex.Matches(line))
                {
                    html.Append(WebUtility.HtmlEncode(line.Substring(last, m.Index - last)));
                    var n = int.Parse(m.Groups[1].Value);
                    blankNumbers.Add(n);
                    html.Append($"<span class=\"blank\" data-q=\"{n}\">({n}) ________</span>");
                    last = m.Index + m.Length;
                }
                html.Append(WebUtility.HtmlEncode(line.Substring(last)));
                sb.Append("<p>").Append(html).Append("</p>");
            }
            return sb.ToString();
        }

        public static string ImageBlock(string contentType, string base64)
        {
            return $"<p class=\"passage-image\"><img src=\"data:{contentType};base64,{base64}\" alt=\"image\" /></p>";
        }
    }
}

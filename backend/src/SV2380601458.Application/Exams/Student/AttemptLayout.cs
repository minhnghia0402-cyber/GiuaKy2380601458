using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SV2380601458.Exams.Dto;

namespace SV2380601458.Exams.Student
{
    /// <summary>
    /// Thứ tự câu hỏi / đáp án của MỘT lượt thi sau khi hoán vị.
    /// Được lưu vào ExamAttempt.ShuffleMapJson để khi mở lại (mất mạng, tắt app) vẫn đúng thứ tự cũ,
    /// và để chấm bài / xem lại theo đúng những gì sinh viên đã thấy.
    /// </summary>
    public class AttemptLayout
    {
        public List<LayoutPart> Parts { get; set; } = new List<LayoutPart>();

        public IEnumerable<LayoutQuestion> AllQuestions => Parts.SelectMany(p => p.Groups).SelectMany(g => g.Questions);

        public string ToJson() => JsonSerializer.Serialize(this);

        public static AttemptLayout FromJson(string json) =>
            string.IsNullOrEmpty(json) ? new AttemptLayout() : JsonSerializer.Deserialize<AttemptLayout>(json);

        /// <summary>Tạo thứ tự mới theo các cờ hoán vị đã khai báo trong file Word</summary>
        public static AttemptLayout Build(ExamDetailDto tree, Random random)
        {
            var layout = new AttemptLayout();
            var allNumbers = tree.Parts.SelectMany(p => p.Questions.Concat(p.Groups.SelectMany(g => g.Questions)))
                .Select(q => q.QuestionNumber).ToList();
            var displayNumber = allNumbers.Count > 0 ? allNumbers.Min() : 1;

            foreach (var part in tree.Parts)
            {
                var lp = new LayoutPart { PartId = part.Id, PartNumber = part.PartNumber };

                // Part 5: câu độc lập, cờ hoán vị ở dòng PART áp dụng cho câu hỏi
                if (part.Questions.Count > 0)
                {
                    var questions = part.AllowShuffle ? Shuffle(part.Questions, random) : part.Questions;
                    var lg = new LayoutGroup { GroupId = null };
                    foreach (var q in questions) lg.Questions.Add(MakeQuestion(q, random, ref displayNumber));
                    lp.Groups.Add(lg);
                }

                // Part 6, 7: cờ ở dòng PART hoán vị thứ tự nhóm, cờ ở dòng GROUP hoán vị câu trong nhóm
                var groups = part.AllowShuffle ? Shuffle(part.Groups, random) : part.Groups;
                foreach (var g in groups)
                {
                    var questions = g.AllowShuffleQuestions ? Shuffle(g.Questions, random) : g.Questions;
                    var lg = new LayoutGroup { GroupId = g.Id };
                    foreach (var q in questions) lg.Questions.Add(MakeQuestion(q, random, ref displayNumber));
                    lp.Groups.Add(lg);
                }
                layout.Parts.Add(lp);
            }
            return layout;
        }

        private static LayoutQuestion MakeQuestion(QuestionDto q, Random random, ref int displayNumber)
        {
            var options = q.AllowShuffleAnswers ? Shuffle(q.Options, random) : q.Options;
            return new LayoutQuestion
            {
                QuestionId = q.Id,
                OriginalNumber = q.QuestionNumber,
                DisplayNumber = displayNumber++,
                OptionIds = options.Select(o => o.Id).ToList()
            };
        }

        private static IList<T> Shuffle<T>(IList<T> source, Random random)
        {
            var list = source.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static readonly Regex BlankSpan = new Regex("<span class=\"blank\" data-q=\"(\\d+)\">\\(\\d+\\) ________</span>");

        /// <summary>Đánh lại số các chỗ trống Part 6 trong bài đọc theo số hiển thị mới</summary>
        public static string RenumberBlanks(string html, Dictionary<int, int> originalToDisplay)
        {
            return BlankSpan.Replace(html ?? "", m =>
            {
                var original = int.Parse(m.Groups[1].Value);
                var shown = originalToDisplay.TryGetValue(original, out var d) ? d : original;
                return $"<span class=\"blank\" data-q=\"{shown}\">({shown}) ________</span>";
            });
        }
    }

    public class LayoutPart
    {
        public int PartId { get; set; }
        public int PartNumber { get; set; }
        public List<LayoutGroup> Groups { get; set; } = new List<LayoutGroup>();
    }

    public class LayoutGroup
    {
        public int? GroupId { get; set; }
        public List<LayoutQuestion> Questions { get; set; } = new List<LayoutQuestion>();
    }

    public class LayoutQuestion
    {
        public int QuestionId { get; set; }
        public int OriginalNumber { get; set; }
        public int DisplayNumber { get; set; }
        public List<int> OptionIds { get; set; } = new List<int>();
    }
}

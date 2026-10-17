using System.Collections.Generic;
using System.Linq;

namespace SV2380601458.Exams.Import
{
    /// <summary>
    /// Kiểm tra NỘI DUNG đề sau khi parser đã đọc xong.
    /// Trả về danh sách lỗi; danh sách rỗng nghĩa là file đúng 100% và được phép lưu.
    /// </summary>
    public class ExamValidator
    {
        // Phân bổ số câu chuẩn của TOEIC Reading
        private static readonly Dictionary<int, (int From, int To)> PartRanges = new Dictionary<int, (int, int)>
        {
            { 5, (101, 130) },
            { 6, (131, 146) },
            { 7, (147, 200) }
        };

        private static readonly string[] Labels = { "A", "B", "C", "D" };

        public List<string> Validate(ParsedExam exam)
        {
            var errors = new List<string>();
            if (!exam.HasExamTag) return errors; // parser đã báo lỗi thiếu [EXAM]

            // ---------- Thông tin chung ----------
            if (string.IsNullOrWhiteSpace(exam.Title))
                errors.Add("Thiếu [TITLE] (tên đề thi).");
            if (exam.DurationMinutes == null)
                errors.Add("Thiếu [DURATION] (thời gian làm bài, phút).");
            else if (exam.DurationMinutes < 1 || exam.DurationMinutes > 300)
                errors.Add($"[DURATION] = {exam.DurationMinutes} không hợp lệ (phải từ 1 đến 300 phút).");

            // ---------- Các Part ----------
            var partNumbers = exam.Parts.Select(p => p.PartNumber).Where(n => n != 0).ToList();
            if (partNumbers.Count == 0)
                errors.Add("Đề thi không có PART nào.");
            for (int i = 1; i < partNumbers.Count; i++)
                if (partNumbers[i] <= partNumbers[i - 1])
                    errors.Add($"Thứ tự Part sai: PART {partNumbers[i]} đứng sau PART {partNumbers[i - 1]}.");
            if (!exam.TestMode)
                foreach (var required in new[] { 5, 6, 7 })
                    if (!partNumbers.Contains(required))
                        errors.Add($"Thiếu PART {required}.");

            foreach (var part in exam.Parts.Where(p => p.PartNumber != 0))
            {
                if (part.Questions.Count == 0)
                    errors.Add($"PART {part.PartNumber} (dòng {part.Line}) không có câu hỏi nào.");

                foreach (var group in part.Groups)
                    ValidateGroup(part, group, errors);

                foreach (var q in part.Questions)
                    ValidateQuestion(q, errors);
            }

            // ---------- Số thứ tự câu trên toàn đề ----------
            var all = exam.Parts.SelectMany(p => p.Questions).Where(q => q.Number != 0).ToList();

            foreach (var dup in all.GroupBy(q => q.Number).Where(g => g.Count() > 1))
                errors.Add($"Câu {dup.Key} bị trùng số thứ tự (xuất hiện ở các dòng {string.Join(", ", dup.Select(q => q.Line))}).");

            for (int i = 1; i < all.Count; i++)
            {
                var prev = all[i - 1];
                var cur = all[i];
                if (cur.Number < prev.Number)
                    errors.Add($"Câu {cur.Number} (dòng {cur.Line}) đứng sau câu {prev.Number}: số thứ tự không tăng dần.");
                else if (!exam.TestMode && cur.Number > prev.Number + 1)
                    errors.Add(cur.Number == prev.Number + 2
                        ? $"Thiếu câu {prev.Number + 1} (giữa câu {prev.Number} và câu {cur.Number})."
                        : $"Thiếu các câu từ {prev.Number + 1} đến {cur.Number - 1}.");
            }

            foreach (var q in all)
            {
                if (PartRanges.TryGetValue(q.PartNumber, out var range) && (q.Number < range.From || q.Number > range.To))
                    errors.Add($"Câu {q.Number} (dòng {q.Line}) không thuộc phạm vi PART {q.PartNumber} (câu {range.From}-{range.To}).");
            }

            if (!exam.TestMode)
            {
                if (all.Count != 100)
                    errors.Add($"Đề TOEIC Reading phải có đúng 100 câu (101-200), file hiện có {all.Count} câu. " +
                               "Nếu đang thử nghiệm với file ngắn, thêm dòng [MODE] TEST sau [DURATION].");
                if (all.Count > 0 && all[0].Number != 101)
                    errors.Add($"Câu đầu tiên phải là câu 101, file bắt đầu từ câu {all[0].Number}.");
                if (all.Count > 0 && all[all.Count - 1].Number != 200)
                    errors.Add($"Câu cuối cùng phải là câu 200, file kết thúc ở câu {all[all.Count - 1].Number}.");
            }

            return errors;
        }

        private static void ValidateGroup(ParsedPart part, ParsedGroup group, List<string> errors)
        {
            var name = $"GROUP {group.FromNumber}-{group.ToNumber} (dòng {group.Line})";

            if (group.Passages.Count == 0)
                errors.Add($"{name}: thiếu bài đọc [PASSAGE].");
            foreach (var p in group.Passages.Where(p => p.Html.Length == 0))
                errors.Add($"{name}: bài đọc ở dòng {p.Line} đang trống.");

            if (group.Questions.Count == 0)
            {
                errors.Add($"{name}: không có câu hỏi nào.");
                return;
            }

            var expected = Enumerable.Range(group.FromNumber, group.ToNumber - group.FromNumber + 1).ToList();
            var actual = group.Questions.Select(q => q.Number).ToList();
            if (group.FromNumber > 0 && !expected.SequenceEqual(actual))
                errors.Add($"{name}: khai báo câu {group.FromNumber}-{group.ToNumber} nhưng thực tế có các câu [{string.Join(", ", actual)}].");

            // Part 6: mỗi câu phải có đúng một chỗ trống [BLANK n] trong bài đọc và ngược lại
            if (part.PartNumber == 6)
            {
                var blanks = group.Passages.SelectMany(p => p.BlankNumbers).ToList();
                foreach (var n in actual.Where(n => !blanks.Contains(n)))
                    errors.Add($"{name}: câu {n} không có chỗ trống [BLANK {n}] tương ứng trong bài đọc.");
                foreach (var n in blanks.Distinct().Where(n => !actual.Contains(n)))
                    errors.Add($"{name}: bài đọc có [BLANK {n}] nhưng không có câu hỏi {n}.");
                foreach (var n in blanks.GroupBy(b => b).Where(g => g.Count() > 1).Select(g => g.Key))
                    errors.Add($"{name}: [BLANK {n}] xuất hiện nhiều lần.");
            }
        }

        private static void ValidateQuestion(ParsedQuestion q, List<string> errors)
        {
            var name = $"Câu {q.Number} (dòng {q.Line})";

            // Part 6 câu hỏi chỉ là số thứ tự chỗ trống nên không bắt buộc nội dung
            if (q.PartNumber != 6 && q.Content.Length == 0)
                errors.Add($"{name}: thiếu nội dung câu hỏi.");

            var labels = q.Options.Select(o => o.Label).ToList();
            if (q.Options.Count != 4)
                errors.Add($"{name}: phải có đúng 4 đáp án (A)-(D), hiện có {q.Options.Count}.");
            else if (!labels.SequenceEqual(Labels))
                errors.Add($"{name}: các đáp án phải theo thứ tự (A) (B) (C) (D), hiện là ({string.Join(") (", labels)}).");

            foreach (var o in q.Options.Where(o => string.IsNullOrWhiteSpace(o.Content)))
                errors.Add($"{name}: đáp án ({o.Label}) ở dòng {o.Line} chưa có nội dung.");

            if (!q.AnswerSet || string.IsNullOrWhiteSpace(q.Answer))
                errors.Add($"{name}: thiếu [ANSWER] (đáp án đúng).");
            else if (!Labels.Contains(q.Answer))
                errors.Add($"{name}: [ANSWER] = \"{q.Answer}\" không hợp lệ, chỉ nhận A, B, C hoặc D.");
            else if (!labels.Contains(q.Answer))
                errors.Add($"{name}: [ANSWER] = {q.Answer} nhưng câu này không có đáp án ({q.Answer}).");

            if (!q.HintSet || q.Hint.ToString().Trim().Length == 0)
                errors.Add($"{name}: thiếu [HINT] (gợi ý đáp án).");
        }
    }
}

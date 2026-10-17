using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

namespace SV2380601458.Exams.Import
{
    /// <summary>
    /// Đọc file .docx theo bộ tag và dựng ParsedExam.
    /// Lỗi CÚ PHÁP (tag sai chỗ, thiếu đóng tag...) được ghi lại ở đây kèm số dòng.
    /// Lỗi NỘI DUNG (thiếu đáp án, sai số thứ tự câu...) do ExamValidator kiểm tra sau.
    /// Parser không dừng ở lỗi đầu tiên mà đọc hết file để báo TẤT CẢ lỗi một lần.
    /// </summary>
    public class ExamDocxParser
    {
        private static readonly Regex TagRegex =
            new Regex(@"^\[\s*(/?[A-Za-z_]+)(?:\s+([^\]]*?))?\s*\]", RegexOptions.Compiled);
        private static readonly Regex OptionRegex =
            new Regex(@"^\(\s*([A-Da-d])\s*\)\s*(.*)$", RegexOptions.Compiled);
        private static readonly Regex BlankRegex =
            new Regex(@"\[\s*BLANK\s+(\d+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RangeRegex =
            new Regex(@"^(\d+)\s*-\s*(\d+)$", RegexOptions.Compiled);

        private static readonly HashSet<string> MainTags = new HashSet<string>
        {
            "EXAM", "/EXAM", "TITLE", "DURATION", "MODE",
            "PART", "/PART", "DIRECTIONS",
            "GROUP", "/GROUP", "PASSAGE", "/PASSAGE", "IMAGE",
            "Q", "/Q", "ANSWER", "HINT"
        };

        private static readonly HashSet<string> FlagTags = new HashSet<string>
        {
            "SHUFFLE_Q", "NO_SHUFFLE_Q", "SHUFFLE_A", "NO_SHUFFLE_A"
        };

        // ----- trạng thái khi đọc -----
        private ParseResult _result;
        private MainDocumentPart _mainPart;
        private int _line;
        private string _lineText;
        private bool _started;
        private ParsedPart _part;
        private ParsedGroup _group;
        private ParsedPassage _passage;
        private ParsedQuestion _question;

        public ParseResult Parse(byte[] fileContent)
        {
            _result = new ParseResult();
            _line = 0;
            _started = false;
            _part = null; _group = null; _passage = null; _question = null;

            var ms = new MemoryStream(fileContent);
            WordprocessingDocument doc;
            try
            {
                // Chỉ bắt lỗi ở bước MỞ file; lỗi lập trình bên dưới vẫn hiện ra để dễ sửa
                doc = WordprocessingDocument.Open(ms, false);
            }
            catch (Exception)
            {
                ms.Dispose();
                _result.Errors.Add("Không đọc được file. Hãy chắc chắn đây là file Word .docx hợp lệ (không phải .doc đổi đuôi).");
                return _result;
            }

            using (ms)
            using (doc)
            {
                _mainPart = doc.MainDocumentPart;
                var body = _mainPart?.Document?.Body;
                if (body == null)
                {
                    _result.Errors.Add("File Word không có nội dung.");
                    return _result;
                }

                foreach (var block in FlattenBlocks(body))
                {
                    if (block is Paragraph p) ProcessParagraph(p);
                    else if (block is Table t) ProcessTable(t);
                }
            }

            if (!_result.Exam.HasExamTag)
                _result.Errors.Add("Không tìm thấy tag [EXAM] mở đầu đề thi.");
            else if (!_result.Exam.ExamClosed)
            {
                CloseAllOpen(_line);
                _result.Errors.Add("Thiếu tag [/EXAM] ở cuối đề thi.");
            }

            return _result;
        }

        // Lấy Paragraph/Table theo đúng thứ tự, kể cả nằm trong content control (SdtBlock)
        private static IEnumerable<OpenXmlElement> FlattenBlocks(OpenXmlElement parent)
        {
            foreach (var el in parent.ChildElements)
            {
                if (el is Paragraph || el is Table) yield return el;
                else if (el is SdtBlock sdt && sdt.SdtContentBlock != null)
                    foreach (var inner in FlattenBlocks(sdt.SdtContentBlock)) yield return inner;
            }
        }

        // ================== ĐỌC TỪNG ĐOẠN ==================

        private void ProcessParagraph(Paragraph p)
        {
            var text = GetParagraphText(p);
            var hasNumbering = p.ParagraphProperties?.NumberingProperties != null;

            // Shift+Enter (xuống dòng mềm) được tách thành nhiều dòng
            var lines = text.Split('\n');
            foreach (var raw in lines)
            {
                _line++;
                _lineText = raw.Trim();
                ProcessLine(_lineText, hasNumbering);
            }

            var images = p.Descendants<A.Blip>().ToList();
            if (images.Count > 0) ProcessImages(images);
        }

        private static string GetParagraphText(Paragraph p)
        {
            var sb = new StringBuilder();
            foreach (var run in p.Descendants<Run>())
            {
                foreach (var child in run.ChildElements)
                {
                    switch (child)
                    {
                        case Text t: sb.Append(t.Text); break;
                        case TabChar _: sb.Append(' '); break;
                        case Break _:
                        case CarriageReturn _: sb.Append('\n'); break;
                    }
                }
            }
            return sb.ToString().Replace(' ', ' ');
        }

        private void ProcessLine(string line, bool hasNumbering)
        {
            if (line.Length == 0) return;

            // Mọi nội dung trước [EXAM] (vd: phần hướng dẫn) được bỏ qua
            if (!_started)
            {
                var m = TagRegex.Match(line);
                if (m.Success && m.Groups[1].Value.ToUpperInvariant() == "EXAM")
                {
                    _started = true;
                    _result.Exam.HasExamTag = true;
                }
                return;
            }
            if (_result.Exam.ExamClosed) return; // bỏ qua nội dung sau [/EXAM]

            if (TryReadTags(line, out var tags, out var value))
            {
                if (tags.Count > 0) HandleTags(tags, value);
                return;
            }

            HandleText(line, hasNumbering);
        }

        /// <summary>
        /// Tách các tag ở đầu dòng. VD: "[Q 101] [SHUFFLE_A]" → Q(101), SHUFFLE_A.
        /// "[ANSWER] B" → ANSWER, value = "B".
        /// Trả về false nếu dòng là văn bản thường.
        /// </summary>
        private bool TryReadTags(string line, out List<(string Name, string Arg)> tags, out string value)
        {
            tags = new List<(string, string)>();
            value = null;

            var first = TagRegex.Match(line);
            if (!first.Success) return false;

            var firstName = first.Groups[1].Value.ToUpperInvariant();

            if (!MainTags.Contains(firstName) && !FlagTags.Contains(firstName))
            {
                // Trong bài đọc, "[BLANK 131]" hay "[1]" đầu dòng là văn bản bình thường
                if (_passage != null) return false;
                if (firstName == "BLANK")
                {
                    AddError("[BLANK n] chỉ được dùng bên trong [PASSAGE] ... [/PASSAGE].");
                    return true;
                }
                AddError($"Tag không hợp lệ \"[{first.Groups[1].Value}]\".");
                return true;
            }

            tags.Add((firstName, first.Groups[2].Value.Trim()));
            var rest = line.Substring(first.Length).TrimStart();

            // Các tag phía sau chỉ có thể là cờ hoán vị
            while (true)
            {
                var m = TagRegex.Match(rest);
                if (!m.Success) break;
                var name = m.Groups[1].Value.ToUpperInvariant();
                if (!FlagTags.Contains(name)) break;
                tags.Add((name, m.Groups[2].Value.Trim()));
                rest = rest.Substring(m.Length).TrimStart();
            }

            value = rest.Trim();
            return true;
        }

        // ================== XỬ LÝ TAG ==================

        private void HandleTags(List<(string Name, string Arg)> tags, string value)
        {
            var (name, arg) = tags[0];
            var flags = tags.Skip(1).Select(t => t.Name).ToList();
            if (FlagTags.Contains(name))
            {
                AddError($"Tag [{name}] phải nằm cùng dòng với [PART], [GROUP] hoặc [Q].");
                return;
            }

            switch (name)
            {
                case "EXAM":
                    AddError("Tag [EXAM] xuất hiện lần thứ hai.");
                    break;

                case "/EXAM":
                    CloseAllOpen(_line);
                    _result.Exam.ExamClosed = true;
                    break;

                case "TITLE":
                    if (_part != null) { AddError("[TITLE] phải đặt trước [PART] đầu tiên."); break; }
                    if (string.IsNullOrWhiteSpace(value)) AddError("[TITLE] chưa có tên đề.");
                    else _result.Exam.Title = value;
                    break;

                case "DURATION":
                    if (_part != null) { AddError("[DURATION] phải đặt trước [PART] đầu tiên."); break; }
                    if (int.TryParse(value, out var minutes)) _result.Exam.DurationMinutes = minutes;
                    else AddError($"[DURATION] phải là số phút, nhận được \"{value}\".");
                    break;

                case "MODE":
                    if (string.Equals(value, "TEST", StringComparison.OrdinalIgnoreCase)) _result.Exam.TestMode = true;
                    else AddError("[MODE] chỉ nhận giá trị TEST.");
                    break;

                case "PART": OpenPart(arg, flags); break;
                case "/PART": ClosePart(); break;

                case "DIRECTIONS":
                    if (_part == null || _group != null || _question != null)
                        AddError("[DIRECTIONS] phải nằm ngay trong [PART], ngoài GROUP và câu hỏi.");
                    else _part.Directions = value;
                    break;

                case "GROUP": OpenGroup(arg, flags); break;
                case "/GROUP": CloseGroup(); break;

                case "PASSAGE": OpenPassage(); break;
                case "/PASSAGE": ClosePassage(); break;

                case "IMAGE":
                    if (_passage == null) { AddError("[IMAGE] chỉ được dùng bên trong [PASSAGE]."); break; }
                    _passage.PendingImage = true;
                    _passage.PendingImageLine = _line;
                    break;

                case "Q": OpenQuestion(arg, flags); break;
                case "/Q": CloseQuestion(); break;

                case "ANSWER":
                    if (_question == null) { AddError("[ANSWER] phải nằm trong [Q] ... [/Q]."); break; }
                    if (_question.AnswerSet) AddError($"Câu {_question.Number} có nhiều hơn một [ANSWER].");
                    _question.AnswerSet = true;
                    _question.Answer = value.Trim('(', ')', ' ').ToUpperInvariant();
                    break;

                case "HINT":
                    if (_question == null) { AddError("[HINT] phải nằm trong [Q] ... [/Q]."); break; }
                    if (_question.HintSet) AddError($"Câu {_question.Number} có nhiều hơn một [HINT].");
                    _question.HintSet = true;
                    _question.Hint.Append(value);
                    break;
            }
        }

        private void OpenPart(string arg, List<string> flags)
        {
            if (_part != null)
            {
                AddError($"Part {_part.PartNumber} chưa có [/PART] trước khi mở Part mới.");
                ClosePart();
            }
            if (!int.TryParse(arg, out var number) || number < 5 || number > 7)
            {
                AddError($"[PART {arg}] không hợp lệ. Chỉ có PART 5, 6 hoặc 7.");
                number = 0;
            }
            if (number != 0 && _result.Exam.Parts.Any(p => p.PartNumber == number))
                AddError($"PART {number} bị khai báo hai lần.");

            CheckFlags(flags, "SHUFFLE_Q", "NO_SHUFFLE_Q", "PART");
            _part = new ParsedPart
            {
                PartNumber = number,
                Line = _line,
                AllowShuffle = flags.Contains("SHUFFLE_Q")
            };
            _result.Exam.Parts.Add(_part);
        }

        private void ClosePart()
        {
            if (_part == null) { AddError("[/PART] không có [PART] tương ứng."); return; }
            if (_group != null) { AddError($"GROUP {_group.FromNumber}-{_group.ToNumber} chưa có [/GROUP]."); CloseGroup(); }
            if (_question != null) CloseQuestion(missingClose: true);
            _part = null;
        }

        private void OpenGroup(string arg, List<string> flags)
        {
            if (_part == null) { AddError("[GROUP] phải nằm trong [PART]."); return; }
            if (_part.PartNumber == 5) AddError("PART 5 là câu độc lập, không dùng [GROUP].");
            if (_group != null)
            {
                AddError($"GROUP {_group.FromNumber}-{_group.ToNumber} chưa có [/GROUP] trước khi mở GROUP mới.");
                CloseGroup();
            }

            var m = RangeRegex.Match(arg ?? "");
            int from = 0, to = 0;
            if (!m.Success) AddError($"[GROUP {arg}] sai định dạng, phải có dạng [GROUP 131-134].");
            else
            {
                from = int.Parse(m.Groups[1].Value);
                to = int.Parse(m.Groups[2].Value);
                if (from > to) AddError($"[GROUP {arg}]: số đầu lớn hơn số cuối.");
            }

            CheckFlags(flags, "SHUFFLE_Q", "NO_SHUFFLE_Q", "GROUP");
            _group = new ParsedGroup
            {
                FromNumber = from,
                ToNumber = to,
                Line = _line,
                AllowShuffleQuestions = flags.Contains("SHUFFLE_Q")
            };
            _part.Groups.Add(_group);
        }

        private void CloseGroup()
        {
            if (_group == null) { AddError("[/GROUP] không có [GROUP] tương ứng."); return; }
            if (_passage != null) { AddError("Bài đọc chưa có [/PASSAGE]."); ClosePassage(); }
            if (_question != null) CloseQuestion(missingClose: true);
            _group = null;
        }

        private void OpenPassage()
        {
            if (_group == null) { AddError("[PASSAGE] phải nằm trong [GROUP]."); return; }
            if (_question != null) { AddError("[PASSAGE] phải đặt trước các câu hỏi của GROUP."); return; }
            if (_passage != null) { AddError("Bài đọc trước chưa có [/PASSAGE]."); ClosePassage(); }
            _passage = new ParsedPassage { Line = _line };
            _group.Passages.Add(_passage);
        }

        private void ClosePassage()
        {
            if (_passage == null) { AddError("[/PASSAGE] không có [PASSAGE] tương ứng."); return; }
            if (_passage.PendingImage)
                _result.Errors.Add($"Dòng {_passage.PendingImageLine}: có [IMAGE] nhưng không có ảnh nào được dán ngay sau.");
            _passage = null;
        }

        private void OpenQuestion(string arg, List<string> flags)
        {
            if (_part == null) { AddError("[Q] phải nằm trong [PART]."); return; }
            if (_part.PartNumber != 5 && _group == null)
                AddError($"Câu hỏi của PART {_part.PartNumber} phải nằm trong [GROUP].");
            if (_passage != null) { AddError("Bài đọc chưa có [/PASSAGE] trước câu hỏi."); ClosePassage(); }
            if (_question != null) CloseQuestion(missingClose: true);

            if (!int.TryParse(arg, out var number))
            {
                AddError($"[Q {arg}] không hợp lệ, số câu phải là số nguyên. VD: [Q 101].");
                number = 0;
            }

            CheckFlags(flags, "SHUFFLE_A", "NO_SHUFFLE_A", "Q");
            _question = new ParsedQuestion
            {
                Number = number,
                Line = _line,
                PartNumber = _part.PartNumber,
                AllowShuffleAnswers = flags.Contains("SHUFFLE_A"),
                Group = _group
            };
            _part.Questions.Add(_question);
            _group?.Questions.Add(_question);
        }

        private void CloseQuestion(bool missingClose = false)
        {
            if (_question == null) { AddError("[/Q] không có [Q] tương ứng."); return; }
            if (missingClose)
                _result.Errors.Add($"Câu {_question.Number} (dòng {_question.Line}): thiếu tag đóng [/Q].");
            _question = null;
        }

        private void CloseAllOpen(int line)
        {
            if (_part != null)
            {
                AddError($"Part {_part.PartNumber} chưa có [/PART].");
                ClosePart();
            }
        }

        private void CheckFlags(List<string> flags, string allowYes, string allowNo, string tagName)
        {
            foreach (var f in flags.Where(f => f != allowYes && f != allowNo))
                AddError($"Tag [{f}] không dùng được trên dòng [{tagName}]. Chỉ dùng [{allowYes}] hoặc [{allowNo}].");
            if (flags.Contains(allowYes) && flags.Contains(allowNo))
                AddError($"Không được dùng cùng lúc [{allowYes}] và [{allowNo}].");
        }

        // ================== VĂN BẢN THƯỜNG ==================

        private void HandleText(string line, bool hasNumbering)
        {
            if (_question != null)
            {
                var opt = OptionRegex.Match(line);
                if (opt.Success)
                {
                    if (_question.AnswerSet || _question.HintSet)
                        AddError($"Câu {_question.Number}: đáp án ({opt.Groups[1].Value.ToUpper()}) phải đặt trước [ANSWER] và [HINT].");
                    _question.Options.Add(new ParsedOption
                    {
                        Label = opt.Groups[1].Value.ToUpperInvariant(),
                        Content = opt.Groups[2].Value.Trim(),
                        Line = _line
                    });
                    return;
                }

                if (hasNumbering)
                {
                    AddError($"Câu {_question.Number}: dòng này đang dùng danh sách đánh số tự động của Word, " +
                             "chương trình không đọc được nhãn (A)(B)(C)(D). Hãy tắt numbering và gõ tay \"(A) ...\".");
                    return;
                }

                if (_question.HintSet) { _question.Hint.Append(' ').Append(line); return; }

                if (_question.Options.Count > 0 || _question.AnswerSet)
                {
                    AddError($"Câu {_question.Number}: nội dung lạ nằm sau các đáp án.");
                    return;
                }

                if (_question.Content.Length > 0) _question.Content.Append('\n');
                _question.Content.Append(line);
                return;
            }

            if (_passage != null)
            {
                AppendPassageText(line);
                return;
            }

            AddError(hasNumbering
                ? "Nội dung dùng danh sách đánh số tự động của Word và nằm ngoài câu hỏi."
                : "Nội dung không thuộc tag nào (phải nằm trong [PASSAGE] hoặc [Q] ... [/Q]).");
        }

        private void AppendPassageText(string line)
        {
            var html = new StringBuilder();
            int last = 0;
            foreach (Match m in BlankRegex.Matches(line))
            {
                html.Append(WebUtility.HtmlEncode(line.Substring(last, m.Index - last)));
                var n = int.Parse(m.Groups[1].Value);
                _passage.BlankNumbers.Add(n);
                html.Append($"<span class=\"blank\" data-q=\"{n}\">({n}) ________</span>");
                last = m.Index + m.Length;
            }
            html.Append(WebUtility.HtmlEncode(line.Substring(last)));
            _passage.Html.Append("<p>").Append(html).Append("</p>");
        }

        // ================== ẢNH & BẢNG ==================

        private void ProcessImages(List<A.Blip> blips)
        {
            if (!_started || _result.Exam.ExamClosed) return;
            if (_passage == null)
            {
                AddError("Có ảnh nằm ngoài [PASSAGE]. Ảnh phải được dán bên trong bài đọc, ngay sau [IMAGE].");
                return;
            }

            foreach (var blip in blips)
            {
                var relId = blip.Embed?.Value;
                if (string.IsNullOrEmpty(relId)) continue;
                ImagePart imagePart = null;
                try { imagePart = _mainPart.GetPartById(relId) as ImagePart; }
                catch (Exception) { /* ảnh liên kết ngoài, không nằm trong file */ }
                if (imagePart == null)
                {
                    AddError("Không đọc được ảnh trong bài đọc (có thể là ảnh liên kết ngoài, hãy dán ảnh trực tiếp).");
                    continue;
                }

                byte[] bytes;
                using (var s = imagePart.GetStream())
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    bytes = ms.ToArray();
                }

                // Lưu ảnh dạng base64 ngay trong HTML: không cần quản lý file trên server,
                // web và app mobile đều hiển thị được.
                _passage.Html.Append(
                    $"<p class=\"passage-image\"><img src=\"data:{imagePart.ContentType};base64,{Convert.ToBase64String(bytes)}\" alt=\"image\" /></p>");
            }
            _passage.PendingImage = false;
        }

        private void ProcessTable(Table table)
        {
            if (!_started || _result.Exam.ExamClosed) return;   // bảng hướng dẫn trước [EXAM] bỏ qua
            _line++;
            _lineText = "(bảng)";
            if (_passage == null)
            {
                AddError("Bảng (table) chỉ được đặt bên trong [PASSAGE].");
                return;
            }

            var sb = new StringBuilder("<table class=\"passage-table\">");
            foreach (var row in table.Elements<TableRow>())
            {
                sb.Append("<tr>");
                foreach (var cell in row.Elements<TableCell>())
                {
                    var cellText = string.Join("<br/>", cell.Elements<Paragraph>()
                        .Select(GetParagraphText)
                        .Where(t => t.Trim().Length > 0)
                        .Select(t => WebUtility.HtmlEncode(t.Trim())));
                    sb.Append("<td>").Append(cellText).Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            _passage.Html.Append(sb);
        }

        // ================== TIỆN ÍCH ==================

        private void AddError(string message)
        {
            var snippet = _lineText ?? "";
            if (snippet.Length > 60) snippet = snippet.Substring(0, 60) + "...";
            _result.Errors.Add($"Dòng {_line} «{snippet}»: {message}");
        }
    }
}

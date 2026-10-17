using System.Collections.Generic;
using System.Text;

namespace SV2380601458.Exams.Import
{
    // Mô hình trung gian: kết quả đọc file Word, CHƯA lưu vào CSDL.
    // Chỉ khi ExamValidator không còn lỗi thì mới chuyển sang Entity để lưu.

    public class ParsedExam
    {
        public bool HasExamTag { get; set; }
        public bool ExamClosed { get; set; }
        public string Title { get; set; }
        public int? DurationMinutes { get; set; }

        /// <summary>[MODE] TEST: bỏ qua kiểm tra đủ 100 câu (chỉ dùng khi phát triển)</summary>
        public bool TestMode { get; set; }

        public List<ParsedPart> Parts { get; } = new List<ParsedPart>();
    }

    public class ParsedPart
    {
        public int PartNumber { get; set; }
        public int Line { get; set; }
        public string Directions { get; set; }
        public bool AllowShuffle { get; set; }
        public List<ParsedGroup> Groups { get; } = new List<ParsedGroup>();

        /// <summary>Tất cả câu hỏi của Part theo đúng thứ tự trong file</summary>
        public List<ParsedQuestion> Questions { get; } = new List<ParsedQuestion>();
    }

    public class ParsedGroup
    {
        public int FromNumber { get; set; }
        public int ToNumber { get; set; }
        public int Line { get; set; }
        public bool AllowShuffleQuestions { get; set; }
        public List<ParsedPassage> Passages { get; } = new List<ParsedPassage>();
        public List<ParsedQuestion> Questions { get; } = new List<ParsedQuestion>();
    }

    public class ParsedPassage
    {
        public int Line { get; set; }
        public StringBuilder Html { get; } = new StringBuilder();

        /// <summary>Các số câu xuất hiện dưới dạng [BLANK n] trong bài đọc</summary>
        public List<int> BlankNumbers { get; } = new List<int>();

        /// <summary>Đã gặp [IMAGE] nhưng chưa thấy ảnh theo sau</summary>
        public bool PendingImage { get; set; }
        public int PendingImageLine { get; set; }
    }

    public class ParsedQuestion
    {
        public int Number { get; set; }
        public int Line { get; set; }
        public int PartNumber { get; set; }
        public bool AllowShuffleAnswers { get; set; }
        public StringBuilder Content { get; } = new StringBuilder();
        public List<ParsedOption> Options { get; } = new List<ParsedOption>();
        public string Answer { get; set; }
        public bool AnswerSet { get; set; }
        public StringBuilder Hint { get; } = new StringBuilder();
        public bool HintSet { get; set; }
        public ParsedGroup Group { get; set; }
    }

    public class ParsedOption
    {
        public string Label { get; set; }
        public string Content { get; set; }
        public int Line { get; set; }
    }

    public class ParseResult
    {
        public ParsedExam Exam { get; } = new ParsedExam();
        public List<string> Errors { get; } = new List<string>();
    }
}

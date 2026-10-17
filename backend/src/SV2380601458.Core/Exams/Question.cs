using System.Collections.Generic;
using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>Một câu hỏi ([Q 101] ... [/Q])</summary>
    public class Question : Entity<int>
    {
        public int ExamPartId { get; set; }
        public virtual ExamPart ExamPart { get; set; }

        /// <summary>Null với Part 5 (câu độc lập)</summary>
        public int? QuestionGroupId { get; set; }
        public virtual QuestionGroup QuestionGroup { get; set; }

        /// <summary>Số câu trong đề gốc: 101..200</summary>
        public int QuestionNumber { get; set; }

        /// <summary>Nội dung câu hỏi. Part 6 để null (câu hỏi chỉ là số thứ tự chỗ trống)</summary>
        public string Content { get; set; }

        /// <summary>Tag [HINT]</summary>
        public string Hint { get; set; }

        /// <summary>Tag [SHUFFLE_A]</summary>
        public bool AllowShuffleAnswers { get; set; }

        public int OrderIndex { get; set; }

        public virtual ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();
    }
}

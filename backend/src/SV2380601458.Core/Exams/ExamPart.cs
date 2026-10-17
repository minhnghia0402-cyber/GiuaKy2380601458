using System.Collections.Generic;
using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>Một Part của đề (5, 6 hoặc 7)</summary>
    public class ExamPart : Entity<int>
    {
        public int ExamId { get; set; }
        public virtual Exam Exam { get; set; }

        /// <summary>5, 6 hoặc 7</summary>
        public int PartNumber { get; set; }

        public string Directions { get; set; }

        /// <summary>
        /// Tag [SHUFFLE_Q] ở dòng PART.
        /// Part 5: hoán vị thứ tự câu hỏi. Part 6/7: hoán vị thứ tự các GROUP.
        /// </summary>
        public bool AllowShuffle { get; set; }

        public int OrderIndex { get; set; }

        public virtual ICollection<QuestionGroup> Groups { get; set; } = new List<QuestionGroup>();

        /// <summary>Tất cả câu hỏi thuộc Part (Part 5 không có Group)</summary>
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}

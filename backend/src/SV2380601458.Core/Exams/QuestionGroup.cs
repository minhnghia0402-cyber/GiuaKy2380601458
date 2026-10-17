using System.Collections.Generic;
using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>Nhóm câu hỏi dùng chung bài đọc (Part 6, 7). VD: [GROUP 131-134]</summary>
    public class QuestionGroup : Entity<int>
    {
        public int ExamPartId { get; set; }
        public virtual ExamPart ExamPart { get; set; }

        public int FromNumber { get; set; }
        public int ToNumber { get; set; }

        /// <summary>Tag [SHUFFLE_Q] ở dòng GROUP: hoán vị câu hỏi trong nhóm</summary>
        public bool AllowShuffleQuestions { get; set; }

        public int OrderIndex { get; set; }

        /// <summary>Part 7 có thể có 2-3 bài đọc (double/triple passage)</summary>
        public virtual ICollection<Passage> Passages { get; set; } = new List<Passage>();

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}

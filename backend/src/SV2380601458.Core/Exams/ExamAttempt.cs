using System;
using System.Collections.Generic;
using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>Một lượt làm bài của sinh viên</summary>
    public class ExamAttempt : Entity<long>
    {
        public int ExamId { get; set; }
        public virtual Exam Exam { get; set; }

        /// <summary>Id người dùng trong bảng AbpUsers</summary>
        public long UserId { get; set; }

        public DateTime StartTime { get; set; }

        /// <summary>Hạn nộp do SERVER tính = StartTime + DurationMinutes (client không tự quyết)</summary>
        public DateTime EndTime { get; set; }

        public DateTime? SubmitTime { get; set; }

        public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int SkippedCount { get; set; }

        /// <summary>
        /// Thứ tự câu hỏi / đáp án sau khi hoán vị (JSON) để lần mở lại vẫn thấy đúng thứ tự cũ.
        /// </summary>
        public string ShuffleMapJson { get; set; }

        public virtual ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
    }
}

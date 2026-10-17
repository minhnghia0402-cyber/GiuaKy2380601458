using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;

namespace SV2380601458.Exams
{
    /// <summary>Đề thi TOEIC Reading (gốc của cây: Exam > Part > Group > Question > Option)</summary>
    public class Exam : FullAuditedEntity<int>
    {
        public const int MaxTitleLength = 256;
        public const int MaxFileNameLength = 256;

        [Required]
        [StringLength(MaxTitleLength)]
        public string Title { get; set; }

        public string Description { get; set; }

        /// <summary>Thời gian làm bài (phút), mặc định 75</summary>
        public int DurationMinutes { get; set; } = 75;

        public int TotalQuestions { get; set; }

        public ExamStatus Status { get; set; } = ExamStatus.Draft;

        /// <summary>Tên file .docx đã import</summary>
        [StringLength(MaxFileNameLength)]
        public string SourceFileName { get; set; }

        public DateTime? PublishedTime { get; set; }

        public virtual ICollection<ExamPart> Parts { get; set; } = new List<ExamPart>();

        public virtual ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
    }
}

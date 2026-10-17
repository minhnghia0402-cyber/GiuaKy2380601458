using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SV2380601458.Exams.Dto
{
    public class ImportExamInput
    {
        [Required]
        [StringLength(Exam.MaxFileNameLength)]
        public string FileName { get; set; }

        [Required]
        public byte[] Content { get; set; }
    }

    public class ImportExamResultDto
    {
        public bool Success { get; set; }
        public int? ExamId { get; set; }
        public string Title { get; set; }
        public int QuestionCount { get; set; }
        public bool TestMode { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ExamListDto : EntityDto<int>
    {
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public ExamStatus Status { get; set; }
        public string SourceFileName { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? PublishedTime { get; set; }
        public int AttemptCount { get; set; }
    }
}

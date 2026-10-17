using System;
using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>Câu trả lời của sinh viên cho 1 câu hỏi trong 1 lượt làm bài</summary>
    public class AttemptAnswer : Entity<long>
    {
        public long ExamAttemptId { get; set; }
        public virtual ExamAttempt ExamAttempt { get; set; }

        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }

        /// <summary>Null = chưa chọn / đã huỷ chọn (tính là câu bỏ qua)</summary>
        public int? SelectedOptionId { get; set; }
        public virtual AnswerOption SelectedOption { get; set; }

        /// <summary>Được tính khi nộp bài</summary>
        public bool IsCorrect { get; set; }

        public DateTime? AnsweredTime { get; set; }
    }
}

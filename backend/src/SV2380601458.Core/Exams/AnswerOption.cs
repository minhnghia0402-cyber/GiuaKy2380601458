using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>
    /// Một lựa chọn (A)-(D). Đáp án đúng lưu bằng IsCorrect thay vì lưu chữ "B"
    /// vì khi hoán vị đáp án, chữ cái hiển thị thay đổi nhưng Id của option thì không.
    /// </summary>
    public class AnswerOption : Entity<int>
    {
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }

        /// <summary>Nhãn gốc trong file Word: A, B, C, D</summary>
        [Required]
        [StringLength(1)]
        public string Label { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsCorrect { get; set; }

        public int OrderIndex { get; set; }
    }
}

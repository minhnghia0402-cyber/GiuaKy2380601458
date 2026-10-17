using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace SV2380601458.Exams.Dto
{
    // ===================== Danh sách đề cho sinh viên =====================

    public class StudentExamListDto : EntityDto<int>
    {
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime? PublishedTime { get; set; }

        /// <summary>Số lượt mình đã nộp bài</summary>
        public int MyAttemptCount { get; set; }

        /// <summary>Số câu đúng cao nhất của mình (null nếu chưa thi)</summary>
        public int? MyBestCorrect { get; set; }

        /// <summary>Lượt đang làm dở (còn giờ) để app hiển thị nút "Làm tiếp"</summary>
        public long? InProgressAttemptId { get; set; }
    }

    // ===================== Đề thi khi làm bài (KHÔNG có đáp án, gợi ý) =====================

    public class StartAttemptOutput
    {
        public long AttemptId { get; set; }
        public int ExamId { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime StartTime { get; set; }

        /// <summary>Hạn nộp do server quyết định</summary>
        public DateTime EndTime { get; set; }

        public DateTime ServerTime { get; set; }

        /// <summary>Số giây còn lại, app dùng giá trị này để đếm ngược</summary>
        public int RemainingSeconds { get; set; }

        public bool IsResumed { get; set; }
        public List<TakePartDto> Parts { get; set; } = new List<TakePartDto>();

        /// <summary>Các đáp án đã lưu trước đó (khi làm tiếp)</summary>
        public List<SavedAnswerDto> SavedAnswers { get; set; } = new List<SavedAnswerDto>();
    }

    public class TakePartDto
    {
        public int PartNumber { get; set; }
        public string Directions { get; set; }
        public List<TakeGroupDto> Groups { get; set; } = new List<TakeGroupDto>();
    }

    /// <summary>Part 5: một group không có bài đọc chứa toàn bộ câu. Part 6, 7: mỗi group một bài đọc.</summary>
    public class TakeGroupDto
    {
        public int? GroupId { get; set; }
        public int FromNumber { get; set; }
        public int ToNumber { get; set; }

        /// <summary>HTML bài đọc (chữ, ảnh base64, chỗ trống đã đánh lại số)</summary>
        public List<string> Passages { get; set; } = new List<string>();

        public List<TakeQuestionDto> Questions { get; set; } = new List<TakeQuestionDto>();
    }

    public class TakeQuestionDto
    {
        public int QuestionId { get; set; }

        /// <summary>Số câu hiển thị (đánh lại theo thứ tự sau khi hoán vị)</summary>
        public int DisplayNumber { get; set; }

        public int PartNumber { get; set; }
        public string Content { get; set; }
        public List<TakeOptionDto> Options { get; set; } = new List<TakeOptionDto>();
    }

    public class TakeOptionDto
    {
        public int OptionId { get; set; }

        /// <summary>Nhãn hiển thị A-D theo thứ tự sau khi hoán vị</summary>
        public string Label { get; set; }

        public string Content { get; set; }
    }

    public class SavedAnswerDto
    {
        public int QuestionId { get; set; }
        public int? OptionId { get; set; }
    }

    // ===================== Lưu đáp án =====================

    public class SaveAnswerInput
    {
        public long AttemptId { get; set; }
        public int QuestionId { get; set; }

        /// <summary>null = huỷ chọn</summary>
        public int? OptionId { get; set; }
    }

    public class SaveAnswerOutput
    {
        public bool Saved { get; set; }
        public int AnsweredCount { get; set; }
        public int RemainingSeconds { get; set; }
    }

    // ===================== Kết quả =====================

    public class AttemptResultDto
    {
        public long AttemptId { get; set; }
        public int ExamId { get; set; }
        public string Title { get; set; }
        public AttemptStatus Status { get; set; }
        public bool AutoSubmitted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public int DurationUsedSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int SkippedCount { get; set; }
        public double ScorePercent { get; set; }
        public List<PartResultDto> Parts { get; set; } = new List<PartResultDto>();
        public List<ReviewQuestionDto> Review { get; set; } = new List<ReviewQuestionDto>();
    }

    public class PartResultDto
    {
        public int PartNumber { get; set; }
        public int Total { get; set; }
        public int Correct { get; set; }
        public int Wrong { get; set; }
        public int Skipped { get; set; }
    }

    public class ReviewQuestionDto
    {
        public int QuestionId { get; set; }
        public int DisplayNumber { get; set; }
        public int PartNumber { get; set; }
        public string Content { get; set; }
        public string SelectedLabel { get; set; }
        public string CorrectLabel { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSkipped { get; set; }
        public string Hint { get; set; }
        public List<ReviewOptionDto> Options { get; set; } = new List<ReviewOptionDto>();
    }

    public class ReviewOptionDto
    {
        public string Label { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
    }

    // ===================== Lịch sử thi =====================

    public class MyAttemptDto
    {
        public long AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public AttemptStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
    }
}

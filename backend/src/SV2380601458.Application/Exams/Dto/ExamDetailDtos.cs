using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SV2380601458.Exams.Dto
{
    // ====== Cây nội dung đề (dùng cho Xem trước, Sửa, và sau này cho API mobile) ======

    public class ExamDetailDto : EntityDto<int>
    {
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public ExamStatus Status { get; set; }
        public string SourceFileName { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? PublishedTime { get; set; }
        public int AttemptCount { get; set; }
        public List<ExamPartDto> Parts { get; set; } = new List<ExamPartDto>();
    }

    public class ExamPartDto : EntityDto<int>
    {
        public int PartNumber { get; set; }
        public string Directions { get; set; }
        public bool AllowShuffle { get; set; }
        public List<QuestionGroupDto> Groups { get; set; } = new List<QuestionGroupDto>();

        /// <summary>Câu hỏi độc lập (Part 5), không thuộc group nào</summary>
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }

    public class QuestionGroupDto : EntityDto<int>
    {
        public int FromNumber { get; set; }
        public int ToNumber { get; set; }
        public bool AllowShuffleQuestions { get; set; }
        public List<PassageDto> Passages { get; set; } = new List<PassageDto>();
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }

    public class PassageDto : EntityDto<int>
    {
        public int OrderIndex { get; set; }
        public string Content { get; set; }
    }

    public class QuestionDto : EntityDto<int>
    {
        public int QuestionNumber { get; set; }
        public int PartNumber { get; set; }
        public string Content { get; set; }
        public string Hint { get; set; }
        public bool AllowShuffleAnswers { get; set; }
        public List<AnswerOptionDto> Options { get; set; } = new List<AnswerOptionDto>();
    }

    public class AnswerOptionDto : EntityDto<int>
    {
        public string Label { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }

    // ====== Chỉnh sửa ======

    public class UpdateExamInfoInput : EntityDto<int>
    {
        [Required(ErrorMessage = "Tên đề không được để trống")]
        [StringLength(Exam.MaxTitleLength)]
        public string Title { get; set; }

        [Range(1, 300, ErrorMessage = "Thời gian phải từ 1 đến 300 phút")]
        public int DurationMinutes { get; set; }
    }

    public class QuestionEditDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public ExamStatus ExamStatus { get; set; }
        public QuestionDto Question { get; set; }
    }

    public class UpdateQuestionInput : EntityDto<int>
    {
        public string Content { get; set; }
        public string Hint { get; set; }
        public bool AllowShuffleAnswers { get; set; }
        public int CorrectOptionId { get; set; }
        public List<UpdateOptionInput> Options { get; set; } = new List<UpdateOptionInput>();
    }

    public class UpdateOptionInput
    {
        public int Id { get; set; }
        public string Content { get; set; }
    }

    public class PassageEditDto
    {
        public int PassageId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public ExamStatus ExamStatus { get; set; }
        public int PartNumber { get; set; }
        public string GroupLabel { get; set; }

        /// <summary>Phần chữ của bài đọc, mỗi đoạn một dòng, chỗ trống ghi [BLANK n]</summary>
        public string Text { get; set; }

        /// <summary>Ảnh / bảng hiện có (HTML), giữ nguyên khi chỉ sửa chữ</summary>
        public List<string> PreservedBlocks { get; set; } = new List<string>();
    }

    public class UpdatePassageInput
    {
        public int PassageId { get; set; }
        public string Text { get; set; }
        public string NewImageBase64 { get; set; }
        public string NewImageContentType { get; set; }
        public bool RemoveImages { get; set; }
    }

    // ====== Thống kê ======

    public class ExamStatisticsDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; }
        public ExamStatus Status { get; set; }
        public int TotalQuestions { get; set; }
        public int AttemptCount { get; set; }
        public double AverageCorrect { get; set; }
        public double AveragePercent { get; set; }
        public int HighestCorrect { get; set; }
        public int LowestCorrect { get; set; }
        public double AverageSkipped { get; set; }
        public List<PartStatDto> PartStats { get; set; } = new List<PartStatDto>();
        public List<QuestionStatDto> TopWrong { get; set; } = new List<QuestionStatDto>();
    }

    public class PartStatDto
    {
        public int PartNumber { get; set; }
        public int QuestionCount { get; set; }
        public double CorrectRate { get; set; }
    }

    public class QuestionStatDto
    {
        public int QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public int PartNumber { get; set; }
        public string ContentSnippet { get; set; }
        public string CorrectAnswer { get; set; }
        public int WrongCount { get; set; }
        public int SkippedCount { get; set; }
        public int TotalAttempts { get; set; }
        public double WrongRate { get; set; }
    }
}

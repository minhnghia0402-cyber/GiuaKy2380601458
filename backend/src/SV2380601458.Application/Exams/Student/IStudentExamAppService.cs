using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SV2380601458.Exams.Dto;

namespace SV2380601458.Exams.Student
{
    /// <summary>
    /// API cho app mobile của sinh viên. ABP tự sinh các endpoint:
    ///   GET  /api/services/app/StudentExam/GetPublishedExams
    ///   POST /api/services/app/StudentExam/StartAttempt      { "id": examId }
    ///   POST /api/services/app/StudentExam/SaveAnswer        { "attemptId", "questionId", "optionId" }
    ///   POST /api/services/app/StudentExam/Submit            { "id": attemptId }
    ///   GET  /api/services/app/StudentExam/GetResult?Id=...
    ///   GET  /api/services/app/StudentExam/GetMyAttempts
    /// </summary>
    public interface IStudentExamAppService : IApplicationService
    {
        Task<List<StudentExamListDto>> GetPublishedExamsAsync();
        Task<StartAttemptOutput> StartAttemptAsync(EntityDto<int> input);
        Task<SaveAnswerOutput> SaveAnswerAsync(SaveAnswerInput input);
        Task<AttemptResultDto> SubmitAsync(EntityDto<long> input);
        Task<AttemptResultDto> GetResultAsync(EntityDto<long> input);
        Task<List<MyAttemptDto>> GetMyAttemptsAsync();
    }
}

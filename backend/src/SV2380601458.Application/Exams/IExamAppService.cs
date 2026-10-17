using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SV2380601458.Exams.Dto;

namespace SV2380601458.Exams
{
    public interface IExamAppService : IApplicationService
    {
        // ----- Bước 5: Import & danh sách -----
        Task<ImportExamResultDto> ImportAsync(ImportExamInput input);
        Task<List<ExamListDto>> GetAllAsync();
        Task DeleteAsync(EntityDto<int> input);

        // ----- Bước 6: Xem trước & chỉnh sửa -----
        Task<ExamDetailDto> GetDetailAsync(EntityDto<int> input);
        Task UpdateInfoAsync(UpdateExamInfoInput input);
        Task<QuestionEditDto> GetQuestionForEditAsync(EntityDto<int> input);
        Task UpdateQuestionAsync(UpdateQuestionInput input);
        Task<PassageEditDto> GetPassageForEditAsync(EntityDto<int> input);
        Task UpdatePassageAsync(UpdatePassageInput input);

        // ----- Bước 6: Xuất bản -----
        Task PublishAsync(EntityDto<int> input);
        Task CloseAsync(EntityDto<int> input);

        // ----- Bước 6: Thống kê -----
        Task<ExamStatisticsDto> GetStatisticsAsync(EntityDto<int> input);
        Task<int> SeedDemoAttemptsAsync(EntityDto<int> input);
        Task ClearAttemptsAsync(EntityDto<int> input);
    }
}

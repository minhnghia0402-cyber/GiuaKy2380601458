using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SV2380601458.Exams.Dto;

namespace SV2380601458.Exams
{
    public interface IExamAppService : IApplicationService
    {
        /// <summary>Đọc file Word, kiểm tra lỗi; chỉ lưu khi file đúng 100%</summary>
        Task<ImportExamResultDto> ImportAsync(ImportExamInput input);

        Task<List<ExamListDto>> GetAllAsync();

        Task DeleteAsync(EntityDto<int> input);
    }
}

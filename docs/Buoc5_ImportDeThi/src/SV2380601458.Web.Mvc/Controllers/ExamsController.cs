using System;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV2380601458.Controllers;
using SV2380601458.Exams;
using SV2380601458.Exams.Dto;
using SV2380601458.Web.Models.Exams;

namespace SV2380601458.Web.Controllers
{
    [AbpMvcAuthorize]
    public class ExamsController : SV2380601458ControllerBase
    {
        private const long MaxFileSize = 30 * 1024 * 1024; // 30 MB
        private readonly IExamAppService _examAppService;

        public ExamsController(IExamAppService examAppService)
        {
            _examAppService = examAppService;
        }

        // GET /Exams : danh sách đề thi
        public async Task<ActionResult> Index()
        {
            var exams = await _examAppService.GetAllAsync();
            return View(exams);
        }

        // GET /Exams/Import : form upload
        [HttpGet]
        public ActionResult Import()
        {
            return View(new ImportExamViewModel());
        }

        // POST /Exams/Import : nhận file, kiểm tra, lưu
        [HttpPost]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<ActionResult> Import(IFormFile file)
        {
            var vm = new ImportExamViewModel { FileName = file?.FileName };

            if (file == null || file.Length == 0)
            {
                vm.Errors.Add("Vui lòng chọn file .docx để upload.");
                return View(vm);
            }
            if (!string.Equals(Path.GetExtension(file.FileName), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                vm.Errors.Add("Chỉ chấp nhận file Word định dạng .docx.");
                return View(vm);
            }

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                content = ms.ToArray();
            }

            var result = await _examAppService.ImportAsync(new ImportExamInput
            {
                FileName = Path.GetFileName(file.FileName),
                Content = content
            });

            if (!result.Success)
            {
                vm.Errors = result.Errors;
                return View(vm);
            }

            TempData["Success"] = $"Import thành công đề \"{result.Title}\" gồm {result.QuestionCount} câu"
                                  + (result.TestMode ? " (chế độ TEST)." : ".");
            return RedirectToAction(nameof(Index));
        }

        // POST /Exams/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _examAppService.DeleteAsync(new EntityDto<int>(id));
                TempData["Success"] = "Đã xoá đề thi.";
            }
            catch (UserFriendlyException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

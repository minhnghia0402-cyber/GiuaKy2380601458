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
    [AbpMvcAuthorize("Pages.Exams")]
    public class ExamsController : SV2380601458ControllerBase
    {
        private const long MaxFileSize = 30 * 1024 * 1024;   // 30 MB cho file Word
        private const long MaxImageSize = 5 * 1024 * 1024;   // 5 MB cho áº£nh bÃ i Ä‘á»c
        private readonly IExamAppService _examAppService;

        public ExamsController(IExamAppService examAppService)
        {
            _examAppService = examAppService;
        }

        // ================= DANH SÃCH & IMPORT (BÆ°á»›c 5) =================

        public async Task<ActionResult> Index()
        {
            return View(await _examAppService.GetAllAsync());
        }

        [HttpGet]
        public ActionResult Import()
        {
            return View(new ImportExamViewModel());
        }

        [HttpPost]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<ActionResult> Import(IFormFile file)
        {
            var vm = new ImportExamViewModel { FileName = file?.FileName };
            if (file == null || file.Length == 0)
            {
                vm.Errors.Add("Vui lÃ²ng chá»n file .docx Ä‘á»ƒ upload.");
                return View(vm);
            }
            if (!string.Equals(Path.GetExtension(file.FileName), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                vm.Errors.Add("Chá»‰ cháº¥p nháº­n file Word Ä‘á»‹nh dáº¡ng .docx.");
                return View(vm);
            }

            var result = await _examAppService.ImportAsync(new ImportExamInput
            {
                FileName = Path.GetFileName(file.FileName),
                Content = await ReadAllBytes(file)
            });

            if (!result.Success)
            {
                vm.Errors = result.Errors;
                return View(vm);
            }

            TempData["Success"] = $"Import thÃ nh cÃ´ng Ä‘á» \"{result.Title}\" gá»“m {result.QuestionCount} cÃ¢u"
                                  + (result.TestMode ? " (cháº¿ Ä‘á»™ TEST)." : ".");
            return RedirectToAction(nameof(Detail), new { id = result.ExamId });
        }

        [HttpPost]
        public Task<ActionResult> Delete(int id) =>
            Run(() => _examAppService.DeleteAsync(new EntityDto<int>(id)), "ÄÃ£ xoÃ¡ Ä‘á» thi.", RedirectToAction(nameof(Index)));

        // ================= XEM TRÆ¯á»šC & CHá»ˆNH Sá»¬A (BÆ°á»›c 6) =================

        public async Task<ActionResult> Detail(int id)
        {
            return View(await _examAppService.GetDetailAsync(new EntityDto<int>(id)));
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            var exam = await _examAppService.GetDetailAsync(new EntityDto<int>(id));
            return View(new UpdateExamInfoInput { Id = exam.Id, Title = exam.Title, DurationMinutes = exam.DurationMinutes });
        }

        [HttpPost]
        public Task<ActionResult> Edit(UpdateExamInfoInput input) =>
            Run(() => _examAppService.UpdateInfoAsync(input), "ÄÃ£ cáº­p nháº­t thÃ´ng tin Ä‘á».",
                RedirectToAction(nameof(Detail), new { id = input.Id }));

        [HttpGet]
        public async Task<ActionResult> EditQuestion(int id)
        {
            return View(await _examAppService.GetQuestionForEditAsync(new EntityDto<int>(id)));
        }

        [HttpPost]
        public async Task<ActionResult> EditQuestion(UpdateQuestionInput input)
        {
            try
            {
                await _examAppService.UpdateQuestionAsync(input);
                var q = await _examAppService.GetQuestionForEditAsync(new EntityDto<int>(input.Id));
                TempData["Success"] = $"ÄÃ£ lÆ°u cÃ¢u {q.Question.QuestionNumber}.";
                return Redirect(Url.Action(nameof(Detail), new { id = q.ExamId }) + "#q" + q.Question.QuestionNumber);
            }
            catch (UserFriendlyException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(EditQuestion), new { id = input.Id });
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditPassage(int id)
        {
            return View(await _examAppService.GetPassageForEditAsync(new EntityDto<int>(id)));
        }

        [HttpPost]
        [RequestSizeLimit(MaxImageSize + 1024 * 1024)]
        public async Task<ActionResult> EditPassage(int passageId, string text, IFormFile image, bool removeImages = false)
        {
            try
            {
                var input = new UpdatePassageInput { PassageId = passageId, Text = text, RemoveImages = removeImages };
                if (image != null && image.Length > 0)
                {
                    if (!image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        throw new UserFriendlyException("File táº£i lÃªn pháº£i lÃ  áº£nh (jpg, png...).");
                    if (image.Length > MaxImageSize)
                        throw new UserFriendlyException("áº¢nh tá»‘i Ä‘a 5 MB.");
                    input.NewImageBase64 = Convert.ToBase64String(await ReadAllBytes(image));
                    input.NewImageContentType = image.ContentType;
                }

                await _examAppService.UpdatePassageAsync(input);
                var p = await _examAppService.GetPassageForEditAsync(new EntityDto<int>(passageId));
                TempData["Success"] = $"ÄÃ£ lÆ°u bÃ i Ä‘á»c ({p.GroupLabel}).";
                return Redirect(Url.Action(nameof(Detail), new { id = p.ExamId }) + "#p" + passageId);
            }
            catch (UserFriendlyException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(EditPassage), new { id = passageId });
            }
        }

        // ================= XUáº¤T Báº¢N (BÆ°á»›c 6) =================

        [HttpPost]
        public Task<ActionResult> Publish(int id) =>
            Run(() => _examAppService.PublishAsync(new EntityDto<int>(id)),
                "ÄÃ£ xuáº¥t báº£n Ä‘á». Sinh viÃªn cÃ³ thá»ƒ báº¯t Ä‘áº§u thi.", RedirectToAction(nameof(Detail), new { id }));

        [HttpPost]
        public Task<ActionResult> Close(int id) =>
            Run(() => _examAppService.CloseAsync(new EntityDto<int>(id)),
                "ÄÃ£ Ä‘Ã³ng Ä‘á». Sinh viÃªn khÃ´ng thá»ƒ báº¯t Ä‘áº§u lÆ°á»£t thi má»›i.", RedirectToAction(nameof(Detail), new { id }));

        // ================= THá»NG KÃŠ (BÆ°á»›c 6) =================

        public async Task<ActionResult> Statistics(int id)
        {
            return View(await _examAppService.GetStatisticsAsync(new EntityDto<int>(id)));
        }

        [HttpPost]
        public Task<ActionResult> SeedDemo(int id) =>
            Run(() => _examAppService.SeedDemoAttemptsAsync(new EntityDto<int>(id)),
                "ÄÃ£ táº¡o 20 lÆ°á»£t thi giáº£ láº­p Ä‘á»ƒ kiá»ƒm tra thá»‘ng kÃª.", RedirectToAction(nameof(Statistics), new { id }));

        [HttpPost]
        public Task<ActionResult> ClearAttempts(int id) =>
            Run(() => _examAppService.ClearAttemptsAsync(new EntityDto<int>(id)),
                "ÄÃ£ xoÃ¡ toÃ n bá»™ lÆ°á»£t thi cá»§a Ä‘á».", RedirectToAction(nameof(Statistics), new { id }));

        // ================= TIá»†N ÃCH =================

        private async Task<ActionResult> Run(Func<Task> action, string successMessage, ActionResult next)
        {
            try
            {
                await action();
                TempData["Success"] = successMessage;
            }
            catch (UserFriendlyException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return next;
        }

        private static async Task<byte[]> ReadAllBytes(IFormFile file)
        {
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}


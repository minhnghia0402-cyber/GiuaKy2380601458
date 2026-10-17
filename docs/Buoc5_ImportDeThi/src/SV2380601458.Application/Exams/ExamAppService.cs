using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using SV2380601458.Exams.Dto;
using SV2380601458.Exams.Import;

namespace SV2380601458.Exams
{
    [AbpAuthorize]
    public class ExamAppService : SV2380601458AppServiceBase, IExamAppService
    {
        private readonly IRepository<Exam, int> _examRepository;
        private readonly IRepository<ExamAttempt, long> _attemptRepository;

        public ExamAppService(
            IRepository<Exam, int> examRepository,
            IRepository<ExamAttempt, long> attemptRepository)
        {
            _examRepository = examRepository;
            _attemptRepository = attemptRepository;
        }

        // DisableAuditing: không ghi nội dung file (vài MB) vào bảng log của ABP
        [DisableAuditing]
        public async Task<ImportExamResultDto> ImportAsync(ImportExamInput input)
        {
            var result = new ImportExamResultDto();

            // 1. Đọc file theo tag
            var parsed = new ExamDocxParser().Parse(input.Content);
            result.Errors.AddRange(parsed.Errors);

            // 2. Kiểm tra nội dung (chạy cả khi đã có lỗi cú pháp để báo đủ lỗi một lần)
            result.Errors.AddRange(new ExamValidator().Validate(parsed.Exam));

            // 3. Có bất kỳ lỗi nào -> KHÔNG lưu gì vào CSDL
            if (result.Errors.Count > 0)
            {
                result.Success = false;
                return result;
            }

            // 4. File đúng 100% -> dựng cây entity và lưu một lần (trong 1 transaction của UnitOfWork)
            var exam = BuildExam(parsed.Exam, input.FileName);
            await _examRepository.InsertAsync(exam);
            await CurrentUnitOfWork.SaveChangesAsync();

            result.Success = true;
            result.ExamId = exam.Id;
            result.Title = exam.Title;
            result.QuestionCount = exam.TotalQuestions;
            result.TestMode = parsed.Exam.TestMode;
            return result;
        }

        public Task<List<ExamListDto>> GetAllAsync()
        {
            var attemptCounts = _attemptRepository.GetAll()
                .GroupBy(a => a.ExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.ExamId, x => x.Count);

            var list = _examRepository.GetAll()
                .OrderByDescending(e => e.CreationTime)
                .Select(e => new ExamListDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    DurationMinutes = e.DurationMinutes,
                    TotalQuestions = e.TotalQuestions,
                    Status = e.Status,
                    SourceFileName = e.SourceFileName,
                    CreationTime = e.CreationTime,
                    PublishedTime = e.PublishedTime
                })
                .ToList();

            foreach (var item in list)
                item.AttemptCount = attemptCounts.TryGetValue(item.Id, out var c) ? c : 0;

            return Task.FromResult(list);
        }

        public async Task DeleteAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            if (exam.Status == ExamStatus.Published)
                throw new UserFriendlyException("Đề đang được xuất bản, hãy đóng đề trước khi xoá.");
            await _examRepository.DeleteAsync(exam); // xoá mềm (FullAuditedEntity)
        }

        // Chuyển mô hình đọc từ Word sang Entity
        private static Exam BuildExam(ParsedExam p, string fileName)
        {
            var exam = new Exam
            {
                Title = p.Title.Trim(),
                DurationMinutes = p.DurationMinutes ?? 75,
                Status = ExamStatus.Draft,
                SourceFileName = fileName,
                TotalQuestions = p.Parts.Sum(x => x.Questions.Count)
            };

            int partIndex = 0;
            foreach (var pp in p.Parts)
            {
                var part = new ExamPart
                {
                    PartNumber = pp.PartNumber,
                    Directions = pp.Directions,
                    AllowShuffle = pp.AllowShuffle,
                    OrderIndex = partIndex++
                };
                exam.Parts.Add(part);

                // Tạo group trước để gắn câu hỏi vào đúng group
                var groupMap = new Dictionary<ParsedGroup, QuestionGroup>();
                int groupIndex = 0;
                foreach (var pg in pp.Groups)
                {
                    var group = new QuestionGroup
                    {
                        FromNumber = pg.FromNumber,
                        ToNumber = pg.ToNumber,
                        AllowShuffleQuestions = pg.AllowShuffleQuestions,
                        OrderIndex = groupIndex++
                    };
                    int passageIndex = 0;
                    foreach (var ps in pg.Passages)
                        group.Passages.Add(new Passage { OrderIndex = passageIndex++, Content = ps.Html.ToString() });

                    part.Groups.Add(group);
                    groupMap[pg] = group;
                }

                int questionIndex = 0;
                foreach (var pq in pp.Questions)
                {
                    var question = new Question
                    {
                        QuestionNumber = pq.Number,
                        Content = pq.Content.Length > 0 ? pq.Content.ToString() : null,
                        Hint = pq.Hint.ToString().Trim(),
                        AllowShuffleAnswers = pq.AllowShuffleAnswers,
                        OrderIndex = questionIndex++
                    };
                    if (pq.Group != null) question.QuestionGroup = groupMap[pq.Group];

                    int optionIndex = 0;
                    foreach (var po in pq.Options)
                    {
                        question.Options.Add(new AnswerOption
                        {
                            Label = po.Label,
                            Content = po.Content,
                            IsCorrect = po.Label == pq.Answer,
                            OrderIndex = optionIndex++
                        });
                    }
                    part.Questions.Add(question);
                }
            }
            return exam;
        }
    }
}

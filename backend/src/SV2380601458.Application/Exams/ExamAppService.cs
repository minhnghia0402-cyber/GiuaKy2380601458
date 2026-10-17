using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using SV2380601458.Exams.Dto;
using SV2380601458.Exams.Import;

namespace SV2380601458.Exams
{
    [AbpAuthorize("Pages.Exams")]
    public class ExamAppService : SV2380601458AppServiceBase, IExamAppService
    {
        private readonly IRepository<Exam, int> _examRepository;
        private readonly IRepository<ExamPart, int> _partRepository;
        private readonly IRepository<QuestionGroup, int> _groupRepository;
        private readonly IRepository<Passage, int> _passageRepository;
        private readonly IRepository<Question, int> _questionRepository;
        private readonly IRepository<AnswerOption, int> _optionRepository;
        private readonly IRepository<ExamAttempt, long> _attemptRepository;
        private readonly IRepository<AttemptAnswer, long> _attemptAnswerRepository;

        public ExamAppService(
            IRepository<Exam, int> examRepository,
            IRepository<ExamPart, int> partRepository,
            IRepository<QuestionGroup, int> groupRepository,
            IRepository<Passage, int> passageRepository,
            IRepository<Question, int> questionRepository,
            IRepository<AnswerOption, int> optionRepository,
            IRepository<ExamAttempt, long> attemptRepository,
            IRepository<AttemptAnswer, long> attemptAnswerRepository)
        {
            _examRepository = examRepository;
            _partRepository = partRepository;
            _groupRepository = groupRepository;
            _passageRepository = passageRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _attemptRepository = attemptRepository;
            _attemptAnswerRepository = attemptAnswerRepository;
        }

        // =====================================================================
        // BÆ¯á»šC 5: IMPORT & DANH SÃCH
        // =====================================================================

        [DisableAuditing]
        public async Task<ImportExamResultDto> ImportAsync(ImportExamInput input)
        {
            var result = new ImportExamResultDto();

            var parsed = new ExamDocxParser().Parse(input.Content);
            result.Errors.AddRange(parsed.Errors);
            result.Errors.AddRange(new ExamValidator().Validate(parsed.Exam));

            if (result.Errors.Count > 0)
            {
                result.Success = false;
                return result;
            }

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
            var attemptCounts = CountSubmittedAttempts();

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
                throw new UserFriendlyException("Äá» Ä‘ang má»Ÿ thi, hÃ£y Ä‘Ã³ng Ä‘á» trÆ°á»›c khi xoÃ¡.");
            await _examRepository.DeleteAsync(exam); // xoÃ¡ má»m (FullAuditedEntity)
        }

        // =====================================================================
        // BÆ¯á»šC 6: XEM TRÆ¯á»šC & CHá»ˆNH Sá»¬A
        // =====================================================================

        public async Task<ExamDetailDto> GetDetailAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            var dto = LoadExamTree(exam);
            dto.AttemptCount = CountSubmittedAttempts().TryGetValue(exam.Id, out var c) ? c : 0;
            return dto;
        }

        public async Task UpdateInfoAsync(UpdateExamInfoInput input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            EnsureEditable(exam);
            exam.Title = input.Title.Trim();
            exam.DurationMinutes = input.DurationMinutes;
        }

        public async Task<QuestionEditDto> GetQuestionForEditAsync(EntityDto<int> input)
        {
            var question = await _questionRepository.GetAsync(input.Id);
            var part = await _partRepository.GetAsync(question.ExamPartId);
            var exam = await _examRepository.GetAsync(part.ExamId);
            var options = _optionRepository.GetAll()
                .Where(o => o.QuestionId == question.Id)
                .OrderBy(o => o.OrderIndex)
                .ToList();

            return new QuestionEditDto
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                ExamStatus = exam.Status,
                Question = MapQuestion(question, part.PartNumber, options)
            };
        }

        public async Task UpdateQuestionAsync(UpdateQuestionInput input)
        {
            var question = await _questionRepository.GetAsync(input.Id);
            var part = await _partRepository.GetAsync(question.ExamPartId);
            var exam = await _examRepository.GetAsync(part.ExamId);
            EnsureEditable(exam);

            if (part.PartNumber != 6 && string.IsNullOrWhiteSpace(input.Content))
                throw new UserFriendlyException("Ná»™i dung cÃ¢u há»i khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
            if (string.IsNullOrWhiteSpace(input.Hint))
                throw new UserFriendlyException("Gá»£i Ã½ (hint) khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");

            var options = _optionRepository.GetAll().Where(o => o.QuestionId == question.Id).ToList();
            if (input.Options == null || input.Options.Count != options.Count)
                throw new UserFriendlyException("Dá»¯ liá»‡u Ä‘Ã¡p Ã¡n khÃ´ng há»£p lá»‡.");
            if (options.All(o => o.Id != input.CorrectOptionId))
                throw new UserFriendlyException("HÃ£y chá»n má»™t Ä‘Ã¡p Ã¡n Ä‘Ãºng.");

            foreach (var option in options)
            {
                var edited = input.Options.FirstOrDefault(x => x.Id == option.Id);
                if (edited == null || string.IsNullOrWhiteSpace(edited.Content))
                    throw new UserFriendlyException($"ÄÃ¡p Ã¡n ({option.Label}) khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
                option.Content = edited.Content.Trim();
                option.IsCorrect = option.Id == input.CorrectOptionId;
            }

            question.Content = string.IsNullOrWhiteSpace(input.Content) ? null : input.Content.Trim();
            question.Hint = input.Hint.Trim();
            question.AllowShuffleAnswers = input.AllowShuffleAnswers;
        }

        public async Task<PassageEditDto> GetPassageForEditAsync(EntityDto<int> input)
        {
            var passage = await _passageRepository.GetAsync(input.Id);
            var group = await _groupRepository.GetAsync(passage.QuestionGroupId);
            var part = await _partRepository.GetAsync(group.ExamPartId);
            var exam = await _examRepository.GetAsync(part.ExamId);

            return new PassageEditDto
            {
                PassageId = passage.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                ExamStatus = exam.Status,
                PartNumber = part.PartNumber,
                GroupLabel = $"CÃ¢u {group.FromNumber}-{group.ToNumber}",
                Text = PassageHtmlHelper.ToEditableText(passage.Content),
                PreservedBlocks = PassageHtmlHelper.GetPreservedBlocks(passage.Content)
            };
        }

        [DisableAuditing]
        public async Task UpdatePassageAsync(UpdatePassageInput input)
        {
            var passage = await _passageRepository.GetAsync(input.PassageId);
            var group = await _groupRepository.GetAsync(passage.QuestionGroupId);
            var part = await _partRepository.GetAsync(group.ExamPartId);
            var exam = await _examRepository.GetAsync(part.ExamId);
            EnsureEditable(exam);

            var textHtml = PassageHtmlHelper.TextToHtml(input.Text, out var blanks);

            // Part 6: chá»— trá»‘ng trong bÃ i Ä‘á»c pháº£i khá»›p Ä‘Ãºng cÃ¡c cÃ¢u cá»§a nhÃ³m
            if (part.PartNumber == 6)
            {
                var groupPassageBlanks = new List<int>(blanks);
                foreach (var other in _passageRepository.GetAll()
                             .Where(p => p.QuestionGroupId == group.Id && p.Id != passage.Id).ToList())
                {
                    PassageHtmlHelper.TextToHtml(PassageHtmlHelper.ToEditableText(other.Content), out var otherBlanks);
                    groupPassageBlanks.AddRange(otherBlanks);
                }

                var numbers = _questionRepository.GetAll()
                    .Where(q => q.QuestionGroupId == group.Id)
                    .Select(q => q.QuestionNumber).ToList();
                var missing = numbers.Where(n => !groupPassageBlanks.Contains(n)).ToList();
                var extra = groupPassageBlanks.Distinct().Where(n => !numbers.Contains(n)).ToList();
                if (missing.Count > 0 || extra.Count > 0)
                {
                    var msg = new List<string>();
                    if (missing.Count > 0) msg.Add("thiáº¿u " + string.Join(", ", missing.Select(n => $"[BLANK {n}]")));
                    if (extra.Count > 0) msg.Add("thá»«a " + string.Join(", ", extra.Select(n => $"[BLANK {n}]")));
                    throw new UserFriendlyException("Chá»— trá»‘ng khÃ´ng khá»›p vá»›i cÃ¢u há»i cá»§a nhÃ³m: " + string.Join("; ", msg) + ".");
                }
            }

            List<string> blocks;
            if (!string.IsNullOrEmpty(input.NewImageBase64))
                blocks = new List<string> { PassageHtmlHelper.ImageBlock(input.NewImageContentType, input.NewImageBase64) };
            else if (input.RemoveImages)
                blocks = new List<string>();
            else
                blocks = PassageHtmlHelper.GetPreservedBlocks(passage.Content);

            var newContent = textHtml + string.Concat(blocks);
            if (string.IsNullOrWhiteSpace(newContent))
                throw new UserFriendlyException("BÃ i Ä‘á»c khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng (cáº§n cÃ³ chá»¯ hoáº·c áº£nh).");

            passage.Content = newContent;
        }

        // =====================================================================
        // BÆ¯á»šC 6: XUáº¤T Báº¢N
        // =====================================================================

        public async Task PublishAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            if (exam.Status == ExamStatus.Published)
                throw new UserFriendlyException("Äá» nÃ y Ä‘ang Ä‘Æ°á»£c má»Ÿ thi.");
            if (exam.TotalQuestions == 0)
                throw new UserFriendlyException("Äá» chÆ°a cÃ³ cÃ¢u há»i, khÃ´ng thá»ƒ xuáº¥t báº£n.");

            exam.Status = ExamStatus.Published;
            exam.PublishedTime = Clock.Now;
        }

        public async Task CloseAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            if (exam.Status != ExamStatus.Published)
                throw new UserFriendlyException("Chá»‰ Ä‘Ã³ng Ä‘Æ°á»£c Ä‘á» Ä‘ang má»Ÿ thi.");
            exam.Status = ExamStatus.Closed;
        }

        // =====================================================================
        // BÆ¯á»šC 6: THá»NG KÃŠ
        // =====================================================================

        public async Task<ExamStatisticsDto> GetStatisticsAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            var tree = LoadExamTree(exam);
            var allQuestions = tree.Parts
                .SelectMany(p => p.Questions.Concat(p.Groups.SelectMany(g => g.Questions)))
                .ToList();

            var attempts = _attemptRepository.GetAll()
                .Where(a => a.ExamId == exam.Id && a.Status != AttemptStatus.InProgress)
                .Select(a => new { a.Id, a.CorrectCount, a.SkippedCount })
                .ToList();

            var stats = new ExamStatisticsDto
            {
                ExamId = exam.Id,
                Title = exam.Title,
                Status = exam.Status,
                TotalQuestions = exam.TotalQuestions,
                AttemptCount = attempts.Count
            };
            if (attempts.Count == 0) return stats;

            stats.AverageCorrect = Math.Round(attempts.Average(a => a.CorrectCount), 1);
            stats.AveragePercent = exam.TotalQuestions == 0 ? 0 : Math.Round(stats.AverageCorrect * 100.0 / exam.TotalQuestions, 1);
            stats.HighestCorrect = attempts.Max(a => a.CorrectCount);
            stats.LowestCorrect = attempts.Min(a => a.CorrectCount);
            stats.AverageSkipped = Math.Round(attempts.Average(a => a.SkippedCount), 1);

            // Thá»‘ng kÃª tá»«ng cÃ¢u: sá»‘ láº§n sai (Ä‘Ã£ chá»n nhÆ°ng sai) vÃ  bá» qua
            var answerStats = _attemptAnswerRepository.GetAll()
                .Where(x => x.ExamAttempt.ExamId == exam.Id && x.ExamAttempt.Status != AttemptStatus.InProgress)
                .GroupBy(x => x.QuestionId)
                .Select(g => new
                {
                    QuestionId = g.Key,
                    Correct = g.Count(x => x.IsCorrect),
                    Wrong = g.Count(x => !x.IsCorrect && x.SelectedOptionId != null),
                    Skipped = g.Count(x => x.SelectedOptionId == null)
                })
                .ToList()
                .ToDictionary(x => x.QuestionId);

            foreach (var part in tree.Parts)
            {
                var ids = part.Questions.Concat(part.Groups.SelectMany(g => g.Questions)).Select(q => q.Id).ToList();
                var correct = ids.Sum(id => answerStats.TryGetValue(id, out var s) ? s.Correct : 0);
                stats.PartStats.Add(new PartStatDto
                {
                    PartNumber = part.PartNumber,
                    QuestionCount = ids.Count,
                    CorrectRate = ids.Count == 0 ? 0 : Math.Round(correct * 100.0 / (ids.Count * attempts.Count), 1)
                });
            }

            stats.TopWrong = allQuestions
                .Select(q =>
                {
                    answerStats.TryGetValue(q.Id, out var s);
                    var wrong = s?.Wrong ?? 0;
                    var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);
                    return new QuestionStatDto
                    {
                        QuestionId = q.Id,
                        QuestionNumber = q.QuestionNumber,
                        PartNumber = q.PartNumber,
                        ContentSnippet = Snippet(q.Content ?? $"(Chá»— trá»‘ng sá»‘ {q.QuestionNumber} trong bÃ i Ä‘á»c)"),
                        CorrectAnswer = correctOption == null ? "" : $"({correctOption.Label}) {correctOption.Content}",
                        WrongCount = wrong,
                        SkippedCount = s?.Skipped ?? 0,
                        TotalAttempts = attempts.Count,
                        WrongRate = Math.Round(wrong * 100.0 / attempts.Count, 1)
                    };
                })
                .Where(x => x.WrongCount > 0)
                .OrderByDescending(x => x.WrongCount)
                .ThenBy(x => x.QuestionNumber)
                .Take(10)
                .ToList();

            return stats;
        }

        /// <summary>
        /// Sinh dá»¯ liá»‡u lÆ°á»£t thi giáº£ láº­p Ä‘á»ƒ kiá»ƒm tra trang thá»‘ng kÃª khi app mobile chÆ°a hoÃ n thÃ nh.
        /// Má»—i cÃ¢u Ä‘Æ°á»£c gÃ¡n má»™t "Ä‘á»™ khÃ³" ngáº«u nhiÃªn Ä‘á»ƒ káº¿t quáº£ cÃ³ cÃ¢u dá»…, cÃ¢u khÃ³.
        /// </summary>
        public async Task<int> SeedDemoAttemptsAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            var userId = AbpSession.GetUserId();
            var questions = LoadExamTree(exam).Parts
                .SelectMany(p => p.Questions.Concat(p.Groups.SelectMany(g => g.Questions)))
                .ToList();
            if (questions.Count == 0) throw new UserFriendlyException("Äá» chÆ°a cÃ³ cÃ¢u há»i.");

            var random = new Random();
            var difficulty = questions.ToDictionary(q => q.Id, q => 0.35 + random.NextDouble() * 0.6);
            const int count = 20;

            for (int i = 0; i < count; i++)
            {
                var start = Clock.Now.AddDays(-random.Next(0, 7)).AddMinutes(-random.Next(0, 600));
                var attempt = new ExamAttempt
                {
                    ExamId = exam.Id,
                    UserId = userId,
                    StartTime = start,
                    EndTime = start.AddMinutes(exam.DurationMinutes),
                    SubmitTime = start.AddMinutes(random.Next(1, Math.Max(2, exam.DurationMinutes))),
                    Status = AttemptStatus.Submitted
                };

                foreach (var q in questions)
                {
                    var answer = new AttemptAnswer { QuestionId = q.Id, AnsweredTime = attempt.SubmitTime };
                    var roll = random.NextDouble();
                    if (roll < 0.05)
                    {
                        attempt.SkippedCount++;                       // bá» qua
                    }
                    else if (roll < difficulty[q.Id])
                    {
                        answer.SelectedOptionId = q.Options.First(o => o.IsCorrect).Id;
                        answer.IsCorrect = true;
                        attempt.CorrectCount++;
                    }
                    else
                    {
                        var wrongOptions = q.Options.Where(o => !o.IsCorrect).ToList();
                        answer.SelectedOptionId = wrongOptions[random.Next(wrongOptions.Count)].Id;
                        attempt.WrongCount++;
                    }
                    attempt.Answers.Add(answer);
                }

                await _attemptRepository.InsertAsync(attempt);
            }
            return count;
        }

        public async Task ClearAttemptsAsync(EntityDto<int> input)
        {
            var exam = await _examRepository.GetAsync(input.Id);
            var attemptIds = _attemptRepository.GetAll().Where(a => a.ExamId == exam.Id).Select(a => a.Id).ToList();
            foreach (var id in attemptIds)
                await _attemptAnswerRepository.DeleteAsync(x => x.ExamAttemptId == id);
            await _attemptRepository.DeleteAsync(a => a.ExamId == exam.Id);
        }

        // =====================================================================
        // HÃ€M DÃ™NG CHUNG
        // =====================================================================

        private static void EnsureEditable(Exam exam)
        {
            if (exam.Status == ExamStatus.Published)
                throw new UserFriendlyException("Äá» Ä‘ang má»Ÿ thi nÃªn khÃ´ng thá»ƒ chá»‰nh sá»­a. HÃ£y Ä‘Ã³ng Ä‘á» trÆ°á»›c.");
        }

        private Dictionary<int, int> CountSubmittedAttempts()
        {
            return _attemptRepository.GetAll()
                .Where(a => a.Status != AttemptStatus.InProgress)
                .GroupBy(a => a.ExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.ExamId, x => x.Count);
        }

        /// <summary>Äá»c toÃ n bá»™ cÃ¢y ná»™i dung Ä‘á» báº±ng vÃ i truy váº¥n (má»—i báº£ng má»™t láº§n)</summary>
        private ExamDetailDto LoadExamTree(Exam exam)
        {
            var parts = _partRepository.GetAll().Where(p => p.ExamId == exam.Id).OrderBy(p => p.OrderIndex).ToList();
            var partIds = parts.Select(p => p.Id).ToList();

            var groups = _groupRepository.GetAll().Where(g => partIds.Contains(g.ExamPartId)).OrderBy(g => g.OrderIndex).ToList();
            var groupIds = groups.Select(g => g.Id).ToList();

            var passages = _passageRepository.GetAll().Where(p => groupIds.Contains(p.QuestionGroupId)).OrderBy(p => p.OrderIndex).ToList();
            var questions = _questionRepository.GetAll().Where(q => partIds.Contains(q.ExamPartId)).OrderBy(q => q.OrderIndex).ToList();
            var questionIds = questions.Select(q => q.Id).ToList();
            var options = _optionRepository.GetAll().Where(o => questionIds.Contains(o.QuestionId)).OrderBy(o => o.OrderIndex).ToList();

            var optionsByQuestion = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());
            var dto = new ExamDetailDto
            {
                Id = exam.Id,
                Title = exam.Title,
                DurationMinutes = exam.DurationMinutes,
                TotalQuestions = exam.TotalQuestions,
                Status = exam.Status,
                SourceFileName = exam.SourceFileName,
                CreationTime = exam.CreationTime,
                PublishedTime = exam.PublishedTime
            };

            foreach (var part in parts)
            {
                var partDto = new ExamPartDto
                {
                    Id = part.Id,
                    PartNumber = part.PartNumber,
                    Directions = part.Directions,
                    AllowShuffle = part.AllowShuffle
                };

                QuestionDto Map(Question q) => MapQuestion(q, part.PartNumber,
                    optionsByQuestion.TryGetValue(q.Id, out var list) ? list : new List<AnswerOption>());

                partDto.Questions = questions
                    .Where(q => q.ExamPartId == part.Id && q.QuestionGroupId == null)
                    .Select(Map).ToList();

                partDto.Groups = groups.Where(g => g.ExamPartId == part.Id).Select(g => new QuestionGroupDto
                {
                    Id = g.Id,
                    FromNumber = g.FromNumber,
                    ToNumber = g.ToNumber,
                    AllowShuffleQuestions = g.AllowShuffleQuestions,
                    Passages = passages.Where(p => p.QuestionGroupId == g.Id)
                        .Select(p => new PassageDto { Id = p.Id, OrderIndex = p.OrderIndex, Content = p.Content }).ToList(),
                    Questions = questions.Where(q => q.QuestionGroupId == g.Id).Select(Map).ToList()
                }).ToList();

                dto.Parts.Add(partDto);
            }
            return dto;
        }

        private static QuestionDto MapQuestion(Question q, int partNumber, List<AnswerOption> options)
        {
            return new QuestionDto
            {
                Id = q.Id,
                QuestionNumber = q.QuestionNumber,
                PartNumber = partNumber,
                Content = q.Content,
                Hint = q.Hint,
                AllowShuffleAnswers = q.AllowShuffleAnswers,
                Options = options.OrderBy(o => o.OrderIndex).Select(o => new AnswerOptionDto
                {
                    Id = o.Id,
                    Label = o.Label,
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
        }

        private static string Snippet(string s)
        {
            s = (s ?? "").Replace("\n", " ");
            return s.Length > 90 ? s.Substring(0, 90) + "..." : s;
        }

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


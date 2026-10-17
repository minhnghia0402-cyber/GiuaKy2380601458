using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using SV2380601458.Exams.Dto;

namespace SV2380601458.Exams.Student
{
    [AbpAuthorize] // mọi người dùng đã đăng nhập (sinh viên)
    public class StudentExamAppService : SV2380601458AppServiceBase, IStudentExamAppService
    {
        /// <summary>Cho phép trễ vài giây do độ trễ mạng khi app tự nộp bài lúc hết giờ</summary>
        private const int GraceSeconds = 30;

        private readonly IRepository<Exam, int> _examRepository;
        private readonly IRepository<ExamAttempt, long> _attemptRepository;
        private readonly IRepository<AttemptAnswer, long> _answerRepository;
        private readonly IRepository<AnswerOption, int> _optionRepository;
        private readonly ExamContentLoader _loader;

        public StudentExamAppService(
            IRepository<Exam, int> examRepository,
            IRepository<ExamAttempt, long> attemptRepository,
            IRepository<AttemptAnswer, long> answerRepository,
            IRepository<AnswerOption, int> optionRepository,
            ExamContentLoader loader)
        {
            _examRepository = examRepository;
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
            _optionRepository = optionRepository;
            _loader = loader;
        }

        // ============================ DANH SÁCH ĐỀ ============================

        public Task<List<StudentExamListDto>> GetPublishedExamsAsync()
        {
            var userId = AbpSession.GetUserId();
            var now = Clock.Now;

            var exams = _examRepository.GetAll()
                .Where(e => e.Status == ExamStatus.Published)
                .OrderByDescending(e => e.PublishedTime)
                .ToList();

            var myAttempts = _attemptRepository.GetAll()
                .Where(a => a.UserId == userId)
                .Select(a => new { a.Id, a.ExamId, a.Status, a.CorrectCount, a.EndTime })
                .ToList();

            var result = exams.Select(e =>
            {
                var mine = myAttempts.Where(a => a.ExamId == e.Id).ToList();
                var done = mine.Where(a => a.Status != AttemptStatus.InProgress).ToList();
                var running = mine.Where(a => a.Status == AttemptStatus.InProgress && a.EndTime.AddSeconds(GraceSeconds) > now)
                    .OrderByDescending(a => a.Id).FirstOrDefault();
                return new StudentExamListDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    DurationMinutes = e.DurationMinutes,
                    TotalQuestions = e.TotalQuestions,
                    PublishedTime = e.PublishedTime,
                    MyAttemptCount = done.Count,
                    MyBestCorrect = done.Count == 0 ? (int?)null : done.Max(a => a.CorrectCount),
                    InProgressAttemptId = running?.Id
                };
            }).ToList();

            return Task.FromResult(result);
        }

        // ============================ BẮT ĐẦU / LÀM TIẾP ============================

        public async Task<StartAttemptOutput> StartAttemptAsync(EntityDto<int> input)
        {
            var userId = AbpSession.GetUserId();
            var exam = await _examRepository.GetAsync(input.Id);
            var tree = _loader.Load(exam);

            // Có lượt đang làm dở?
            var existing = _attemptRepository.GetAll()
                .Where(a => a.ExamId == exam.Id && a.UserId == userId && a.Status == AttemptStatus.InProgress)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();

            if (existing != null && IsExpired(existing))
            {
                await FinalizeAsync(existing, autoSubmitted: true);
                existing = null;
            }

            if (existing == null && exam.Status != ExamStatus.Published)
                throw new UserFriendlyException("Đề thi chưa được mở hoặc đã đóng.");

            ExamAttempt attempt;
            AttemptLayout layout;
            var resumed = existing != null;

            if (resumed)
            {
                attempt = existing;
                layout = AttemptLayout.FromJson(attempt.ShuffleMapJson);
            }
            else
            {
                layout = AttemptLayout.Build(tree, new Random());
                var now = Clock.Now;
                attempt = new ExamAttempt
                {
                    ExamId = exam.Id,
                    UserId = userId,
                    StartTime = now,
                    EndTime = now.AddMinutes(exam.DurationMinutes),
                    Status = AttemptStatus.InProgress,
                    ShuffleMapJson = layout.ToJson()
                };
                await _attemptRepository.InsertAsync(attempt);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            var output = new StartAttemptOutput
            {
                AttemptId = attempt.Id,
                ExamId = exam.Id,
                Title = exam.Title,
                DurationMinutes = exam.DurationMinutes,
                TotalQuestions = exam.TotalQuestions,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                ServerTime = Clock.Now,
                RemainingSeconds = RemainingSeconds(attempt),
                IsResumed = resumed,
                Parts = BuildTakeParts(tree, layout)
            };

            if (resumed)
            {
                output.SavedAnswers = _answerRepository.GetAll()
                    .Where(x => x.ExamAttemptId == attempt.Id && x.SelectedOptionId != null)
                    .Select(x => new SavedAnswerDto { QuestionId = x.QuestionId, OptionId = x.SelectedOptionId })
                    .ToList();
            }
            return output;
        }

        // ============================ LƯU ĐÁP ÁN TẠM ============================

        public async Task<SaveAnswerOutput> SaveAnswerAsync(SaveAnswerInput input)
        {
            var attempt = await GetOwnAttemptAsync(input.AttemptId);
            if (attempt.Status != AttemptStatus.InProgress)
                throw new UserFriendlyException("Bài thi đã được nộp, không thể thay đổi đáp án.");
            if (IsExpired(attempt))
            {
                await FinalizeAsync(attempt, autoSubmitted: true);
                throw new UserFriendlyException("Đã hết giờ làm bài. Bài thi đã được tự động nộp.");
            }

            var layout = AttemptLayout.FromJson(attempt.ShuffleMapJson);
            var lq = layout.AllQuestions.FirstOrDefault(q => q.QuestionId == input.QuestionId);
            if (lq == null)
                throw new UserFriendlyException("Câu hỏi không thuộc đề thi này.");
            if (input.OptionId.HasValue && !lq.OptionIds.Contains(input.OptionId.Value))
                throw new UserFriendlyException("Đáp án không thuộc câu hỏi này.");

            var answer = await _answerRepository.FirstOrDefaultAsync(x => x.ExamAttemptId == attempt.Id && x.QuestionId == input.QuestionId);
            if (answer == null)
            {
                await _answerRepository.InsertAsync(new AttemptAnswer
                {
                    ExamAttemptId = attempt.Id,
                    QuestionId = input.QuestionId,
                    SelectedOptionId = input.OptionId,
                    AnsweredTime = Clock.Now
                });
            }
            else
            {
                answer.SelectedOptionId = input.OptionId;   // đổi đáp án hoặc huỷ chọn (null)
                answer.AnsweredTime = Clock.Now;
            }
            await CurrentUnitOfWork.SaveChangesAsync();

            return new SaveAnswerOutput
            {
                Saved = true,
                AnsweredCount = _answerRepository.GetAll().Count(x => x.ExamAttemptId == attempt.Id && x.SelectedOptionId != null),
                RemainingSeconds = RemainingSeconds(attempt)
            };
        }

        // ============================ NỘP BÀI & KẾT QUẢ ============================

        public async Task<AttemptResultDto> SubmitAsync(EntityDto<long> input)
        {
            var attempt = await GetOwnAttemptAsync(input.Id);
            if (attempt.Status == AttemptStatus.InProgress)
                await FinalizeAsync(attempt, autoSubmitted: IsExpired(attempt));
            return await BuildResultAsync(attempt);
        }

        public async Task<AttemptResultDto> GetResultAsync(EntityDto<long> input)
        {
            var attempt = await GetOwnAttemptAsync(input.Id);
            if (attempt.Status == AttemptStatus.InProgress)
            {
                if (!IsExpired(attempt))
                    throw new UserFriendlyException("Bài thi chưa được nộp.");
                await FinalizeAsync(attempt, autoSubmitted: true);
            }
            return await BuildResultAsync(attempt);
        }

        public Task<List<MyAttemptDto>> GetMyAttemptsAsync()
        {
            var userId = AbpSession.GetUserId();
            var list = (from a in _attemptRepository.GetAll()
                        join e in _examRepository.GetAll() on a.ExamId equals e.Id
                        where a.UserId == userId && a.Status != AttemptStatus.InProgress
                        orderby a.StartTime descending
                        select new MyAttemptDto
                        {
                            AttemptId = a.Id,
                            ExamId = e.Id,
                            ExamTitle = e.Title,
                            Status = a.Status,
                            StartTime = a.StartTime,
                            SubmitTime = a.SubmitTime,
                            CorrectCount = a.CorrectCount,
                            TotalQuestions = e.TotalQuestions
                        }).ToList();
            return Task.FromResult(list);
        }

        // ============================ HÀM NỘI BỘ ============================

        private async Task<ExamAttempt> GetOwnAttemptAsync(long attemptId)
        {
            var attempt = await _attemptRepository.GetAsync(attemptId);
            if (attempt.UserId != AbpSession.GetUserId())
                throw new AbpAuthorizationException("Bạn không có quyền truy cập bài thi này.");
            return attempt;
        }

        private static bool IsExpired(ExamAttempt attempt) => Clock.Now > attempt.EndTime.AddSeconds(GraceSeconds);

        private static int RemainingSeconds(ExamAttempt attempt) =>
            Math.Max(0, (int)Math.Floor((attempt.EndTime - Clock.Now).TotalSeconds));

        /// <summary>Chấm điểm: so khớp đáp án đã chọn với đáp án đúng, đếm đúng / sai / bỏ qua</summary>
        private async Task FinalizeAsync(ExamAttempt attempt, bool autoSubmitted)
        {
            var layout = AttemptLayout.FromJson(attempt.ShuffleMapJson);
            var questionIds = layout.AllQuestions.Select(q => q.QuestionId).ToList();

            var correctOptionByQuestion = _optionRepository.GetAll()
                .Where(o => questionIds.Contains(o.QuestionId) && o.IsCorrect)
                .Select(o => new { o.QuestionId, o.Id })
                .ToList()
                .GroupBy(x => x.QuestionId)
                .ToDictionary(g => g.Key, g => g.First().Id);

            var answers = _answerRepository.GetAll().Where(x => x.ExamAttemptId == attempt.Id).ToList();
            int correct = 0, wrong = 0, skipped = 0;

            foreach (var qid in questionIds)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == qid);
                if (answer == null)
                {
                    // câu chưa làm cũng được lưu (SelectedOptionId = null) để thống kê "bỏ qua"
                    answer = new AttemptAnswer { ExamAttemptId = attempt.Id, QuestionId = qid };
                    await _answerRepository.InsertAsync(answer);
                }

                if (answer.SelectedOptionId == null)
                {
                    answer.IsCorrect = false;
                    skipped++;
                }
                else
                {
                    answer.IsCorrect = correctOptionByQuestion.TryGetValue(qid, out var correctId) && correctId == answer.SelectedOptionId;
                    if (answer.IsCorrect) correct++; else wrong++;
                }
            }

            var now = Clock.Now;
            attempt.CorrectCount = correct;
            attempt.WrongCount = wrong;
            attempt.SkippedCount = skipped;
            attempt.Status = autoSubmitted ? AttemptStatus.AutoSubmitted : AttemptStatus.Submitted;
            attempt.SubmitTime = autoSubmitted && now > attempt.EndTime ? attempt.EndTime : now;
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        private List<TakePartDto> BuildTakeParts(ExamDetailDto tree, AttemptLayout layout)
        {
            var questionsById = tree.Parts
                .SelectMany(p => p.Questions.Concat(p.Groups.SelectMany(g => g.Questions)))
                .ToDictionary(q => q.Id);
            var groupsById = tree.Parts.SelectMany(p => p.Groups).ToDictionary(g => g.Id);
            var partsById = tree.Parts.ToDictionary(p => p.Id);
            var originalToDisplay = layout.AllQuestions.ToDictionary(q => q.OriginalNumber, q => q.DisplayNumber);

            var result = new List<TakePartDto>();
            foreach (var lp in layout.Parts)
            {
                if (!partsById.TryGetValue(lp.PartId, out var part)) continue;
                var tp = new TakePartDto { PartNumber = part.PartNumber, Directions = part.Directions };

                foreach (var lg in lp.Groups)
                {
                    var tg = new TakeGroupDto { GroupId = lg.GroupId };
                    if (lg.GroupId.HasValue && groupsById.TryGetValue(lg.GroupId.Value, out var group))
                        tg.Passages = group.Passages.Select(ps => AttemptLayout.RenumberBlanks(ps.Content, originalToDisplay)).ToList();

                    foreach (var lq in lg.Questions)
                    {
                        if (!questionsById.TryGetValue(lq.QuestionId, out var q)) continue;
                        var optionsById = q.Options.ToDictionary(o => o.Id);
                        var tq = new TakeQuestionDto
                        {
                            QuestionId = q.Id,
                            DisplayNumber = lq.DisplayNumber,
                            PartNumber = part.PartNumber,
                            Content = q.Content
                        };
                        for (int i = 0; i < lq.OptionIds.Count; i++)
                        {
                            if (!optionsById.TryGetValue(lq.OptionIds[i], out var o)) continue;
                            // KHÔNG gửi IsCorrect và Hint xuống app khi đang thi
                            tq.Options.Add(new TakeOptionDto { OptionId = o.Id, Label = ((char)('A' + i)).ToString(), Content = o.Content });
                        }
                        tg.Questions.Add(tq);
                    }

                    if (tg.Questions.Count > 0)
                    {
                        tg.FromNumber = tg.Questions.Min(x => x.DisplayNumber);
                        tg.ToNumber = tg.Questions.Max(x => x.DisplayNumber);
                    }
                    tp.Groups.Add(tg);
                }
                result.Add(tp);
            }
            return result;
        }

        private Task<AttemptResultDto> BuildResultAsync(ExamAttempt attempt)
        {
            var exam = _examRepository.Get(attempt.ExamId);
            var tree = _loader.Load(exam);
            var layout = AttemptLayout.FromJson(attempt.ShuffleMapJson);
            var questionsById = tree.Parts
                .SelectMany(p => p.Questions.Concat(p.Groups.SelectMany(g => g.Questions)))
                .ToDictionary(q => q.Id);
            var answers = _answerRepository.GetAll()
                .Where(x => x.ExamAttemptId == attempt.Id)
                .ToList()
                .GroupBy(x => x.QuestionId)
                .ToDictionary(g => g.Key, g => g.First());

            var total = layout.AllQuestions.Count();
            var result = new AttemptResultDto
            {
                AttemptId = attempt.Id,
                ExamId = exam.Id,
                Title = exam.Title,
                Status = attempt.Status,
                AutoSubmitted = attempt.Status == AttemptStatus.AutoSubmitted,
                StartTime = attempt.StartTime,
                SubmitTime = attempt.SubmitTime,
                DurationUsedSeconds = attempt.SubmitTime.HasValue
                    ? Math.Max(0, (int)(attempt.SubmitTime.Value - attempt.StartTime).TotalSeconds) : 0,
                TotalQuestions = total,
                CorrectCount = attempt.CorrectCount,
                WrongCount = attempt.WrongCount,
                SkippedCount = attempt.SkippedCount,
                ScorePercent = total == 0 ? 0 : Math.Round(attempt.CorrectCount * 100.0 / total, 1)
            };

            foreach (var lp in layout.Parts)
            {
                var pr = new PartResultDto { PartNumber = lp.PartNumber };
                foreach (var lq in lp.Groups.SelectMany(g => g.Questions))
                {
                    if (!questionsById.TryGetValue(lq.QuestionId, out var q)) continue;
                    answers.TryGetValue(q.Id, out var answer);
                    var optionsById = q.Options.ToDictionary(o => o.Id);

                    var review = new ReviewQuestionDto
                    {
                        QuestionId = q.Id,
                        DisplayNumber = lq.DisplayNumber,
                        PartNumber = lp.PartNumber,
                        Content = q.Content,
                        Hint = q.Hint,
                        IsSkipped = answer?.SelectedOptionId == null,
                        IsCorrect = answer?.IsCorrect ?? false
                    };
                    for (int i = 0; i < lq.OptionIds.Count; i++)
                    {
                        if (!optionsById.TryGetValue(lq.OptionIds[i], out var o)) continue;
                        var label = ((char)('A' + i)).ToString();
                        var selected = answer?.SelectedOptionId == o.Id;
                        if (o.IsCorrect) review.CorrectLabel = label;
                        if (selected) review.SelectedLabel = label;
                        review.Options.Add(new ReviewOptionDto { Label = label, Content = o.Content, IsCorrect = o.IsCorrect, IsSelected = selected });
                    }
                    result.Review.Add(review);

                    pr.Total++;
                    if (review.IsSkipped) pr.Skipped++;
                    else if (review.IsCorrect) pr.Correct++;
                    else pr.Wrong++;
                }
                result.Parts.Add(pr);
            }
            return Task.FromResult(result);
        }
    }
}

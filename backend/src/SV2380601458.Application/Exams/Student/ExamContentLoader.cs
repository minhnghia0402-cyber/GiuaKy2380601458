using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using SV2380601458.Exams.Dto;

namespace SV2380601458.Exams.Student
{
    /// <summary>Đọc toàn bộ cây nội dung một đề (mỗi bảng một truy vấn)</summary>
    public class ExamContentLoader : ITransientDependency
    {
        private readonly IRepository<ExamPart, int> _partRepository;
        private readonly IRepository<QuestionGroup, int> _groupRepository;
        private readonly IRepository<Passage, int> _passageRepository;
        private readonly IRepository<Question, int> _questionRepository;
        private readonly IRepository<AnswerOption, int> _optionRepository;

        public ExamContentLoader(
            IRepository<ExamPart, int> partRepository,
            IRepository<QuestionGroup, int> groupRepository,
            IRepository<Passage, int> passageRepository,
            IRepository<Question, int> questionRepository,
            IRepository<AnswerOption, int> optionRepository)
        {
            _partRepository = partRepository;
            _groupRepository = groupRepository;
            _passageRepository = passageRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
        }

        public ExamDetailDto Load(Exam exam)
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
                PublishedTime = exam.PublishedTime
            };

            foreach (var part in parts)
            {
                QuestionDto Map(Question q) => new QuestionDto
                {
                    Id = q.Id,
                    QuestionNumber = q.QuestionNumber,
                    PartNumber = part.PartNumber,
                    Content = q.Content,
                    Hint = q.Hint,
                    AllowShuffleAnswers = q.AllowShuffleAnswers,
                    Options = (optionsByQuestion.TryGetValue(q.Id, out var list) ? list : new List<AnswerOption>())
                        .Select(o => new AnswerOptionDto { Id = o.Id, Label = o.Label, Content = o.Content, IsCorrect = o.IsCorrect })
                        .ToList()
                };

                dto.Parts.Add(new ExamPartDto
                {
                    Id = part.Id,
                    PartNumber = part.PartNumber,
                    Directions = part.Directions,
                    AllowShuffle = part.AllowShuffle,
                    Questions = questions.Where(q => q.ExamPartId == part.Id && q.QuestionGroupId == null).Select(Map).ToList(),
                    Groups = groups.Where(g => g.ExamPartId == part.Id).Select(g => new QuestionGroupDto
                    {
                        Id = g.Id,
                        FromNumber = g.FromNumber,
                        ToNumber = g.ToNumber,
                        AllowShuffleQuestions = g.AllowShuffleQuestions,
                        Passages = passages.Where(p => p.QuestionGroupId == g.Id)
                            .Select(p => new PassageDto { Id = p.Id, OrderIndex = p.OrderIndex, Content = p.Content }).ToList(),
                        Questions = questions.Where(q => q.QuestionGroupId == g.Id).Select(Map).ToList()
                    }).ToList()
                });
            }
            return dto;
        }
    }
}

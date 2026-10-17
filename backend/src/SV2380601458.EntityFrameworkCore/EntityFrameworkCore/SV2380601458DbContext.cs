using SV2380601458.Exams;
using Abp.Zero.EntityFrameworkCore;
using SV2380601458.Authorization.Roles;
using SV2380601458.Authorization.Users;
using SV2380601458.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace SV2380601458.EntityFrameworkCore;

public class SV2380601458DbContext : AbpZeroDbContext<Tenant, Role, User, SV2380601458DbContext>
{
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamPart> ExamParts { get; set; }
        public DbSet<QuestionGroup> QuestionGroups { get; set; }
        public DbSet<Passage> Passages { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<AttemptAnswer> AttemptAnswers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureToeicExam();
        }
    /* Define a DbSet for each entity of the application */

    public SV2380601458DbContext(DbContextOptions<SV2380601458DbContext> options)
        : base(options)
    {
    }
}


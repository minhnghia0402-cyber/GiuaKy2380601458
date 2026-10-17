using Microsoft.EntityFrameworkCore;
using SV2380601458.Authorization.Users;
using SV2380601458.Exams;

namespace SV2380601458.EntityFrameworkCore
{
    /// <summary>
    /// Cấu hình bảng, khoá ngoại, index cho các entity thi TOEIC.
    /// Gọi trong SV2380601458DbContext.OnModelCreating: modelBuilder.ConfigureToeicExam();
    /// </summary>
    public static class ToeicModelBuilderExtensions
    {
        public static void ConfigureToeicExam(this ModelBuilder b)
        {
            b.Entity<Exam>(e =>
            {
                e.ToTable("Exams");
                e.HasIndex(x => x.Status);
            });

            b.Entity<ExamPart>(e =>
            {
                e.ToTable("ExamParts");
                e.HasOne(x => x.Exam).WithMany(x => x.Parts)
                    .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.ExamId, x.PartNumber }).IsUnique();
            });

            b.Entity<QuestionGroup>(e =>
            {
                e.ToTable("QuestionGroups");
                e.HasOne(x => x.ExamPart).WithMany(x => x.Groups)
                    .HasForeignKey(x => x.ExamPartId).OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Passage>(e =>
            {
                e.ToTable("Passages");
                e.HasOne(x => x.QuestionGroup).WithMany(x => x.Passages)
                    .HasForeignKey(x => x.QuestionGroupId).OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Question>(e =>
            {
                e.ToTable("Questions");
                e.HasOne(x => x.ExamPart).WithMany(x => x.Questions)
                    .HasForeignKey(x => x.ExamPartId).OnDelete(DeleteBehavior.Cascade);
                // SQL Server không cho 2 đường cascade (Part->Question và Part->Group->Question)
                // nên nhánh Group->Question để EF tự xoá ở phía ứng dụng (ClientCascade)
                e.HasOne(x => x.QuestionGroup).WithMany(x => x.Questions)
                    .HasForeignKey(x => x.QuestionGroupId).OnDelete(DeleteBehavior.ClientCascade);
                e.HasIndex(x => new { x.ExamPartId, x.QuestionNumber }).IsUnique();
            });

            b.Entity<AnswerOption>(e =>
            {
                e.ToTable("AnswerOptions");
                e.HasOne(x => x.Question).WithMany(x => x.Options)
                    .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<ExamAttempt>(e =>
            {
                e.ToTable("ExamAttempts");
                e.HasOne(x => x.Exam).WithMany(x => x.Attempts)
                    .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<User>().WithMany()
                    .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.ExamId, x.UserId });
            });

            b.Entity<AttemptAnswer>(e =>
            {
                e.ToTable("AttemptAnswers");
                e.HasOne(x => x.ExamAttempt).WithMany(x => x.Answers)
                    .HasForeignKey(x => x.ExamAttemptId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Question).WithMany()
                    .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.SelectedOption).WithMany()
                    .HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.ExamAttemptId, x.QuestionId }).IsUnique();
            });
        }
    }
}

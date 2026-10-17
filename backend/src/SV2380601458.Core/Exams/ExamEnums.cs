namespace SV2380601458.Exams
{
    /// <summary>Trạng thái đề thi</summary>
    public enum ExamStatus
    {
        Draft = 0,      // Mới import, giảng viên đang rà soát/chỉnh sửa
        Published = 1,  // Đã xuất bản, sinh viên được thi
        Closed = 2      // Đã đóng ca thi
    }

    /// <summary>Trạng thái một lượt làm bài</summary>
    public enum AttemptStatus
    {
        InProgress = 0,     // Đang làm
        Submitted = 1,      // Sinh viên tự nộp
        AutoSubmitted = 2   // Hết giờ, hệ thống tự thu bài
    }
}

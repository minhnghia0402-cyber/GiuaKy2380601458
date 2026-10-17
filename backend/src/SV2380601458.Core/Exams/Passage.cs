using Abp.Domain.Entities;

namespace SV2380601458.Exams
{
    /// <summary>Một bài đọc trong Group ([PASSAGE] ... [/PASSAGE])</summary>
    public class Passage : Entity<int>
    {
        public int QuestionGroupId { get; set; }
        public virtual QuestionGroup QuestionGroup { get; set; }

        public int OrderIndex { get; set; }

        /// <summary>
        /// Nội dung dạng HTML: đoạn văn &lt;p&gt;, ảnh &lt;img src="/uploads/exams/..."&gt;,
        /// chỗ trống Part 6 lưu dạng &lt;span class="blank" data-q="131"&gt;---131---&lt;/span&gt;.
        /// Lưu HTML giúp giữ đúng thứ tự chữ/ảnh và hiển thị trực tiếp trên web/mobile.
        /// </summary>
        public string Content { get; set; }
    }
}

using System.Collections.Generic;

namespace SV2380601458.Web.Models.Exams
{
    public class ImportExamViewModel
    {
        public string FileName { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace SV2380601458.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}
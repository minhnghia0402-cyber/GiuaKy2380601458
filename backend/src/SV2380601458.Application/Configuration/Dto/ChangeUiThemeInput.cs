using System.ComponentModel.DataAnnotations;

namespace SV2380601458.Configuration.Dto;

public class ChangeUiThemeInput
{
    [Required]
    [StringLength(32)]
    public string Theme { get; set; }
}

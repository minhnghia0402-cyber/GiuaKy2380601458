using Abp.MultiTenancy;
using System.ComponentModel.DataAnnotations;

namespace SV2380601458.Authorization.Accounts.Dto;

public class IsTenantAvailableInput
{
    [Required]
    [StringLength(AbpTenantBase.MaxTenancyNameLength)]
    public string TenancyName { get; set; }
}

using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SV2380601458.MultiTenancy;

namespace SV2380601458.Sessions.Dto;

[AutoMapFrom(typeof(Tenant))]
public class TenantLoginInfoDto : EntityDto
{
    public string TenancyName { get; set; }

    public string Name { get; set; }
}

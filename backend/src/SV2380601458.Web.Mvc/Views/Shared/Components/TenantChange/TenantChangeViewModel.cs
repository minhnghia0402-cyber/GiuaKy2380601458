using Abp.AutoMapper;
using SV2380601458.Sessions.Dto;

namespace SV2380601458.Web.Views.Shared.Components.TenantChange;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput))]
public class TenantChangeViewModel
{
    public TenantLoginInfoDto Tenant { get; set; }
}

using Abp.Application.Services;
using SV2380601458.MultiTenancy.Dto;

namespace SV2380601458.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}


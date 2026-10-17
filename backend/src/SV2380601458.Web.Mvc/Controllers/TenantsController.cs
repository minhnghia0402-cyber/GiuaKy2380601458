using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using SV2380601458.Authorization;
using SV2380601458.Controllers;
using SV2380601458.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace SV2380601458.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Tenants)]
public class TenantsController : SV2380601458ControllerBase
{
    private readonly ITenantAppService _tenantAppService;

    public TenantsController(ITenantAppService tenantAppService)
    {
        _tenantAppService = tenantAppService;
    }

    public ActionResult Index() => View();

    public async Task<ActionResult> EditModal(int tenantId)
    {
        var tenantDto = await _tenantAppService.GetAsync(new EntityDto(tenantId));
        return PartialView("_EditModal", tenantDto);
    }
}

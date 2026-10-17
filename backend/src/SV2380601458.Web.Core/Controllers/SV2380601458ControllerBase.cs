using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace SV2380601458.Controllers
{
    public abstract class SV2380601458ControllerBase : AbpController
    {
        protected SV2380601458ControllerBase()
        {
            LocalizationSourceName = SV2380601458Consts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}

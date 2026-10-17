using Abp.AspNetCore.Mvc.ViewComponents;

namespace SV2380601458.Web.Views;

public abstract class SV2380601458ViewComponent : AbpViewComponent
{
    protected SV2380601458ViewComponent()
    {
        LocalizationSourceName = SV2380601458Consts.LocalizationSourceName;
    }
}

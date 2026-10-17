using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace SV2380601458.Web.Views;

public abstract class SV2380601458RazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected SV2380601458RazorPage()
    {
        LocalizationSourceName = SV2380601458Consts.LocalizationSourceName;
    }
}

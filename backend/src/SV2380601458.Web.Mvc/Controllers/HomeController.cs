using Abp.AspNetCore.Mvc.Authorization;
using SV2380601458.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace SV2380601458.Web.Controllers;

[AbpMvcAuthorize]
public class HomeController : SV2380601458ControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}

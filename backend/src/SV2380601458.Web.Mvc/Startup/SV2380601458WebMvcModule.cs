using Abp.Modules;
using Abp.Reflection.Extensions;
using SV2380601458.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SV2380601458.Web.Startup;

[DependsOn(typeof(SV2380601458WebCoreModule))]
public class SV2380601458WebMvcModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public SV2380601458WebMvcModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Navigation.Providers.Add<SV2380601458NavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(SV2380601458WebMvcModule).GetAssembly());
    }
}

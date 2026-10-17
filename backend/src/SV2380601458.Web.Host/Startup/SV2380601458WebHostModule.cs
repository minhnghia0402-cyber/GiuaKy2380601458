using Abp.Modules;
using Abp.Reflection.Extensions;
using SV2380601458.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SV2380601458.Web.Host.Startup
{
    [DependsOn(
       typeof(SV2380601458WebCoreModule))]
    public class SV2380601458WebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public SV2380601458WebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(SV2380601458WebHostModule).GetAssembly());
        }
    }
}

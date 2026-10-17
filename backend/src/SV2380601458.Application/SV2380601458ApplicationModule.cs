using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using SV2380601458.Authorization;

namespace SV2380601458;

[DependsOn(
    typeof(SV2380601458CoreModule),
    typeof(AbpAutoMapperModule))]
public class SV2380601458ApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<SV2380601458AuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(SV2380601458ApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}

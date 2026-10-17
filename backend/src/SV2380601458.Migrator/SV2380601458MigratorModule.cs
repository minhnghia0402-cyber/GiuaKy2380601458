using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using SV2380601458.Configuration;
using SV2380601458.EntityFrameworkCore;
using SV2380601458.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace SV2380601458.Migrator;

[DependsOn(typeof(SV2380601458EntityFrameworkModule))]
public class SV2380601458MigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public SV2380601458MigratorModule(SV2380601458EntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(SV2380601458MigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            SV2380601458Consts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(SV2380601458MigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}

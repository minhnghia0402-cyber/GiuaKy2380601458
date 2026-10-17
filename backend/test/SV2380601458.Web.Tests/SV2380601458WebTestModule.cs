using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using SV2380601458.EntityFrameworkCore;
using SV2380601458.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace SV2380601458.Web.Tests;

[DependsOn(
    typeof(SV2380601458WebMvcModule),
    typeof(AbpAspNetCoreTestBaseModule)
)]
public class SV2380601458WebTestModule : AbpModule
{
    public SV2380601458WebTestModule(SV2380601458EntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(SV2380601458WebTestModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(SV2380601458WebMvcModule).Assembly);
    }
}
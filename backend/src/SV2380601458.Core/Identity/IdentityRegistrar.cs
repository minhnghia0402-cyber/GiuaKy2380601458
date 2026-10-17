using SV2380601458.Authorization;
using SV2380601458.Authorization.Roles;
using SV2380601458.Authorization.Users;
using SV2380601458.Editions;
using SV2380601458.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace SV2380601458.Identity;

public static class IdentityRegistrar
{
    public static IdentityBuilder Register(IServiceCollection services)
    {
        services.AddLogging();

        return services.AddAbpIdentity<Tenant, User, Role>()
            .AddAbpTenantManager<TenantManager>()
            .AddAbpUserManager<UserManager>()
            .AddAbpRoleManager<RoleManager>()
            .AddAbpEditionManager<EditionManager>()
            .AddAbpUserStore<UserStore>()
            .AddAbpRoleStore<RoleStore>()
            .AddAbpLogInManager<LogInManager>()
            .AddAbpSignInManager<SignInManager>()
            .AddAbpSecurityStampValidator<SecurityStampValidator>()
            .AddAbpUserClaimsPrincipalFactory<UserClaimsPrincipalFactory>()
            .AddPermissionChecker<PermissionChecker>()
            .AddDefaultTokenProviders();
    }
}

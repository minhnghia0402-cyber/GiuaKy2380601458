using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace SV2380601458.Authorization;

public class SV2380601458AuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
            context.CreatePermission("Pages.Exams", new FixedLocalizableString("Quản lý đề thi"));
        context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
        context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
        context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
        context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, SV2380601458Consts.LocalizationSourceName);
    }
}



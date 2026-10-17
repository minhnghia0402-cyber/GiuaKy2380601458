using Abp.Authorization;
using SV2380601458.Authorization.Roles;
using SV2380601458.Authorization.Users;

namespace SV2380601458.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}

using SV2380601458.Roles.Dto;
using SV2380601458.Users.Dto;
using System.Collections.Generic;
using System.Linq;

namespace SV2380601458.Web.Models.Users;

public class EditUserModalViewModel
{
    public UserDto User { get; set; }

    public IReadOnlyList<RoleDto> Roles { get; set; }

    public bool UserIsInRole(RoleDto role)
    {
        return User.RoleNames != null && User.RoleNames.Any(r => r == role.NormalizedName);
    }
}

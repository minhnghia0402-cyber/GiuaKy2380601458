using SV2380601458.Roles.Dto;
using System.Collections.Generic;

namespace SV2380601458.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}

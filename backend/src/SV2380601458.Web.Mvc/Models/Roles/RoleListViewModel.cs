using SV2380601458.Roles.Dto;
using System.Collections.Generic;

namespace SV2380601458.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}

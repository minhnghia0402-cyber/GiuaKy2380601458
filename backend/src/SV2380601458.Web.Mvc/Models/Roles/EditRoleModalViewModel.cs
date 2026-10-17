using Abp.AutoMapper;
using SV2380601458.Roles.Dto;
using SV2380601458.Web.Models.Common;

namespace SV2380601458.Web.Models.Roles;

[AutoMapFrom(typeof(GetRoleForEditOutput))]
public class EditRoleModalViewModel : GetRoleForEditOutput, IPermissionsEditViewModel
{
    public bool HasPermission(FlatPermissionDto permission)
    {
        return GrantedPermissionNames.Contains(permission.Name);
    }
}

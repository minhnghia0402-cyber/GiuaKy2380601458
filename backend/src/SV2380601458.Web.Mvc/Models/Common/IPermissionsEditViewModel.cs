using SV2380601458.Roles.Dto;
using System.Collections.Generic;

namespace SV2380601458.Web.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }
}
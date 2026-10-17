using Abp.Authorization;
using Abp.Runtime.Session;
using SV2380601458.Configuration.Dto;
using System.Threading.Tasks;

namespace SV2380601458.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : SV2380601458AppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}

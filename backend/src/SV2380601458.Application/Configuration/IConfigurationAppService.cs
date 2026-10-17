using SV2380601458.Configuration.Dto;
using System.Threading.Tasks;

namespace SV2380601458.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}

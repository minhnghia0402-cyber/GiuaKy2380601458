using Abp.Application.Services;
using SV2380601458.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace SV2380601458.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}

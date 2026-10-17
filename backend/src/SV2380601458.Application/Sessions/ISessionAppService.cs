using Abp.Application.Services;
using SV2380601458.Sessions.Dto;
using System.Threading.Tasks;

namespace SV2380601458.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}

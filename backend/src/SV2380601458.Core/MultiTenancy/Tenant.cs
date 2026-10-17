using Abp.MultiTenancy;
using SV2380601458.Authorization.Users;

namespace SV2380601458.MultiTenancy;

public class Tenant : AbpTenant<User>
{
    public Tenant()
    {
    }

    public Tenant(string tenancyName, string name)
        : base(tenancyName, name)
    {
    }
}

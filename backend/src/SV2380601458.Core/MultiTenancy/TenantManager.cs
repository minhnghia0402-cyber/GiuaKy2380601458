using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.MultiTenancy;
using SV2380601458.Authorization.Users;
using SV2380601458.Editions;

namespace SV2380601458.MultiTenancy;

public class TenantManager : AbpTenantManager<Tenant, User>
{
    public TenantManager(
        IRepository<Tenant> tenantRepository,
        IRepository<TenantFeatureSetting, long> tenantFeatureRepository,
        EditionManager editionManager,
        IAbpZeroFeatureValueStore featureValueStore)
        : base(
            tenantRepository,
            tenantFeatureRepository,
            editionManager,
            featureValueStore)
    {
    }
}

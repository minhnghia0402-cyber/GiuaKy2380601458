using Abp.MultiTenancy;
using SV2380601458.Editions;
using SV2380601458.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SV2380601458.EntityFrameworkCore.Seed.Tenants;

public class DefaultTenantBuilder
{
    private readonly SV2380601458DbContext _context;

    public DefaultTenantBuilder(SV2380601458DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateDefaultTenant();
    }

    private void CreateDefaultTenant()
    {
        // Default tenant

        var defaultTenant = _context.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.TenancyName == AbpTenantBase.DefaultTenantName);
        if (defaultTenant == null)
        {
            defaultTenant = new Tenant(AbpTenantBase.DefaultTenantName, AbpTenantBase.DefaultTenantName);

            var defaultEdition = _context.Editions.IgnoreQueryFilters().FirstOrDefault(e => e.Name == EditionManager.DefaultEditionName);
            if (defaultEdition != null)
            {
                defaultTenant.EditionId = defaultEdition.Id;
            }

            _context.Tenants.Add(defaultTenant);
            _context.SaveChanges();
        }
    }
}

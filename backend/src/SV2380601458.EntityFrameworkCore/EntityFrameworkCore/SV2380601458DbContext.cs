using Abp.Zero.EntityFrameworkCore;
using SV2380601458.Authorization.Roles;
using SV2380601458.Authorization.Users;
using SV2380601458.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace SV2380601458.EntityFrameworkCore;

public class SV2380601458DbContext : AbpZeroDbContext<Tenant, Role, User, SV2380601458DbContext>
{
    /* Define a DbSet for each entity of the application */

    public SV2380601458DbContext(DbContextOptions<SV2380601458DbContext> options)
        : base(options)
    {
    }
}

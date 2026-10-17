using Abp;
using Abp.Authorization.Users;
using Abp.Events.Bus;
using Abp.Events.Bus.Entities;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using Abp.TestBase;
using SV2380601458.Authorization.Users;
using SV2380601458.EntityFrameworkCore;
using SV2380601458.EntityFrameworkCore.Seed.Host;
using SV2380601458.EntityFrameworkCore.Seed.Tenants;
using SV2380601458.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SV2380601458.Tests;

public abstract class SV2380601458TestBase : AbpIntegratedTestBase<SV2380601458TestModule>
{
    protected SV2380601458TestBase()
    {
        void NormalizeDbContext(SV2380601458DbContext context)
        {
            context.EntityChangeEventHelper = NullEntityChangeEventHelper.Instance;
            context.EventBus = NullEventBus.Instance;
            context.SuppressAutoSetTenantId = true;
        }

        // Seed initial data for host
        AbpSession.TenantId = null;
        UsingDbContext(context =>
        {
            NormalizeDbContext(context);
            new InitialHostDbBuilder(context).Create();
            new DefaultTenantBuilder(context).Create();
        });

        // Seed initial data for default tenant
        AbpSession.TenantId = 1;
        UsingDbContext(context =>
        {
            NormalizeDbContext(context);
            new TenantRoleAndUserBuilder(context, 1).Create();
        });

        LoginAsDefaultTenantAdmin();
    }

    #region UsingDbContext

    protected IDisposable UsingTenantId(int? tenantId)
    {
        var previousTenantId = AbpSession.TenantId;
        AbpSession.TenantId = tenantId;
        return new DisposeAction(() => AbpSession.TenantId = previousTenantId);
    }

    protected void UsingDbContext(Action<SV2380601458DbContext> action)
    {
        UsingDbContext(AbpSession.TenantId, action);
    }

    protected Task UsingDbContextAsync(Func<SV2380601458DbContext, Task> action)
    {
        return UsingDbContextAsync(AbpSession.TenantId, action);
    }

    protected T UsingDbContext<T>(Func<SV2380601458DbContext, T> func)
    {
        return UsingDbContext(AbpSession.TenantId, func);
    }

    protected Task<T> UsingDbContextAsync<T>(Func<SV2380601458DbContext, Task<T>> func)
    {
        return UsingDbContextAsync(AbpSession.TenantId, func);
    }

    protected void UsingDbContext(int? tenantId, Action<SV2380601458DbContext> action)
    {
        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<SV2380601458DbContext>())
            {
                action(context);
                context.SaveChanges();
            }
        }
    }

    protected async Task UsingDbContextAsync(int? tenantId, Func<SV2380601458DbContext, Task> action)
    {
        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<SV2380601458DbContext>())
            {
                await action(context);
                await context.SaveChangesAsync();
            }
        }
    }

    protected T UsingDbContext<T>(int? tenantId, Func<SV2380601458DbContext, T> func)
    {
        T result;

        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<SV2380601458DbContext>())
            {
                result = func(context);
                context.SaveChanges();
            }
        }

        return result;
    }

    protected async Task<T> UsingDbContextAsync<T>(int? tenantId, Func<SV2380601458DbContext, Task<T>> func)
    {
        T result;

        using (UsingTenantId(tenantId))
        {
            using (var context = LocalIocManager.Resolve<SV2380601458DbContext>())
            {
                result = await func(context);
                await context.SaveChangesAsync();
            }
        }

        return result;
    }

    #endregion

    #region Login

    protected void LoginAsHostAdmin()
    {
        LoginAsHost(AbpUserBase.AdminUserName);
    }

    protected void LoginAsDefaultTenantAdmin()
    {
        LoginAsTenant(AbpTenantBase.DefaultTenantName, AbpUserBase.AdminUserName);
    }

    protected void LoginAsHost(string userName)
    {
        AbpSession.TenantId = null;

        var user =
            UsingDbContext(
                context =>
                    context.Users.FirstOrDefault(u => u.TenantId == AbpSession.TenantId && u.UserName == userName));
        if (user == null)
        {
            throw new Exception("There is no user: " + userName + " for host.");
        }

        AbpSession.UserId = user.Id;
    }

    protected void LoginAsTenant(string tenancyName, string userName)
    {
        var tenant = UsingDbContext(context => context.Tenants.FirstOrDefault(t => t.TenancyName == tenancyName));
        if (tenant == null)
        {
            throw new Exception("There is no tenant: " + tenancyName);
        }

        AbpSession.TenantId = tenant.Id;

        var user =
            UsingDbContext(
                context =>
                    context.Users.FirstOrDefault(u => u.TenantId == AbpSession.TenantId && u.UserName == userName));
        if (user == null)
        {
            throw new Exception("There is no user: " + userName + " for tenant: " + tenancyName);
        }

        AbpSession.UserId = user.Id;
    }

    #endregion

    /// <summary>
    /// Gets current user if <see cref="IAbpSession.UserId"/> is not null.
    /// Throws exception if it's null.
    /// </summary>
    protected async Task<User> GetCurrentUserAsync()
    {
        var userId = AbpSession.GetUserId();
        return await UsingDbContext(context => context.Users.SingleAsync(u => u.Id == userId));
    }

    /// <summary>
    /// Gets current tenant if <see cref="IAbpSession.TenantId"/> is not null.
    /// Throws exception if there is no current tenant.
    /// </summary>
    protected async Task<Tenant> GetCurrentTenantAsync()
    {
        var tenantId = AbpSession.GetTenantId();
        return await UsingDbContext(context => context.Tenants.SingleAsync(t => t.Id == tenantId));
    }
}

using SV2380601458.Configuration;
using SV2380601458.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SV2380601458.EntityFrameworkCore;

/* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
public class SV2380601458DbContextFactory : IDesignTimeDbContextFactory<SV2380601458DbContext>
{
    public SV2380601458DbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SV2380601458DbContext>();

        /*
         You can provide an environmentName parameter to the AppConfigurations.Get method. 
         In this case, AppConfigurations will try to read appsettings.{environmentName}.json.
         Use Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") method or from string[] args to get environment if necessary.
         https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#args
         */
        var configuration = AppConfigurations.Get(WebContentDirectoryFinder.CalculateContentRootFolder());

        SV2380601458DbContextConfigurer.Configure(builder, configuration.GetConnectionString(SV2380601458Consts.ConnectionStringName));

        return new SV2380601458DbContext(builder.Options);
    }
}

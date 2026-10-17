using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace SV2380601458.EntityFrameworkCore;

public static class SV2380601458DbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<SV2380601458DbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<SV2380601458DbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}

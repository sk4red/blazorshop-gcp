namespace BlazorShop.Web.Server.Infrastructure.Extensions
{
    using Microsoft.Extensions.Configuration;

    public static class ConfigurationExtensions
    {
        public static string GetDefaultConnectionString(this IConfiguration configuration)
            => configuration.GetConnectionString("DefaultConnection");
        public static string GetMysqlConnectionString(this IConfiguration configuration)
           => configuration.GetConnectionString("MySQLConnection");
    }
}

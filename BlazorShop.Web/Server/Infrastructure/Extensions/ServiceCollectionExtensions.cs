namespace BlazorShop.Web.Server.Infrastructure.Extensions
{
    using System.Linq;
    using System.Text;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.EntityFrameworkCore;
 
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using BlazorShop.Services.Common;
    using Data;
    using Data.Contracts;
    using Data.Models;
    using Data.Seed;
    using Filters;
    using Models;
    using Services;

    using static Data.ModelConstants.Identity;
    using System;
    using MySql.Data.MySqlClient;

    public static class ServiceCollectionExtensions
    {
        public static ApplicationSettings GetApplicationSettings(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var applicationSettingsConfiguration = configuration.GetSection(nameof(ApplicationSettings));
            services.Configure<ApplicationSettings>(applicationSettingsConfiguration);
            return applicationSettingsConfiguration.Get<ApplicationSettings>();
        }
        public static IServiceCollection AddMySQLDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string conn = "";
       
            string strdbtype = Environment.GetEnvironmentVariable("CLOUDPROV");

            // GCP Host
            if (strdbtype == "GCP")
            {
                String dbSocketDir = Environment.GetEnvironmentVariable("DB_SOCKET_PATH") ?? "/cloudsql";
                String instanceConnectionName = Environment.GetEnvironmentVariable("INSTANCE_CONNECTION_NAME");
                var connectionString = new MySqlConnectionStringBuilder()
                {
                    // The Cloud SQL proxy provides encryption between the proxy and instance.
                    SslMode = MySqlSslMode.None,
                    // Remember - storing secrets in plain text is potentially unsafe. Consider using
                    // something like https://cloud.google.com/secret-manager/docs/overview to help keep
                    // secrets secret.
                    Server = String.Format("{0}/{1}", dbSocketDir, instanceConnectionName),
                    UserID = Environment.GetEnvironmentVariable("DBUSER"),   // e.g. 'my-db-user
                    Password = Environment.GetEnvironmentVariable("DBPASSWORD"), // e.g. 'my-db-password'
                    Database = Environment.GetEnvironmentVariable("DBNAME"), // e.g. 'my-database'
                    ConnectionProtocol = MySqlConnectionProtocol.UnixSocket
                };
                connectionString.Pooling = true;
                // Specify additional properties here.
                conn = connectionString.ToString();
            } else
            {
                // Non Cloud Host
                string dbhost = Environment.GetEnvironmentVariable("DBHOST");
                string dbname = Environment.GetEnvironmentVariable("DBNAME");
                string dbuser = Environment.GetEnvironmentVariable("DBUSER");
                string dbpassword = Environment.GetEnvironmentVariable("DBPASSWORD");
                var config = new StringBuilder("Server=" + dbhost + ";Database=" + dbname + ";Uid=" + dbuser + ";Pwd=" + dbpassword + ";");
                conn = config.ToString();
            }
          

            services.AddDbContext<BlazorShopDbContext>(options => options
            .UseMySql(conn))
                 .AddTransient<IInitialData, CategoriesData>()
                .AddTransient<IInitialData, ProductsData>()
                .AddTransient<IInitializer, BlazorShopDbInitializer>();
           
            return services;
        }
        public static IServiceCollection AddDatabase(this IServiceCollection services,IConfiguration configuration)
            => services
                .AddDbContext<BlazorShopDbContext>(options => options
                //.UseSqlServer(configuration.GetDefaultConnectionString
                
                .UseMySql(configuration["ConnectionStrings:MySQLConnection"]))
                //   .UseMySql(configuration.GetConnectionString("MySQLConnection")))
                //.UseMySql("Server=185.4.49.4;Database=furmidge_mysqldb;Uid=furmidge_adm;Pwd=Pass@word1!;"))
                //.UseMySql(Environment.GetEnvironmentVariable("ConnectionStrings:MySQLConnection")))
                //     .UseMySql("Server=192.168.0.28;Database=BlazorShop;Uid=dfurmidge;Pwd=Taekw0nd0!;"))

                .AddTransient<IInitialData, CategoriesData>()
                .AddTransient<IInitialData, ProductsData>()
                .AddTransient<IInitializer, BlazorShopDbInitializer>();

        public static IServiceCollection AddIdentity(this IServiceCollection services)
        {
            services
                .AddIdentity<BlazorShopUser, BlazorShopRole>(options =>
                {
                    options.Password.RequiredLength = MinPasswordLength;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<BlazorShopDbContext>();

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            ApplicationSettings applicationSettings)
        {
            var key = Encoding.ASCII.GetBytes(applicationSettings.Secret);

            services
                .AddAuthentication(authentication =>
                {
                    authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(bearer =>
                {
                    bearer.RequireHttpsMetadata = false;
                    bearer.SaveToken = true;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var serviceInterfaceType = typeof(IService);
            var singletonServiceInterfaceType = typeof(ISingletonService);
            var scopedServiceInterfaceType = typeof(IScopedService);

            var types = serviceInterfaceType
                .Assembly
                .GetExportedTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Service = t.GetInterface($"I{t.Name}"),
                    Implementation = t
                })
                .Where(t => t.Service != null);

            foreach (var type in types)
            {
                if (serviceInterfaceType.IsAssignableFrom(type.Service))
                {
                    services.AddTransient(type.Service, type.Implementation);
                }
                else if (singletonServiceInterfaceType.IsAssignableFrom(type.Service))
                {
                    services.AddSingleton(type.Service, type.Implementation);
                }
                else if (scopedServiceInterfaceType.IsAssignableFrom(type.Service))
                {
                    services.AddScoped(type.Service, type.Implementation);
                }
            }

            return services;
        }

        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services
                .AddControllers(options => options
                      .Filters
                      .Add<ModelOrNotFoundActionFilter>());

            services.AddRazorPages();

            return services;
        }
    }
}

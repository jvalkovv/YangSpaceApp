using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data;
using YangSpaceApp.Server.Data.Models;

namespace YangSpaceApp.Server.Data.Extension
{
    public static class ServiceCollectionExtension
    {
        //Connection to DB
        public static IServiceCollection AddYangSpaceDbContext(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DefaultConnection");

            services.AddDbContext<YangSpaceDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                    options =>
                    {
                        options.EnableRetryOnFailure(maxRetryCount: 1, maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
            });


            return services;
        }
        public static IServiceCollection AddRegisterIdentity(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<YangSpaceDbContext>()
                .AddDefaultTokenProviders();
            return services;
        }


    }
}

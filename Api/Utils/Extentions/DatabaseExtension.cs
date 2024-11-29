using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Utils.Extentions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("Default")), ServiceLifetime.Transient);

            return services;
        }
    }
}
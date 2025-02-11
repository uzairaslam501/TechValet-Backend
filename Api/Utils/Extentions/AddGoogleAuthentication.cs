using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace ITValet.Utils.Extentions
{
    public static class GoogleExtension
    {
        public static IServiceCollection AddGoogleAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = configuration["GoogleAuth:ClientId"];
                googleOptions.ClientSecret = configuration["GoogleAuth:ClientSecret"];
                googleOptions.CallbackPath = "/auth/google/callback";
                googleOptions.Scope.Add("openid");
                googleOptions.Scope.Add("email");
                googleOptions.Scope.Add("profile");
            });

            return services;
        }
    }
}
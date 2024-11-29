using Stripe;

namespace ITValet.Utils.Extentions
{
    public static class StripeExtension
    {
        public static IServiceCollection ConfigureStripe(this IServiceCollection services, IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration["Stripe:StripeApiKey"];
            StripeConfiguration.ClientId = configuration["Stripe:ClientId"];
            return services;
        }
    }
}
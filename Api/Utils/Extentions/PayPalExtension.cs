using PayPalCheckoutSdk.Core;

namespace ITValet.Utils.Extentions
{
    public static class PayPalExtension
    {
        public static IServiceCollection ConfigurePayPal(this IServiceCollection services, IConfiguration configuration)
        {
            PayPalHttpClient paypalHttpClient;
            if (configuration.GetValue<bool>("PayPal:Live"))
            {
                var liveEnvironment = new LiveEnvironment(configuration["PayPal:ClientId"], configuration["PayPal:ClientSecret"]);
                paypalHttpClient = new PayPalHttpClient(liveEnvironment);
            }
            else
            {
                var sandboxEnvironment = new SandboxEnvironment(configuration["PayPal:ClientId"], configuration["PayPal:ClientSecret"]);
                paypalHttpClient = new PayPalHttpClient(sandboxEnvironment);
            }

            services.AddSingleton(paypalHttpClient);
            return services;
        }
    }
}

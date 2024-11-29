using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.NotificationHub;
using ITValet.Services;

namespace ITValet.Utils.Extentions
{
    public static class ApplicationServicesExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddHttpContextAccessor();

            // Dependency Injection
            RegisterRepositories(services);
            RegisterServices(services, configuration);

            return services;
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IUserEducationRepo, UserEducationRepo>();
            services.AddScoped<IUserSocialProfileRepo, UserSocialProfileRepo>();
            services.AddScoped<IUserExperienceRepo, UserExperienceRepo>();
            services.AddScoped<IUserSkillRepo, UserSkillRepo>();
            services.AddScoped<IUserTagRepo, UserTagRepo>();
            services.AddScoped<IUserAvailableSlotRepo, UserAvailableSlotRepo>();
            services.AddScoped<ISearchLogRepo, SearchLogRepo>();
            services.AddScoped<IContactUsRepo, ContactUsRepo>();
            services.AddScoped<IRequestServiceRepo, RequestServiceRepo>();
            services.AddScoped<IMessagesRepo, MessagesRepo>();
            services.AddScoped<IOfferDetailsRepo, OfferDetailsRepo>();
            services.AddScoped<IOrderRepo, OrderRepo>();
            services.AddScoped<IOrderReasonRepo, OrderReasonRepo>();
            services.AddScoped<INotificationRepo, NotificationRepo>();
            services.AddScoped<IUserRatingRepo, UserRatingRepo>();
        }

        private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ProjectVariables>(configuration.GetSection("ProjectVariables"));
            services.Configure<StripeApiKeys>(configuration.GetSection("StripeApiKeys"));
            services.Configure<ReturnUrls>(configuration.GetSection("ReturnUrls"));
            services.AddScoped<IJwtUtils, JwtUtils>();
            services.AddTransient<NotificationHubSocket>();
            services.AddScoped<LogApiRequestResponseFilter>();
            services.AddScoped<ISearchLogService, SearchLogService>();
            services.AddScoped<INotificationService, UserPackageService>();
            services.AddTransient<IFundTransferService, FundTransferService>();
            services.AddTransient<IPayPalGateWayService, PayPalGateWayService>();
        }
    }
}
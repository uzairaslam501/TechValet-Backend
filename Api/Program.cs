using ITValet.DependencyInjection;
using ITValet.Filters;
using ITValet.HelpingClasses;
using ITValet.JWTAuthentication;
using ITValet.JwtAuthorization;
using ITValet.Models;
using ITValet.NotificationHub;
using ITValet.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PayPalCheckoutSdk.Core;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configuration
var configuration = builder.Configuration;

// Configure PayPal SDK with sandbox/live credentials
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


//Stripe Dependency
StripeConfiguration.ApiKey = "sk_test_51LdJU1JGItIO6che6rYKSSzY2NEhOmMJtbUKUAxe1H95dl8oQPI6jWPmWHNBLfRsC8PdeqVi2TY1CFWjwsxWrlfp00D0eREv8W";


// Add services to the container.
builder.Services.AddCors(options => {
    options.AddPolicy("CORSPolicy", builder => builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials().SetIsOriginAllowed((hosts) => true));
});
builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IT Valet API Services",
        Version = "v1",
        Description = "IT Valet Swagger Services",
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Paste your Jwt token in value field to authorize your APIs. You can get token from post login APIs' response",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" //The name of the previously defined security scheme.
                }
            }, new List<string>()
        }
    });

});
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")), ServiceLifetime.Transient);
builder.Services.Configure<ProjectVariables>(builder.Configuration.GetSection("ProjectVariables"));
builder.Services.AddQuartzDependencyInjection();
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IUserEducationRepo, UserEducationRepo>();
builder.Services.AddScoped<IUserSocialProfileRepo, UserSocialProfileRepo>();
builder.Services.AddScoped<IUserExperienceRepo, UserExperienceRepo>();
builder.Services.AddScoped<IUserSkillRepo, UserSkillRepo>();
builder.Services.AddScoped<IUserTagRepo, UserTagRepo>();
builder.Services.AddScoped<IUserAvailableSlotRepo, UserAvailableSlotRepo>();
builder.Services.AddScoped<ISearchLogRepo, SearchLogRepo>();
builder.Services.AddScoped<IContactUsRepo, ContactUsRepo>();
builder.Services.AddScoped<IRequestServiceRepo, RequestServiceRepo>();
builder.Services.AddScoped<IJwtUtils, JwtUtils>();
builder.Services.AddScoped<IMessagesRepo, MessagesRepo>();
builder.Services.AddScoped<INotificationService, UserPackageService>();
builder.Services.AddScoped<IOfferDetailsRepo, OfferDetailsRepo>();
builder.Services.AddScoped<IOrderRepo, OrderRepo>();
builder.Services.AddTransient<NotificationHubSocket>();
builder.Services.AddTransient<IPayPalGateWayService, PayPalGateWayService>();
builder.Services.AddScoped<IOfferDetailsRepo, OfferDetailsRepo>();
builder.Services.AddScoped<IOrderRepo, OrderRepo>();
builder.Services.AddScoped<IOrderReasonRepo, OrderReasonRepo>();
builder.Services.AddScoped<INotificationRepo, NotificationRepo>();
builder.Services.AddScoped<IUserRatingRepo, UserRatingRepo>();
builder.Services.AddScoped<ISearchLogService, SearchLogService>();
builder.Services.AddScoped<LogApiRequestResponseFilter>();
builder.Services.AddHttpClient<PayPalHttpClient>(); // Replace with the actual registration method you're using.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(paypalHttpClient);
builder.Services.AddTransient<IFundTransferService, FundTransferService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
//{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PlaceInfo Services");
    });
//}

app.UseCors("CORSPolicy");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();

app.UseEndpoints(endpoints => {
    endpoints.MapHub<NotificationHubSocket>("/NotificationHubSocket");
    endpoints.MapControllers();
});
app.UseStaticFiles();
app.Run();
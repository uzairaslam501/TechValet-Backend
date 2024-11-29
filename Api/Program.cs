using ITValet.JwtAuthorization;
using ITValet.NotificationHub;
using ITValet.Utils.Extentions;

var builder = WebApplication.CreateBuilder(args);

// Add services and configurations
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.ConfigurePayPal(builder.Configuration);
builder.Services.ConfigureStripe(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure middleware
app.UseSwaggerDocumentation();
app.UseCors("CORSPolicy");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();
app.UseStaticFiles();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<NotificationHubSocket>("/NotificationHubSocket");
    endpoints.MapControllers();
});

app.Run();

using System.Text;
using Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

// Load NLog.config configuration file
var logger =
NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Retrieve environment variables or use default values
    string mySecret = Environment.GetEnvironmentVariable("Secret") ?? "none";
    string myIssuer = Environment.GetEnvironmentVariable("Issuer") ?? "none";

    // Add JWT authentication
    builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = myIssuer,
            ValidAudience = "http://localhost",
            IssuerSigningKey =
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(mySecret))
        };
    });

    // Add User Repository as a singleton service
    builder.Services.AddSingleton<UserRepository>();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    // Configure Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Use NLog as the logger
    builder.Host.UseNLog();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();

} // End of try block

catch (Exception ex)
{
    // Log and handle exceptions
    logger.Error(ex, "Stopped program because of exception");
    throw;
}

finally
{
    // Shutdown NLog to ensure all logs are flushed
    NLog.LogManager.Shutdown();
}
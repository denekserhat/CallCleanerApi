using Amazon.CloudWatchLogs;
using CallCleaner.Application.Services;
using CallCleaner.Application.Validations;
using CallCleaner.Core.Aspects;
using CallCleaner.Core.Utilities;
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.AwsCloudWatch;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

#region ControllerJsonConfigs
builder.Services.AddControllers(opt => opt.Filters.Add(typeof(ValidateModelAttribute)))
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    o.JsonSerializerOptions.WriteIndented = true;
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    o.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
                    o.JsonSerializerOptions.MaxDepth = 16;
                });
builder.Services.AddEndpointsApiExplorer();
#endregion
#region SwaggerConfig(SonraSilinebilir)
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
#endregion
#region SeriLogConfig
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.AmazonCloudWatch(
        logGroup: "CallCleanerLogs",
        logStreamPrefix: "MyApi",
        cloudWatchClient: new AmazonCloudWatchLogsClient(Amazon.RegionEndpoint.EUNorth1),
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
    )
    .CreateLogger();
#endregion

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddDbContext<DataContext>(option => option.UseNpgsql(builder.Configuration.GetConnectionString("PostgreConnection")));
builder.Services.AddMemoryCache();
builder.Host.UseSerilog();

#region identityRules
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;

    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<DataContext>()
    .AddErrorDescriber<CustomIdentityValidator>()
    .AddDefaultTokenProviders();
#endregion
#region AddServices
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<IBlockedCallsService, BlockedCallsService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INumberCheckService, NumberCheckService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
#endregion
#region AddCors
builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           //.AllowCredentials()
           .SetIsOriginAllowed(_ => true);
}));
#endregion


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "openapi/{documentName}.json";
    });
}

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapScalarApiReference();
app.MapControllers();
app.Run();
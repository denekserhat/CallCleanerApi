using CallCleaner.Api.ApiDocs;
using CallCleaner.Application.Services;
using CallCleaner.Application.Validations;
using CallCleaner.Core.Aspects;
using CallCleaner.Core.Utilities;
using CallCleaner.DataAccess;
using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<SwaggerDefaultValues>();
    //options.IncludeXmlComments(Path.ChangeExtension(typeof(Program).Assembly.Location, ".xml"));
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddDbContext<DataContext>(option => option.UseNpgsql(builder.Configuration.GetConnectionString("PostgreConnection")));

//Jwt security ayarlar�
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer("Admin", options =>
//    {
//        options.TokenValidationParameters = new()
//        {
//            ValidateAudience = true, //Olu�turulacak token de�erini kimlerin/hangi originlerin/sitelerin kullan�c� belirledi�imiz de�erdir. -> www.bilmemne.com
//            ValidateIssuer = true, //Olu�turulacak token de�erini kimin da��tt�n� ifade edece�imiz aland�r. -> www.myapi.com
//            ValidateLifetime = true, //Olu�turulan token de�erinin s�resini kontrol edecek olan do�rulamad�r.
//            ValidateIssuerSigningKey = true, //�retilecek token de�erinin uygulamam�za ait bir de�er oldu�unu ifade eden suciry key verisinin do�rulanmas�d�r.

//            ValidAudience = builder.Configuration["Token:Audience"],
//            ValidIssuer = builder.Configuration["Token:Issuer"],
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:SecurityKey"])),
//            LifetimeValidator = (notBefore, expires, securityToken, validationParameters) => expires != null ? expires > DateTime.UtcNow : false,

//            NameClaimType = ClaimTypes.Name //JWT �zerinde Name claimne kar��l�k gelen de�eri AppUser.Identity.Name propertysinden elde edebiliriz.
//        };
//    });

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

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ISmsService, SmsService>();


builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials()
           .SetIsOriginAllowed(_ => true);
}));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Sayfadaki �gelerin expand �zelli�ini kapat�r. Sayfa boyutunu d���rmek ve donmalar�
        c.DocExpansion(DocExpansion.List);
        c.SwaggerEndpoint($"/swagger/v1/swagger.json", "CallCleaner v1 API");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

using Amazon.DynamoDBv2;
using Amazon.Runtime;
using HvacMock.AdminApi.Repositories;
using HvacMock.AdminApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// camelCase JSON zodat de response de structuur van de klant volgt
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// DynamoDB Local verbinding (zelfde tabel als MockApi → gedeelde state)
AmazonDynamoDBConfig dynamoConfig = new AmazonDynamoDBConfig
{
    ServiceURL = builder.Configuration["DynamoDb:ServiceURL"]
};

AmazonDynamoDBClient dynamoClient = new AmazonDynamoDBClient(
    new BasicAWSCredentials("dummy", "dummy"),
    dynamoConfig
);

builder.Services.AddSingleton<IAmazonDynamoDB>(dynamoClient);

// JWT authenticatie configureren
string jwtSecret = builder.Configuration["Jwt:Secret"]!;
string jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
string jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Dependency injection
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IAdminDeviceService, AdminDeviceService>();

// CORS: open voor demo (UI kan op elke poort draaien)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Volgorde is belangrijk: eerst Authentication, dan Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Maak DynamoDB-tabel aan bij opstart als die nog niet bestaat
using (IServiceScope scope = app.Services.CreateScope())
{
    IDeviceRepository repo = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
    await repo.CreateTableIfNotExistsAsync();
}

app.Run();

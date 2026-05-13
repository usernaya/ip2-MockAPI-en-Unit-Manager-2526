using Amazon.DynamoDBv2;
using Amazon.Runtime;
using HvacMock.Application.Repositories;
using HvacMock.Application.Services;
using HvacMock.Infrastructure.Repositories;
using HvacMock.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddEndpointsApiExplorer();

// Swagger met Bearer token support zodat je in Swagger kan authenticeren
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Voer je JWT token in. Haal een token op via POST /v1/oidc/token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT authenticatie
string secretKey = builder.Configuration["Jwt:SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            // Geen issuer/audience validatie nodig voor deze eenvoudige implementatie
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

// DynamoDB Local verbinding
AmazonDynamoDBConfig config = new AmazonDynamoDBConfig
{
    ServiceURL = builder.Configuration["DynamoDb:ServiceURL"]
};

AmazonDynamoDBClient client = new AmazonDynamoDBClient(
    new BasicAWSCredentials("dummy", "dummy"),
    config
);

builder.Services.AddSingleton<IAmazonDynamoDB>(client);

// Dependency injection
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowUI");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Maak tabel aan en seed data bij opstart
using (IServiceScope scope = app.Services.CreateScope())
{
    DataSeeder seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    string seedPath = Path.Combine(app.Environment.ContentRootPath, "Infrastructure", "seed-devices.json");
    await seeder.SeedAsync(seedPath);
}

app.Run();

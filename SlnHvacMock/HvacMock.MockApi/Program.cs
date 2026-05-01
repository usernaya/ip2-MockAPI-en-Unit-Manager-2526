using Amazon.DynamoDBv2;
using Amazon.Runtime;
using HvacMock.MockApi.Infrastructure;
using HvacMock.MockApi.Repositories;
using HvacMock.MockApi.Services;
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
builder.Services.AddSwaggerGen();

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
app.UseAuthorization();
app.MapControllers();

// Maak tabel aan en seed data bij opstart
using (IServiceScope scope = app.Services.CreateScope())
{
    DataSeeder seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();

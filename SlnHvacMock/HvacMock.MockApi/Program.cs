using Amazon.DynamoDBv2;
using Amazon.Runtime;
using HvacMock.MockApi.Repositories;
using HvacMock.MockApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DynamoDB
AmazonDynamoDBConfig config = new AmazonDynamoDBConfig
{
    ServiceURL = builder.Configuration["DynamoDb:ServiceURL"]
};

AmazonDynamoDBClient client = new AmazonDynamoDBClient(
    new BasicAWSCredentials("dummy", "dummy"),
    config
);

builder.Services.AddSingleton<IAmazonDynamoDB>(client);

// dependency injection
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

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

app.Run();
using Amazon.DynamoDBv2;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dynamoServiceUrl = builder.Configuration["DynamoDb:ServiceURL"];

var dynamoConfig = new AmazonDynamoDBConfig
{
    ServiceURL = dynamoServiceUrl
};

var dynamoClient = new AmazonDynamoDBClient(
    new BasicAWSCredentials("dummy", "dummy"),
    dynamoConfig
);

builder.Services.AddSingleton<IAmazonDynamoDB>(dynamoClient);

var app = builder.Build();

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
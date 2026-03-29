using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace HvacMock.MockApi.Repositories
{
    public class DeviceRepository
    {
        private readonly IAmazonDynamoDB _db;
        private const string Table = "Devices";

        public DeviceRepository(IAmazonDynamoDB db)
        {
            _db = db;
        }

        public async Task<List<Dictionary<string, AttributeValue>>> GetAllAsync()
        {
            var response = await _db.ScanAsync(new ScanRequest
            {
                TableName = Table
            });

            return response.Items;
        }

        public async Task UpdateFieldAsync(string id, string field, string value)
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "deviceId", new AttributeValue { S = id } }
            };

            var request = new UpdateItemRequest
            {
                TableName = Table,
                Key = key,
                UpdateExpression = "SET #f = :v",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#f", field }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
            };

            if (field == "temperature")
            {
                request.ExpressionAttributeValues.Add(":v", new AttributeValue { N = value });
            }
            else
            {
                request.ExpressionAttributeValues.Add(":v", new AttributeValue { S = value });
            }

            await _db.UpdateItemAsync(request);
        }
    }
}
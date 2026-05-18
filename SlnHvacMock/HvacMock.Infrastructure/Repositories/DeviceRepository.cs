using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using HvacMock.Application.Repositories;
using HvacMock.Domain.Devices;
using System.Text.Json;

namespace HvacMock.Infrastructure.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly IAmazonDynamoDB _db;
        private const string TableName = "Devices";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public DeviceRepository(IAmazonDynamoDB db)
        {
            _db = db;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            ScanResponse response = await _db.ScanAsync(new ScanRequest
            {
                TableName = TableName
            });

            List<Device> devices = new List<Device>();

            foreach (Dictionary<string, AttributeValue> item in response.Items)
            {
                Device? device = DeserializeItem(item);
                if (device != null)
                {
                    devices.Add(device);
                }
            }

            return devices;
        }

        public async Task<Device?> GetByIdAsync(string deviceId)
        {
            GetItemResponse response = await _db.GetItemAsync(new GetItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "deviceId", new AttributeValue { S = deviceId } }
                }
            });

            if (response.Item == null || response.Item.Count == 0)
            {
                return null;
            }

            return DeserializeItem(response.Item);
        }

        public async Task SaveAsync(Device device)
        {
            string json = JsonSerializer.Serialize(device, JsonOptions);
            Dictionary<string, AttributeValue> item = Document.FromJson(json).ToAttributeMap();

            item["deviceId"] = new AttributeValue { S = device.Id };

            await _db.PutItemAsync(new PutItemRequest
            {
                TableName = TableName,
                Item = item
            });
        }

        public async Task DeleteAsync(string deviceId)
        {
            await _db.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "deviceId", new AttributeValue { S = deviceId } }
                }
            });
        }

        public async Task CreateTableIfNotExistsAsync()
        {
            ListTablesResponse tables = await _db.ListTablesAsync();

            if (tables.TableNames.Contains(TableName))
            {
                return;
            }

            await _db.CreateTableAsync(new CreateTableRequest
            {
                TableName = TableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "deviceId", KeyType = KeyType.HASH }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "deviceId", AttributeType = ScalarAttributeType.S }
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            });

            bool isActive = false;
            while (!isActive)
            {
                await Task.Delay(500);
                DescribeTableResponse describe = await _db.DescribeTableAsync(TableName);
                isActive = describe.Table.TableStatus == TableStatus.ACTIVE;
            }
        }

        private static Device? DeserializeItem(Dictionary<string, AttributeValue> item)
        {
            if (item.TryGetValue("data", out AttributeValue? dataAttr) && !string.IsNullOrEmpty(dataAttr.S))
            {
                return JsonSerializer.Deserialize<Device>(dataAttr.S, JsonOptions);
            }

            string json = Document.FromAttributeMap(item).ToJson();
            return JsonSerializer.Deserialize<Device>(json, JsonOptions);
        }
    }
}

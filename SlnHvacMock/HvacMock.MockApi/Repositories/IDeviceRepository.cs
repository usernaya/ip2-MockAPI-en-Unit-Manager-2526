using Amazon.DynamoDBv2.Model;

namespace HvacMock.MockApi.Repositories
{
    public interface IDeviceRepository
    {
        Task<List<Dictionary<string, AttributeValue>>> GetAllAsync();
        Task<Dictionary<string, AttributeValue>?> GetByIdAsync(string id);
        Task UpdateFieldAsync(string id, string field, string value);
    }
}
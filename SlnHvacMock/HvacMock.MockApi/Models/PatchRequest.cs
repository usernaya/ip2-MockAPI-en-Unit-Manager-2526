using System.Text.Json;

namespace HvacMock.MockApi.Models
{
    public class PatchRequest
    {
        // JsonElement zodat zowel strings ("cooling"), getallen (23.5) als booleans (true) werken
        public JsonElement Value { get; set; }
    }
}
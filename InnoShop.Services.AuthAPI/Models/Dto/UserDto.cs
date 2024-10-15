using System.Text.Json.Serialization;

namespace InnoShop.Services.AuthAPI.Models.Dto
{
    public class UserDto
    {
        public string Id { get; set; } 
        public string Email { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PhoneNumber { get; set; }
    }
}

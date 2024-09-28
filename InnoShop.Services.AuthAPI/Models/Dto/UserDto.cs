namespace InnoShop.Services.AuthAPI.Models.Dto
{
    public class UserDto
    {
        public string ID { get; set; } // string because it will be Guid in .net identity
        public string Email { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
    }
}

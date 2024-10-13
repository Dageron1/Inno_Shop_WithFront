namespace InnoShop.Services.AuthAPI.Models.Dto
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}

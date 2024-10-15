using System.ComponentModel.DataAnnotations;

namespace InnoShop.Services.ProductAPI.Models
{
    public class Product
    {
        // FluentAPI использовать вместо атрибутов 
        [Key]
        public int ProductId { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }

        [Range(1, 1000)]
        public double? Price { get; set; }

        public string CreatedByUserId { get; set; } = "";
    }
}

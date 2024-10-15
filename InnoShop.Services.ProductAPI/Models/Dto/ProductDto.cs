namespace InnoShop.Services.ProductAPI.Models.Dto;

// Использовать всегда наиболее закрытый и приватный тип
public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CategoryName { get; set; }
    public string ImageUrl { get; set; }
    public double Price { get; set; }

    public string CreatedByUserId { get; set; } = "";
}

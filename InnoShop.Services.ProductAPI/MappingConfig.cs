using AutoMapper;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Models.Dto;

namespace InnoShop.Services.ProductAPI
{
    // посмотреть подход с конструтором и валидация всех свойств в конструкторе
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<ProductDto, Product>();
                config.CreateMap<Product, ProductDto>();
            });
            return mappingConfig;
        }
    }
}

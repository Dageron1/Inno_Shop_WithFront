using AutoMapper;
using Azure;
using FluentAssertions;
using InnoShop.Services.ProductAPI.Controllers;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Services;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static NUnit.Framework.Constraints.Tolerance;

namespace InnoShop.Services.ProductAPI.UnitTests.Services
{
    [TestFixture]
    public class ProductServiceTests
    {
        private ProductDbContext _dbContext;
        private IMapper _mapper;
        private ProductService _productService;
        // лучше сделать мок дб контекста на эксепшен
        // и проверка вызова метода

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _dbContext = new ProductDbContext(options);
            
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Product, ProductDto>();
                cfg.CreateMap<ProductDto, Product>();
            });
            _mapper = configuration.CreateMapper();

            _productService = new ProductService(_dbContext, _mapper);
        }

        private void ThrowDbUpdateException(object? sender, SavingChangesEventArgs e)
        {
            throw new DbUpdateException("Simulated database update exception.");
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnEmptyCollection_WhenNoProductsExist()
        {
            // Act
            var result = await _productService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty("because there are no products in the database");
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnAllProductsDto_WhenProductsExist()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Product 1", Price = 9.99 },
                new Product { ProductId = 2, Name = "Product 2", Price = 19.99 }
            };

            _dbContext.Products.AddRange(products);
            await _dbContext.SaveChangesAsync();

            var expectedResult = new List<ProductDto>
            {
                new ProductDto { ProductId = 1, Name = "Product 1", Price = 9.99 },
                new ProductDto { ProductId = 2, Name = "Product 2", Price = 19.99 }
            };

            // Act
            var result = await _productService.GetAllAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task GetById_ShouldReturnNull_WhenProductDoesNotExist()
        {
            // Act
            var result = await _productService.GetById(1);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetById_ShouldReturnProductDto_WhenProductExists()
        {
            // Arrange
            var product = new Product {
                ProductId = 1,
                Name = "Test Product",
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var expectedProductDto = new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name
            };

            // Act
            var result = await _productService.GetById(1);

            // Assert
            result.Should().BeEquivalentTo(expectedProductDto, options => options.ExcludingMissingMembers());
        }

        [Test]
        public async Task GetByName_ShouldReturnNull_WhenNameIsNullOrEmptyOrWhitespace()
        {
            // Act
            var resultWithNull = await _productService.GetByName(null);
            var resultWithEmpty = await _productService.GetByName(string.Empty);
            var resultWithWhitespace = await _productService.GetByName("   ");

            // Assert
            resultWithNull.Should().BeNull("because name is null and the method should return null");
            resultWithEmpty.Should().BeNull("because name is an empty string and the method should return null");
            resultWithWhitespace.Should().BeNull("because name is only whitespace and the method should return null");
        }

        [Test]
        public async Task GetByName_ShouldReturnNull_WhenProductDoesNotExist()
        {
            // Act
            var result = await _productService.GetByName("NonExistingProduct");

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetByName_ShouldReturnProductDto_WhenProductExists()
        {
            // Arrange
            var product = new Product {
                ProductId = 1,
                Name = "Test Product",
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var expectedProductDto = new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name
            };

            // Act
            var result = await _productService.GetByName("Test Product");

            // Assert
            result.Should().BeEquivalentTo(expectedProductDto, options => options.ExcludingMissingMembers());
        }

        [Test]
        public async Task GetByCategory_ShouldReturnEmptyCollection_WhenCategoryIsNullOrWhiteSpace()
        {
            // Act
            var resultWithNull = await _productService.GetByCategory(null);
            var resultWithEmpty = await _productService.GetByCategory(string.Empty);
            var resultWithWhitespace = await _productService.GetByCategory("   ");

            // Assert
            resultWithNull.Should().BeEmpty("because the category is null, and the method should return an empty collection");
            resultWithEmpty.Should().BeEmpty("because the category is empty, and the method should return an empty collection");
            resultWithWhitespace.Should().BeEmpty("because the category is whitespace, and the method should return an empty collection");
        }

        [Test]
        public async Task GetByCategory_ShouldReturnEmptyCollection_WhenNoProductsInCategory()
        {
            // Arrange
            var category = "NonExistingCategory";

            // Act
            var result = await _productService.GetByCategory(category);

            // Assert
            result.Should().BeEmpty("because there are no products in the specified category");
        }

        [Test]
        public async Task GetByCategory_ShouldReturnProductsDto_WhenProductsExistInCategory()
        {
            // Arrange
            var category = "Electronics";
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Product 1", CategoryName = category, Price = 100 },
                new Product { ProductId = 2, Name = "Product 2", CategoryName = category, Price = 200 }
            };

            _dbContext.Products.AddRange(products);
            await _dbContext.SaveChangesAsync();

            var expectedProductsDto = new List<ProductDto>
            {
                new ProductDto { ProductId = 1, Name = "Product 1", CategoryName = category, Price = 100 },
                new ProductDto { ProductId = 2, Name = "Product 2", CategoryName = category, Price = 200 }
            };

            // Act
            var result = await _productService.GetByCategory(category);

            // Assert
            result.Should().BeEquivalentTo(expectedProductsDto, options => options.ExcludingMissingMembers());
        }

        [Test]
        public async Task CreateAsync_ShouldAddProductToDatabase_AndReturnProductDtoWithId()
        {
            // Arrange
            var productDto = new ProductDto 
            {
                ProductId = 5, // not correct id
                Name = "New Product", 
                Price = 10.99 
            };

            // Act
            var result = await _productService.CreateAsync(productDto);

            // Assert
            var productInDb = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == result.ProductId);

            result.Should().BeEquivalentTo(productInDb);
        }

        [Test]
        public async Task CreateAsync_ShouldNotAddProduct_WhenDbUpdateExceptionOccurs()
        {
            // Arrange
            var productDto = new ProductDto
            {
                ProductId = -5,
                Name = "New Product",
                Price = 10.99
            };

            _dbContext.SavingChanges += ThrowDbUpdateException;

            // Act
            Func<Task> act = async () => await _productService.CreateAsync(productDto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Simulated database update exception.");
        }

        [Test]
        public async Task CreateAsync_ShouldSetProductIdToZero_WhenCreatingNewProduct()
        {
            // Arrange
            var productDto = new ProductDto
            {
                ProductId = 10,
                Name = "Test Product",
                Price = 50.00
            };

            // Act
            var result = await _productService.CreateAsync(productDto);

            // Assert
            productDto.ProductId.Should().Be(0, "because the ProductId should be reset to zero before creating a new product");

            result.ProductId.Should().BeGreaterThan(0, "because the product ID should be generated by the database");
        }

        [Test]
        public async Task UpdateAsync_ShouldSuccessfullyUpdate_WhenProductExist()
        {
            // Arrange
            var product = new Product { 
                ProductId = 1, 
                Name = "Old Name",         
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var expectedResult = new ProductDto { 
                ProductId = 1, 
                Name = "Updated Name",
            };

            // Act
            var result = await _productService.UpdateAsync(expectedResult);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            var updatedProduct = await _dbContext.Products.FindAsync(1);
            updatedProduct.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task UpdateAsync_ShouldNotUpdateProduct_WhenDbUpdateExceptionOccurs()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Old Name",
                Price = 50,
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            _dbContext.SavingChanges += ThrowDbUpdateException;

            var updatedProduct = new ProductDto
            {
                ProductId = 1,
                Name = "Updated Name",
                Price = 100,
            };

            // Act
            Func<Task> act = async () => await _productService.UpdateAsync(updatedProduct);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Simulated database update exception.");

            var notUpdatedProduct = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == 1);
            notUpdatedProduct.Should().BeEquivalentTo(product);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnEmptyProductDto_WhenProductDoesNotExist()
        {
            // Arrange
            var productDto = new ProductDto { ProductId = 99, Name = "Non-existing product", };

            // Act
            var result = await _productService.UpdateAsync(productDto);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveProduct_WhenProductExists()
        {
            // Arrange
            var product = new Product { ProductId = 1, Name = "Test Product" };
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var productDto = new ProductDto { ProductId = 1, Name = "Test Product" };

            // Act
            await _productService.DeleteAsync(productDto.ProductId);

            // Assert
            var deletedProduct = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == 1);
            deletedProduct.Should().BeNull("because the product should have been deleted from the database.");
        }

        [Test]
        public async Task DeleteAsync_ShouldThrowAnException_WhenDbUpdateExceptionOccurs()
        {
            // Arrange
            var product = new Product { ProductId = 1, Name = "Test Product" };
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var productDto = new ProductDto { ProductId = 1, Name = "Test Product" };

            _dbContext.SavingChanges += ThrowDbUpdateException;

            // Act
            Func<Task> act = async () => await _productService.DeleteAsync(productDto.ProductId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Simulated database update exception.");
        }
    }
}

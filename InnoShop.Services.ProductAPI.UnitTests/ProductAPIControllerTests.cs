using AutoMapper;
using Azure;
using FluentAssertions;
using InnoShop.Services.ProductAPI.Controllers;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Services;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace InnoShop.Services.ProductAPI.UnitTests
{
    [TestFixture]
    public class ProductAPIControllerTests
    {
        private IMapper _mapper;
        private Mock<IProductService> _productServiceMock;
        private ProductAPIController _productAPIController;

        [SetUp]
        public void SetUp()
        {

            // Настройка AutoMapper (для простоты, используем реальные маппинги)
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Product, ProductDto>();
                cfg.CreateMap<ProductDto, Product>();
            });
            _mapper = configuration.CreateMapper();

            // Создание сервиса
            _productServiceMock = new Mock<IProductService>();
            _productAPIController = new ProductAPIController(_productServiceMock.Object);
        }

        [Test]
        public async Task Get_ShouldReturnOkWithResponseDto_WhenProductsExist()
        {
            // Arrange
            var productsDto = new ProductDto[]
            {
                new ProductDto
                {
                    ProductId = 1,
                    Name = "Product 1",
                },
                new ProductDto
                {
                    ProductId = 2,
                    Name = "Product 2",
                }
            };

            _productServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(productsDto).Verifiable(Times.Once);

            var responseDto = new ResponseDto
            {
                IsSuccess = true,
                Message = "Products retrieved successfully.",
                Result = productsDto,
            };

            // Act
            var result = await _productAPIController.Get();

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(responseDto);

            _productServiceMock.Verify();
        }

        [Test]
        public async Task Get_ShouldReturnNotFoundWithRespnseDto_WhenNoProductsExist()
        {
            // Arrange
            var productsDto = new ProductDto[0];

            _productServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(productsDto).Verifiable(Times.Once);

            var responseDto = new ResponseDto
            {
                IsSuccess = false,
                Message = "No products found.",
                Result = null,
            };

            // Act
            var result = await _productAPIController.Get();

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(responseDto);

            _productServiceMock.Verify();
        }

        [Test]
        public async Task GetById_ShouldReturnNotFoundWithResponseDto_WhenProductDoesNotExist()
        {
            // Arrange
            int invalidProductId = 9939499;

            _productServiceMock.Setup(x => x.GetById(invalidProductId)).ReturnsAsync((ProductDto)null).Verifiable(Times.Once);

            var expectedResponse = new ResponseDto
            {
                IsSuccess = false,
                Message = "Product not found.",
                Result = null,
            };

            // Act
            var result = await _productAPIController.Get(invalidProductId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResponse);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task GetById_ShouldReturnOkWithResponseDto_WhenProductExists()
        {
            // Arrange
            var productDto = new ProductDto 
            {
                ProductId = 1,
                Name = "Product 1",
            };

            _productServiceMock.Setup(x => x.GetById(1)).ReturnsAsync(productDto).Verifiable(Times.Once);

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "Product retrieved successfully.",
                Result = productDto,
            };

            // Act
            var result = await _productAPIController.Get(1);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task GetByName_ShouldReturnNotFoundWithResponseDto_WhenProductNotFound()
        {
            // Arrange
            var productName = "Non-existing Product";

            _productServiceMock.Setup(x => x.GetByName(productName)).ReturnsAsync((ProductDto)null).Verifiable(Times.Once);

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Product not found.",
                Result = null,
            };

            // Act
            var result = await _productAPIController.GetByName(productName);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task GetByName_ShouldReturnOkWithResponseDto_WhenProductExists()
        {
            // Arrange
            var productName = "Test Product";
            var productDto = new ProductDto { ProductId = 1, Name = productName };

            _productServiceMock.Setup(x => x.GetByName(productName)).ReturnsAsync(productDto).Verifiable(Times.Once);

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "Product retrieved successfully.",
                Result = productDto,
            };

            // Act
            var result = await _productAPIController.GetByName(productName);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task GetByCategory_ShouldReturnOkWithResponseDto_WhenProductsExist()
        {
            // Arrange
            var categoryName = "Phone";
            var productsDto = new ProductDto[]
            {
                new ProductDto { ProductId = 1, Name = "Test product 1", CategoryName = categoryName },
                new ProductDto { ProductId = 2, Name = "Test product 2", CategoryName = categoryName },
            };

            _productServiceMock.Setup(x => x.GetByCategory(categoryName)).ReturnsAsync(productsDto).Verifiable(Times.Once);

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "Product retrieved successfully.",
                Result = productsDto,
            };

            // Act
            var result = await _productAPIController.GetByCategory(categoryName);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task GetByCategory_ShouldReturnNotFoundWithResponseDto_WhenNoProductsExist()
        {
            // Arrange

            var categoryName = "Non-existing Category";

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Product not found.",
                Result = null,
            };

            _productServiceMock.Setup(x => x.GetByCategory(categoryName)).ReturnsAsync(Array.Empty<ProductDto>()).Verifiable(Times.Once);

            // Act
            var result = await _productAPIController.GetByCategory("Non-existing Category");

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task Post_ShouldReturnOkWithResponseDto_WhenProductIsValid()
        {
            // Arrange
            var productDto = new ProductDto { Name = "New Product", CreatedByUserId = "test-user" };

            var createdProductDto = new ProductDto 
            { 
                ProductId = 1, 
                Name = "New Product", 
                CreatedByUserId = "test-user" 
            };

            _productServiceMock.Setup(x => x.CreateAsync(productDto)).ReturnsAsync(createdProductDto).Verifiable(Times.Once);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user")
            }, "mock"));

            _productAPIController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "Product created successfully.",
                Result = createdProductDto,
            };

            // Act
            var result = await _productAPIController.Post(productDto);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        //[Test]
        //public async Task Post_ShouldReturnStatusCode500WithResponseDto_WhenExceptionOccurs()
        //{
        //    // Arrange
        //    var productDto = new ProductDto { Name = "New Product" };

        //    // Мокируем выброс исключения в сервисе
        //    _productServiceMock.Setup(x => x.CreateAsync(It.IsAny<ProductDto>())).ThrowsAsync(new Exception("An error occurred while creating the product."));

        //    // Мокируем получение userId из Claims
        //    var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, "test-user")
        //    }, "mock"));

        //    _productAPIController.ControllerContext = new ControllerContext()
        //    {
        //        HttpContext = new DefaultHttpContext() { User = user }
        //    };

        //    var expectedResult = new ResponseDto
        //    {
        //        IsSuccess = false,
        //        Message = "An error occurred while creating the product.",
        //        Result = null,
        //    };

        //    // Act
        //    var result = await _productAPIController.Post(productDto);

        //    // Assert
        //    var objectResult = result.Result as ObjectResult;
        //    objectResult.Should().NotBeNull();
        //    objectResult.StatusCode.Should().Be(500);

        //    var response = objectResult.Value as ResponseDto;

        //    response.Should().BeEquivalentTo(expectedResult);
        //    // Проверяем, что метод CreateAsync был вызван ровно один раз
        //    _productServiceMock.Verify(x => x.CreateAsync(It.IsAny<ProductDto>()), Times.Once);
        //}

        [Test]
        public async Task Post_ShouldSetCreatedByUserId_FromUserClaims()
        {
            // Arrange
            var productDto = new ProductDto { Name = "New Product" };
            var createdProductDto = new ProductDto { ProductId = 1, Name = "New Product", CreatedByUserId = "test-user" };

            _productServiceMock.Setup(x => x.CreateAsync(It.IsAny<ProductDto>())).ReturnsAsync(createdProductDto);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                 new Claim(ClaimTypes.NameIdentifier, "test-user")
            }, "mock"));

            _productAPIController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Act
            var result = await _productAPIController.Post(productDto);

            // Assert
            _productServiceMock.Verify(x => x.CreateAsync(It.Is<ProductDto>(p => p.CreatedByUserId == "test-user")), Times.Once);
        }

        [Test]
        public async Task Put_ShouldReturnBadRequestWithResponseDto_WhenIdInUrlDoesNotMatchIdInModel()
        {
            // Arrange
            int urlId = 1;
            var productDto = new ProductDto
            {
                ProductId = 2,
                Name = "Test Product",
                Description = "Test Description",
                CategoryName = "Test Category",
                Price = 100.0,
                ImageUrl = "https://example.com/image.jpg"
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "ID in URL does not match ID in the model."
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                 new Claim(ClaimTypes.NameIdentifier, "test-user")
            }, "mock"));

            _productAPIController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Act
            var result = await _productAPIController.Put(urlId, productDto);

            // Assert
            var objectResult = result.Result as BadRequestObjectResult;
            objectResult.StatusCode.Should().Be(400);
            var response = objectResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task Put_ShouldReturnNotFoundWithResponseDto_WhenProductDoesNotExist()
        {
            var urlId = 1;
            // Arrange
            var productDto = new ProductDto { ProductId = 1, Name = "Non-Existent Product" };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                 new Claim(ClaimTypes.NameIdentifier, "test-user")
            }, "mock"));

            _productAPIController.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            _productServiceMock.Setup(x => x.GetById(1)).ReturnsAsync((ProductDto)null).Verifiable(Times.Once);

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Product not found.",
                Result = null,
            };

            // Act
            var result = await _productAPIController.Put(urlId, productDto);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task Put_ShouldReturnOkWithResponseDto_WhenProductExistsAndUserIsAdmin()
        {
            // Arrange
            var urlId = 1;
            var productDto = new ProductDto { ProductId = 1, Name = "Updated Product" };
            var productFromDbDto = new ProductDto { ProductId = 1, Name = "Old Product", CreatedByUserId = "user-id" };

            _productServiceMock.Setup(x => x.GetById(productDto.ProductId)).ReturnsAsync(productFromDbDto).Verifiable(Times.Once);

            _productServiceMock.Setup(x => x.UpdateAsync(productDto)).ReturnsAsync(productDto).Verifiable(Times.Once);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "ADMIN") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "The product was updated successfully.",
                Result = productDto,
            };

            // Act
            var result = await _productAPIController.Put(urlId, productDto);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify(x => x.GetById(1));
            _productServiceMock.Verify(x => x.UpdateAsync(productDto));
        }


        [Test]
        public async Task Put_ShouldReturnOkWithResponseDto_WhenProductExistAndUserIsOwner()
        {
            // Arrange
            var urlId = 1;
            var userId = "test-user-id";

            var productOld = new ProductDto
            {
                ProductId = 1,
                Name = "Old Product",
                CreatedByUserId = "test-user-id"
            };

            var productNew = new ProductDto
            {
                ProductId = 1,
                Name = "Updated Product",
                CreatedByUserId = "test-user-id"
            };

            _productServiceMock.Setup(x => x.GetById(1)).ReturnsAsync(productOld).Verifiable(Times.Once);
            _productServiceMock.Setup(x => x.UpdateAsync(productNew)).ReturnsAsync(productNew).Verifiable(Times.Once);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "The product was updated successfully.",
                Result = productNew,
            };

            // Act
            var result = await _productAPIController.Put(urlId, productNew);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify(x => x.GetById(1), Times.Once);
            _productServiceMock.Verify(x => x.UpdateAsync(productNew), Times.Once);
        }

        [Test]
        public async Task Put_ShoudReturnStatusCode400WithResponseDto_WhenUserIsOwnerAndUpdateServiceReturnsNull()
        {
            // Arrange
            var urlId = 1;
            var userId = "test-user-id";

            var productOld = new ProductDto
            {
                ProductId = 1,
                Name = "Old Product",
                CreatedByUserId = "test-user-id"
            };

            var productNew = new ProductDto
            {
                ProductId = 1,
                Name = "Updated Product",
                CreatedByUserId = "test-user-id"
            };

            _productServiceMock.Setup(x => x.GetById(1)).ReturnsAsync(productOld).Verifiable(Times.Once);
            _productServiceMock.Setup(x => x.UpdateAsync(productNew)).ReturnsAsync((ProductDto)null).Verifiable(Times.Once);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to update the product.",
                Result = productNew,
            };

            // Act
            var result = await _productAPIController.Put(urlId, productNew);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(400);

            var response = objectResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify(x => x.GetById(1), Times.Once);
            _productServiceMock.Verify(x => x.UpdateAsync(productNew), Times.Once);
        }

        [Test]
        public async Task Put_ShoudReturnStatusCode400WithResponseDto_WhenUserIsAdminAndServiceReturnsNull()
        {
            // Arrange
            var urlId = 1;
            var productOld = new ProductDto
            {
                ProductId = 1,
                Name = "Old Product",
                CreatedByUserId = "test-user-id"
            };

            var productNew = new ProductDto
            {
                ProductId = 1,
                Name = "Updated Product",
                CreatedByUserId = "test-user-id"
            };

            _productServiceMock.Setup(x => x.GetById(1)).ReturnsAsync(productOld);
            _productServiceMock.Setup(x => x.UpdateAsync(productNew)).ReturnsAsync((ProductDto)null);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "ADMIN") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to update the product.",
                Result = productNew,
            };

            // Act
            var result = await _productAPIController.Put(urlId, productNew);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(400);

            var response = objectResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify(x => x.GetById(1), Times.Once);
            _productServiceMock.Verify(x => x.UpdateAsync(productNew), Times.Once);
        }

        [Test]
        public async Task Put_ShouldReturnStatusCode403WithResponseDto_WhenUserIsNotAdminOrOwner()
        {
            // Arrange
            var urlId = 1;
            var userId = "test-user-id";
            var productDto = new ProductDto { ProductId = 1, Name = "Updated Product", CreatedByUserId = "different-user-id" };

            _productServiceMock.Setup(x => x.GetById(productDto.ProductId)).ReturnsAsync(productDto).Verifiable(Times.Once);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Access denied. You are not authorized to update this product.",
                Result = null,
            };

            // Act
            var result = await _productAPIController.Put(urlId, productDto);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(403);

            var response = objectResult.Value as ResponseDto;

            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task Delete_ShouldReturnNotFoundWithResponseDto_ProductNotFound()
        {
            // Arrange
            int productId = 1;
            var userId = "test-user-id";
            _productServiceMock.Setup(p => p.GetById(productId))
                .ReturnsAsync((ProductDto)null).Verifiable(Times.Once);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "Product not found."
            };

            // Act
            var result = await _productAPIController.Delete(productId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            var response = notFoundResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task Delete_ShouldReturnForbiddenWithResponseDto_UserNotAdminAndNotCreator()
        {
            // Arrange
            int productId = 1;
            string userId = "test-user-id";

            var expectedResult = new ResponseDto
            {
                IsSuccess = false,
                Message = "You are not the creator of this product. " +
                        "Only the creator or administrator can delete this product."
            };

            var product = new ProductDto 
            { 
                ProductId = productId, 
                CreatedByUserId = "different-user-id" 
            };
            _productServiceMock.Setup(p => p.GetById(productId))
                .ReturnsAsync(product).Verifiable(Times.Once);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _productAPIController.Delete(productId);

            // Assert
            var forbiddenResult = result.Result as ObjectResult;
            forbiddenResult.StatusCode.Should().Be(403);
            var response = forbiddenResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResult);
            _productServiceMock.Verify();
        }

        [Test]
        public async Task Delete_ShouldReturnOkWithResponseDto_WhenUserIsAdmin()
        {
            // Arrange
            int productId = 1;
            string role = "ADMIN";

            var expectedResult = new ResponseDto
            {
                IsSuccess = true,
                Message = "Product deleted successfully."
            };

            var product = new ProductDto { ProductId = productId, CreatedByUserId = "different-user-id" };
            _productServiceMock.Setup(p => p.GetById(productId))
                .ReturnsAsync(product);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _productAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _productServiceMock.Setup(p => p.DeleteAsync(productId)).Returns(Task.CompletedTask);

            // Act
            var result = await _productAPIController.Delete(productId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.StatusCode.Should().Be(200);
            var response = okResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResult);

            _productServiceMock.Verify(p => p.GetById(productId), Times.Once);
            _productServiceMock.Verify(p => p.DeleteAsync(productId), Times.Once);
        }

        //[Test]
        //public async Task Delete_ShouldReturnInternalServerErrorWithResponseDto_WhenExceptionThrown()
        //{
        //    // Arrange
        //    int productId = 1;
        //    string userId = "admin-user-id";

        //    var product = new ProductDto { ProductId = productId, CreatedByUserId = "admin-user-id" };
        //    _productServiceMock.Setup(p => p.GetById(productId))
        //        .ReturnsAsync(product);

        //    var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        //    var identity = new ClaimsIdentity(claims, "TestAuthType");
        //    var claimsPrincipal = new ClaimsPrincipal(identity);

        //    _productAPIController.ControllerContext = new ControllerContext
        //    {
        //        HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        //    };

        //    _productServiceMock.Setup(p => p.DeleteAsync(productId)).ThrowsAsync(new DbUpdateException("Simulated exception"));

        //    // Act
        //    var result = await _productAPIController.Delete(productId);

        //    // Assert
        //    var internalServerErrorResult = result.Result as ObjectResult;
        //    internalServerErrorResult.StatusCode.Should().Be(500);
        //    var response = internalServerErrorResult.Value as ResponseDto;
        //    response.IsSuccess.Should().BeFalse();
        //    response.Message.Should().Contain("Simulated exception");
        //}
    }
}

using FluentAssertions;
using InnoShop.Services.ProductAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Models.Dto;
using Microsoft.AspNetCore.Hosting;
using Moq;
using InnoShop.Services.AuthAPI.Models;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using InnoShop.Services.ProductAPI;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace InnoShop.Services.ProductAPI.IntegrationTests
{
    [TestFixture]
    public class ProductAPIControllerTest
    {
        
        private WebApplicationFactory<Program> _productApiFactory;
        private HttpClient _productApiClient;
        private ProductDbContext _productDbContext;

        [SetUp]
        public async Task SetUp()
        {
            _productApiFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");

                    builder.ConfigureServices(services =>
                    {
                        services.AddDbContext<ProductDbContext>(options =>
                        {
                            options.UseSqlServer();
                        });

                        var serviceProvider = services.BuildServiceProvider();
                        using (var scope = serviceProvider.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                           
                            dbContext.Database.EnsureDeleted();
                            dbContext.Database.Migrate();
                        }
                    });
                });

            _productApiClient = _productApiFactory.CreateClient();   
        }

        [TearDown]
        public async Task CleanUp()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                _productDbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                _productDbContext.Products.RemoveRange(_productDbContext.Products);
                await _productDbContext.SaveChangesAsync();
            }
            _productApiFactory.Dispose();
            _productApiClient.Dispose();
        }

        public async Task<string> GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (tokenHandler.CanReadToken(token))
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub);

                return userIdClaim?.Value;
            }

            return null;
        }

        public async Task<string> GetJwtTokenForAdminUserAsync()
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("Add your secret here and change in the future.");

            var adminUser = new ApplicationUser
            {
                UserName = "Admin@gmail.com",
                Name = "Admin",
                Email = "Admin@gmail.com",
                EmailConfirmed = true,
            };

            var claimsList = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, adminUser.Email),
                new Claim(JwtRegisteredClaimNames.Sub, adminUser.Id),
                new Claim(JwtRegisteredClaimNames.Name, adminUser.UserName),
                new Claim(ClaimTypes.Role, "ADMIN"),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = "inno-client",
                Issuer = "inno-shop-api",
                Subject = new ClaimsIdentity(claimsList),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<string> GetJwtTokenForRegularUserAsync()
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("Add your secret here and change in the future.");

            var customerUser = new ApplicationUser
            {
                UserName = "Customer@gmail.com",
                Name = "Customer",
                Email = "Customer@gmail.com",
                EmailConfirmed = true,
            };

            var claimsList = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, customerUser.Email),
                new Claim(JwtRegisteredClaimNames.Sub, customerUser.Id),
                new Claim(JwtRegisteredClaimNames.Name, customerUser.UserName),
                new Claim(ClaimTypes.Role, "USER"),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = "inno-client",
                Issuer = "inno-shop-api",
                Subject = new ClaimsIdentity(claimsList),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Test]
        public async Task Get_ShouldReturnOkWithResponseDtoWithProducts_WhenProductsExist()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                var client = _productApiClient;

                var product = new Product
                {
                    Name = "Test Product",
                    Price = 100,
                    Description = "This is a test product",
                    CategoryName = "Phone",
                    ImageUrl = "https://placeholder.co/600x400",
                };
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();


                // Act
                var response = await _productApiClient.GetAsync("/api/products");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("Products retrieved successfully.");

                var products = ((JArray)responseDto.Result).ToObject<List<ProductDto>>();
                products.Should().HaveCount(1);
                products.FirstOrDefault().Should().BeEquivalentTo(product);
            }
        }

        [Test]
        public async Task Get_ShouldReturnNotFoundWithResponseDto_WhenNoProductsExist()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                dbContext.Products.RemoveRange(dbContext.Products);
                await dbContext.SaveChangesAsync();
            }
            // Act
            var response = await _productApiClient.GetAsync("/api/products");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Message.Should().Be("No products found.");
        }

        [Test]
        public async Task GetProductById_ShouldReturnOkWithResponseDto_WhenProductExists()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var newProduct = new Product
                {
                    Name = "Test Product",
                    Price = 100,
                    Description = "This is a test product",
                    CategoryName = "Phone",
                    ImageUrl = "https://placeholder.co/600x400",
                };
                dbContext.Products.Add(newProduct);
                await dbContext.SaveChangesAsync();

                // Act
                var response = await _productApiClient.GetAsync($"/api/products/{newProduct.ProductId}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("Product retrieved successfully.");

                var product = ((JObject)responseDto.Result).ToObject<ProductDto>();

                product.Should().NotBeNull();
                product.Should().BeEquivalentTo(newProduct);
            }
        }

        [Test]
        public async Task GetProductById_ShouldReturnNotFoundWithResponseDto_WhenProductDoesNotExist()
        {
            // Act
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                dbContext.Products.RemoveRange(dbContext.Products);
                await dbContext.SaveChangesAsync();
            }

            var nonExistentProductId = 999;
            var response = await _productApiClient.GetAsync($"/api/products/{nonExistentProductId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Message.Should().Be("Product not found.");
        }

        [Test]
        public async Task GetByName_ShouldReturnOkWithProduct_WhenProductExists()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var newProduct = new Product
                {
                    Name = "Test Product",
                    Price = 100,
                    Description = "Test product description",
                    CategoryName = "TestCategory",
                    ImageUrl = "https://example.com/image.jpg"
                };
                dbContext.Products.Add(newProduct);
                await dbContext.SaveChangesAsync();

                // Act
                var productName = Uri.EscapeDataString("Test Product");
                var response = await _productApiClient.GetAsync($"/api/products/by-name/{productName}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("Product retrieved successfully.");

                var product = ((JObject)responseDto.Result).ToObject<ProductDto>();

                product.Should().NotBeNull();
                product.Should().BeEquivalentTo(newProduct);
            }
        }

        [Test]
        public async Task GetByName_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            // Act
            var response = await _productApiClient.GetAsync("/api/products/by-name/NonExistentProduct");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Message.Should().Be("Product not found.");
        }

        [Test]
        public async Task GetByName_ShouldReturnBadRequest_WhenNameIsInvalid()
        {
            // Act
            var response = await _productApiClient.GetAsync("/api/products/by-name/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GetByCategory_ShouldReturnOkWithProducts_WhenProductsExistInCategory()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var productList = new List<Product>()
                {
                    new Product
                    {
                        Name = "Product 1",
                        Price = 100,
                        Description = "Description for Product 1",
                        CategoryName = "Electronics",
                        ImageUrl = "https://example.com/product1.jpg"
                    },
                    new Product
                    {
                        Name = "Product 2",
                        Price = 200,
                        Description = "Description for Product 2",
                        CategoryName = "Electronics",
                        ImageUrl = "https://example.com/product2.jpg"
                    }
                };

                dbContext.Products.AddRange(productList);
                await dbContext.SaveChangesAsync();

                // Act
                var categoryName = Uri.EscapeDataString("Electronics");
                var response = await _productApiClient.GetAsync($"/api/products/category/{categoryName}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("Product retrieved successfully.");

                var products = ((JArray)responseDto.Result).ToObject<List<ProductDto>>();
                products.Should().HaveCount(2);
                products.Should().BeEquivalentTo(productList);
            }
        }

        [Test]
        public async Task GetByCategory_ShouldReturnNotFound_WhenNoProductsExistInCategory()
        {
            // Act
            var categoryName = Uri.EscapeDataString("NonExistingCategory");
            var response = await _productApiClient.GetAsync($"/api/products/category/{categoryName}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Message.Should().Be("Product not found.");
        }


        [Test]
        public async Task Post_ShouldReturnOkWithResponseDto_WhenProductIsAddedSuccessfully()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var token = await GetJwtTokenForAdminUserAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var newProduct = new
                {
                    Name = "New Test Product",
                    Price = 150,
                    Description = "This is a new test product",
                    CategoryName = "Laptop",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(newProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PostAsync("/api/products", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("Product created successfully.");

                using (var newScope = _productApiFactory.Services.CreateScope())
                {
                    var dbContext = newScope.ServiceProvider.GetRequiredService<ProductDbContext>();
                    var addedProduct = await dbContext.Products
                        .FirstOrDefaultAsync(p => p.Name == "New Test Product");
                    
                   addedProduct.Should().BeEquivalentTo(newProduct);
                }
            }
        }

        [Test]
        public async Task Post_ShouldReturnBadRequest_WhenDataInvalidAndProductNotAdded()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var token = await GetJwtTokenForAdminUserAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var noNameProduct = new
                {
                    Price = 150,
                    Description = "This is a new test product",
                    CategoryName = "Laptop",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(noNameProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PostAsync("/api/products", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                responseString.Contains("Product name is required.");
            }
        }

        [Test]
        public async Task Put_ShouldReturnOkWithResponseDto_WhenUserIsAdminAndProductIsUpdatedSuccessfully()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                
                var token = await GetJwtTokenForAdminUserAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var product = new Product
                {
                    Name = "New Test Product",
                    Price = 150,
                    Description = "This is a new test product",
                    CategoryName = "Laptop",
                    ImageUrl = "https://placeholder.co/600x400",
                    CreatedByUserId = "Random",
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                var updatedProduct = new
                {
                    ProductId = product.ProductId,
                    Name = "Updated Product",
                    Price = 150,
                    Description = "Updated description",
                    CategoryName = "Updated Category",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(updatedProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PutAsync($"/api/products/{product.ProductId}", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("The product was updated successfully.");

                using (var newScope = _productApiFactory.Services.CreateScope())
                {
                    var dbContextNEW = newScope.ServiceProvider.GetRequiredService<ProductDbContext>();
                    var addedProduct = await dbContextNEW.Products
                        .FirstOrDefaultAsync(p => p.Name == "Updated Product");

                    addedProduct.Should().BeEquivalentTo(updatedProduct);
                }
            }
        }

        [Test]
        public async Task Put_ShouldReturnOkWithResponseDto_WhenUserIsOwnerAndProductIsUpdatedSuccessfully()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                
                var token = await GetJwtTokenForRegularUserAsync();
                var userId = await GetUserIdFromToken(token);

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var product = new Product
                {
                    Name = "New Test Product",
                    Price = 150,
                    Description = "This is a new test product",
                    CategoryName = "Laptop",
                    ImageUrl = "https://placeholder.co/600x400",
                    CreatedByUserId = userId,
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                var updatedProduct = new
                {
                    ProductId = product.ProductId,
                    Name = "Updated Product",
                    Price = 150,
                    Description = "Updated description",
                    CategoryName = "Updated Category",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(updatedProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PutAsync($"/api/products/{product.ProductId}", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("The product was updated successfully.");

                using (var newScope = _productApiFactory.Services.CreateScope())
                {
                    var dbContextNEW = newScope.ServiceProvider.GetRequiredService<ProductDbContext>();
                    var addedProduct = await dbContextNEW.Products
                        .FirstOrDefaultAsync(p => p.Name == "Updated Product");

                    addedProduct.Should().BeEquivalentTo(updatedProduct);
                }
            }
        }

        [Test]
        public async Task Put_ShouldReturnNotFoundWithResponseDto_WhenProductDoesNotExist()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var token = await GetJwtTokenForAdminUserAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updatedProduct = new
                {
                    ProductId = 9999,
                    Name = "Updated Product",
                    Price = 150,
                    Description = "Updated description",
                    CategoryName = "Updated Category",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(updatedProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PutAsync($"/api/products/9999", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Be("Product not found.");
            }
        }

        [Test]
        public async Task Put_ShouldReturnBadRequest_WhenUrlIdDoesNotMatchBodyId()
        {
            var token = await GetJwtTokenForAdminUserAsync();

            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updatedProduct = new
                {
                    ProductId = 2,
                    Name = "Updated Product",
                    Price = 150,
                    Description = "Updated description",
                    CategoryName = "Updated Category",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(updatedProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PutAsync($"/api/products/1", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Be("ID in URL does not match ID in the model.");
            }
        }

        [Test]
        public async Task Put_ShouldReturnForbiddenWithResponseDto_WhenUserIsNotAuthorizedToUpdateProduct()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                
                var token = await GetJwtTokenForRegularUserAsync();

                var product = new Product
                {
                    Name = "New Test Product",
                    Price = 150,
                    Description = "This is a new test product",
                    CategoryName = "Laptop",
                    ImageUrl = "https://placeholder.co/600x400",
                    CreatedByUserId = "AnotherUserId"
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updatedProduct = new
                {
                    ProductId = product.ProductId,
                    Name = "Updated Product",
                    Price = 150,
                    Description = "Updated description",
                    CategoryName = "Updated Category",
                    ImageUrl = "https://placeholder.co/600x400",
                };

                var jsonContent = JsonConvert.SerializeObject(updatedProduct);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _productApiClient.PutAsync($"/api/products/{product.ProductId}", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Be("Access denied. You are not authorized to update this product.");
            }
        }

        [Test]
        public async Task Delete_ShouldReturnOkWithResponseDto_WhenUserIsOwnerProductIsDeletedSuccessfully()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var token = await GetJwtTokenForRegularUserAsync();
                var userId = await GetUserIdFromToken(token);

                var product = new Product
                {
                    Name = "Test Product",
                    Price = 100,
                    Description = "This is a test product",
                    CategoryName = "Category",
                    ImageUrl = "https://placeholder.co/600x400",
                    CreatedByUserId = userId,
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Act
                var response = await _productApiClient.DeleteAsync($"/api/products/{product.ProductId}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Be("Product deleted successfully.");

                using (var newScope = _productApiFactory.Services.CreateScope())
                {
                    var dbContextNEW = newScope.ServiceProvider.GetRequiredService<ProductDbContext>();

                    var deletedProduct = await dbContextNEW.Products.FindAsync(product.ProductId);
                    deletedProduct.Should().BeNull();
                }
            }
        }

        [Test]
        public async Task Delete_ShouldReturnNotFoundWithResponseDto_WhenProductDoesNotExist()
        {
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var nonExistentProductId = 999;
                var token = await GetJwtTokenForRegularUserAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Act
                var response = await _productApiClient.DeleteAsync($"/api/products/{nonExistentProductId}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Be("Product not found.");
            }
        }

        [Test]
        public async Task Delete_ShouldReturnForbiddenWithResponseDto_WhenUserNotOwner()
        { 
            using (var scope = _productApiFactory.Services.CreateScope())
            {
                // Arrange
                var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var token = await GetJwtTokenForRegularUserAsync();
                var userId = await GetUserIdFromToken(token);

                var product = new Product
                {
                    Name = "Test Product",
                    Price = 100,
                    Description = "This is a test product",
                    CategoryName = "Category",
                    ImageUrl = "https://placeholder.co/600x400",
                    CreatedByUserId = "another-user-id",
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                var client = _productApiClient;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Act
                var response = await _productApiClient.DeleteAsync($"/api/products/{product.ProductId}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Be("You are not the creator of this product. " +
                        "Only the creator or administrator can delete this product.");

                using (var newScope = _productApiFactory.Services.CreateScope())
                {
                    var dbContextNEW = newScope.ServiceProvider.GetRequiredService<ProductDbContext>();

                    var notDeletedProduct = await dbContextNEW.Products.FindAsync(product.ProductId);
                    notDeletedProduct.Should().BeEquivalentTo(product);
                }
            }
        }
    }
}


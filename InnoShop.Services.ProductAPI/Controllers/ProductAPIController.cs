using Azure;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;

namespace InnoShop.Services.ProductAPI.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ProductAPIController : ControllerBase
    {
        private ResponseDto _response;
        private readonly IProductService _productService;

        public ProductAPIController(IProductService productService)
        {
            _response = new ResponseDto();
            _productService = productService;
        }

        [HttpGet("products")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> Get()
        {
            var products = await _productService.GetAllAsync();

            if (!products.Any())
            {
                _response.IsSuccess = false;
                _response.Message = "No products found.";
                return NotFound(_response);
            }

            _response.IsSuccess = true;
            _response.Message = "Products retrieved successfully.";
            _response.Result = products;
            return Ok(_response);
        }

        [HttpGet]
        [Route("products/{id:int}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> Get(int id)
        {
            var product = await _productService.GetById(id);

            if (product == null)
            {
                _response.IsSuccess = false;
                _response.Message = "Product not found.";
                return NotFound(_response);
            }
            _response.Message = "Product retrieved successfully.";
            _response.Result = product;
            return Ok(_response);
        }

        [HttpGet]
        [Route("products/by-name/{name}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> GetByName(string name)
        {
            var product = await _productService.GetByName(name);

            if (product == null)
            {
                _response.IsSuccess = false;
                _response.Message = "Product not found.";
                return NotFound(_response);
            }
            _response.IsSuccess = true;
            _response.Message = "Product retrieved successfully.";
            _response.Result = product;
            return Ok(_response);
        }

        [HttpGet]
        [Route("products/category/{category}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> GetByCategory(string category)
        {
            var products = await _productService.GetByCategory(category);

            if (products.Count() == 0)
            {
                _response.IsSuccess = false;
                _response.Message = "Product not found.";
                return NotFound(_response);
            }
            _response.IsSuccess = true;
            _response.Message = "Product retrieved successfully.";
            _response.Result = products;
            return Ok(_response);
        }

        [Authorize]
        [HttpPost("products/")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ResponseDto>> Post([FromBody] ProductDto productDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            productDto.CreatedByUserId = userId;

            var product = await _productService.CreateAsync(productDto);

            _response.IsSuccess = true;
            _response.Message = "Product created successfully.";
            _response.Result = product;
            return Ok(_response);
        }

        [Authorize]
        [HttpPut("products/{id}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> Put(int id, [FromBody] ProductDto productDto)
        {
            if (id != productDto.ProductId)
            {
                _response.IsSuccess = false;
                _response.Message = "ID in URL does not match ID in the model.";
                return BadRequest(_response);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var productFromDbDto = await _productService.GetById(id);

            if (productFromDbDto == null)
            {
                _response.IsSuccess = false;
                _response.Message = "Product not found.";
                return NotFound(_response);
            }

            if (User.IsInRole("ADMIN") || productFromDbDto.CreatedByUserId == userId)
            {
                var updatedProduct = await _productService.UpdateAsync(productDto);

                if (updatedProduct == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Failed to update the product.";
                    _response.Result = productDto;
                    return StatusCode(StatusCodes.Status400BadRequest, _response);
                }
                _response.IsSuccess = true;
                _response.Message = "The product was updated successfully.";
                _response.Result = productDto;
                return Ok(_response);
            }
            else
            {
                _response.IsSuccess = false;
                _response.Message = "Access denied. You are not authorized to update this product.";
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }
        }

        [HttpDelete]
        [Route("products/{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResponseDto>> Delete(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var productFromDbDto = await _productService.GetById(id);

            if (productFromDbDto == null)
            {
                _response.IsSuccess = false;
                _response.Message = "Product not found.";
                return NotFound(_response);
            }
            if (User.IsInRole("ADMIN") || productFromDbDto.CreatedByUserId == userId)
            {
                await _productService.DeleteAsync(id);

                _response.IsSuccess = true;
                _response.Message = "Product deleted successfully.";
                return Ok(_response);
            }
            else
            {
                _response.IsSuccess = false;
                _response.Message = "You are not the creator of this product. " +
                    "Only the creator or administrator can delete this product.";
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }
        }
    }
}

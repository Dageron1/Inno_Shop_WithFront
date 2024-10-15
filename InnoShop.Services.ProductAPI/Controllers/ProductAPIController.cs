using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using InnoShop.Services.ProductAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoShop.Services.ProductAPI.Controllers;

[Route("api/products")]
[ApiController]
public class ProductApiController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILinkService _linkService;

    public ProductApiController(IProductService productService, ILinkService linkService)
    {
        _productService = productService;
        _linkService = linkService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<Response<ProductDto>>> Get()
    {
        var products = await _productService.GetAllAsync();

        if (products.Length == 0)
        {
            return NoContent();
        }

        var productsWithLinks = products.Select(product => new Response<ProductDto>
        {
            Result = product,
            Links = _linkService.GenerateProductLinks(product.ProductId)
        });

        return Ok(productsWithLinks);
    }

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<Response<ProductDto>>> Get(int id)
    {
        var product = await _productService.GetById(id);

        if (product == null)
        {
            return NoContent();
        }

        return Ok(new Response<ProductDto>
        {
            Result = product,
            Links = _linkService.GenerateProductLinks(id)
        });
    }

    [HttpGet]
    [Route("by-name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<Response<ProductDto>>> GetByName(string name)
    {
        var product = await _productService.GetByName(name);

        if (product == null)
        {
            return NoContent();
        }

        return Ok(new Response<ProductDto>
        {
            Result = product,
            Links = _linkService.GenerateProductLinks(product.ProductId)
        });
    }

    [HttpGet]
    [Route("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ProductDto>>> GetByCategory(string category)
    {
        var products = await _productService.GetByCategory(category);

        if (products.Length == 0)
        {
            return NotFound();
        }

        var productsWithLinks = products.Select(product => new Response<ProductDto>
        {
            Result = product,
            Links = _linkService.GenerateProductLinks(product.ProductId)
        });

        return Ok(productsWithLinks);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<ProductDto>>> Post([FromBody] ProductDto productDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        productDto.CreatedByUserId = userId;

        var product = await _productService.CreateAsync(productDto);

        return StatusCode(201, new Response<ProductDto>
        {
            Result = product,
            Links = _linkService.GenerateProductLinks(product.ProductId)
        });
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ProductDto>>> Put(int id, [FromBody] ProductDto productDto)
    {
        if (id != productDto.ProductId)
        {
            return BadRequest();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var productFromDbDto = await _productService.GetById(id);
        if (productFromDbDto == null)
        {
            return NotFound();
        }

        if (IsUserNotAdminOrOwner(productFromDbDto.CreatedByUserId, userId))
        {
            return Forbid();
        }

        productDto.CreatedByUserId = userId;
        var updatedProduct = await _productService.UpdateAsync(productDto);
        var links = _linkService.GenerateProductLinks(updatedProduct.ProductId);

        return Ok(new Response<ProductDto>
        {
            Result = updatedProduct,
            Links = links
        });
    }

    [Authorize]
    [HttpDelete]
    [Route("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ProductDto>>> Delete(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var productFromDbDto = await _productService.GetById(id);

        if (productFromDbDto == null)
        {
            return NotFound();
        }

        if (IsUserNotAdminOrOwner(productFromDbDto.CreatedByUserId, userId))
        {
            return Forbid();
        }

        await _productService.DeleteAsync(id);

        return NoContent();
    }

    private bool IsUserNotAdminOrOwner(string dtoUserId, string userId) => !User.IsInRole(Role.Admin) && dtoUserId != userId;
}

using InnoShop.Services.ProductAPI.Controllers;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Web.Mvc;

namespace InnoShop.Services.ProductAPI.Services
{
    public class LinkService : ILinkService
    {
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LinkService(IUrlHelperFactory urlHelperFactory, IHttpContextAccessor httpContextAccessor)
        {
            _urlHelperFactory = urlHelperFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<Link> GenerateProductLinks(int id)
        {
            // Создаём IUrlHelper из фабрики
            var urlHelper = _urlHelperFactory.GetUrlHelper(
                new ActionContext(_httpContextAccessor.HttpContext,
                                  _httpContextAccessor.HttpContext.GetRouteData(),
                                  new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor())
            );

            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";

            return new List<Link>
            {
                new Link { Rel = "self", Href = $"{baseUrl}{urlHelper.Action(nameof(ProductApiController.Get), new { id })}", Method = HttpVerbs.Get.ToString()},
                new Link { Rel = "update", Href = $"{baseUrl}{urlHelper.Action(nameof(ProductApiController.Put), new { id })}", Method = HttpVerbs.Put.ToString()},
                new Link { Rel = "delete", Href = $"{baseUrl}{urlHelper.Action(nameof(ProductApiController.Delete), new { id })}", Method = HttpVerbs.Delete.ToString()}
            };
        }
    }
}

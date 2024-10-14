using InnoShop.Services.AuthAPI.Controllers;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace InnoShop.Services.AuthAPI.Services
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

        public List<object> GenerateLinks(int id)
        {
            // Создаём IUrlHelper из фабрики
            var urlHelper = _urlHelperFactory.GetUrlHelper(
                new ActionContext(_httpContextAccessor.HttpContext,
                                  _httpContextAccessor.HttpContext.GetRouteData(),
                                  new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor())
            );

            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";

            return new List<object>
            {
                new { rel = "self", href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.GetUserById), new { id })}" },
                new { rel = "update", href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.UpdateUser), new { id })}" },
                new { rel = "delete", href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.DeleteUser), new { id })}" }
            };
        }
    }
}

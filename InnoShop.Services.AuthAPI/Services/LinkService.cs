using InnoShop.Services.AuthAPI.Controllers;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Web.Mvc;

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

        public List<Link> GenerateCommonEmailLinks(string email)
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(
                new ActionContext(_httpContextAccessor.HttpContext,
                                  _httpContextAccessor.HttpContext.GetRouteData(),
                                  new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor())
            );

            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";

            return new List<Link>
            {
                new Link { Rel = "confirm_email", Href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.SendEmailConfirmation), new { email })}", Method = HttpVerbs.Post.ToString() },   
                new Link { Rel = "forgot_password", Href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.ForgotPassword), new { email })}", Method = HttpVerbs.Post.ToString() },
                new Link { Rel = "login", Href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.Login))}", Method = HttpVerbs.Post.ToString()}
            };
        }

        public List<Link> GenerateLoginAndResetLinks()
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(
                new ActionContext(_httpContextAccessor.HttpContext,
                                  _httpContextAccessor.HttpContext.GetRouteData(),
                                  new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor())
            );

            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";

            return new List<Link>
            {
                new Link { Rel = "forgot_password", Href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.ForgotPassword))}", Method = HttpVerbs.Post.ToString() },
                new Link { Rel = "login", Href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.Login))}", Method = HttpVerbs.Post.ToString()}
            };
        }



        //public List<Link> GenerateResetPasswordLink(string email)
        //{
        //    var urlHelper = _urlHelperFactory.GetUrlHelper(
        //        new ActionContext(_httpContextAccessor.HttpContext,
        //                          _httpContextAccessor.HttpContext.GetRouteData(),
        //                          new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor())
        //    );

        //    var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";

        //    return new List<Link>
        //    {
        //        new Link { Rel = "forgot_password", Href = $"{baseUrl}{urlHelper.Action(nameof(AuthApiController.ForgotPassword), new { email })}", Method = HttpVerbs.Post.ToString() },
        //    };
        //}
    }
}

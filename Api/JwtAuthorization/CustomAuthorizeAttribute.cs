using ITValet.HelpingClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ITValet.JwtAuthorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly IList<EnumRoles> roles;

        public CustomAuthorizeAttribute(params EnumRoles[] _roles)
        {
            roles = _roles ?? new EnumRoles[] { };
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // skip authorization if action is decorated with [AllowAnonymous] attribute
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
            if (allowAnonymous)
                return;

            // authorization
            var user = (UserClaims?)context.HttpContext.Items["LoggedinUser"];

            if (user == null || (roles.Any() && !roles.Contains((EnumRoles)Enum.Parse(typeof(EnumRoles), user.Role))))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}

using ITValet.JWTAuthentication;
using ITValet.Services;

namespace ITValet.JwtAuthorization
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserRepo userRepository, IJwtUtils jwtUtils)
        {
            string? token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var user = jwtUtils.ValidateToken(token);
            if (user != null)
            {
                //can get data from DB
                //context.Items["LoggedinUser"] = await userRepository.GetUserById(userId.Id);
                context.Items["LoggedinUser"] = user;
            }

            await _next(context);
        }
    }
}

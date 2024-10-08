using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ITValet.JWTAuthentication
{
    public interface IJwtUtils
    {
        public string GenerateToken(User user);
        public UserClaims? ValidateToken(string? token);
    }

    public class JwtUtils : IJwtUtils //also needs to register in program.cs as scoped
    {
        private readonly ProjectVariables _projectVariables;

        public JwtUtils(IOptions<ProjectVariables> options)
        {
            _projectVariables = options.Value;
        }

        public string GenerateToken(User user)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_projectVariables.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Id.ToString()!), //For SignalR Purpose Only
                new Claim("userFName", user.FirstName!),
                new Claim("userLName", user.LastName!),
                new Claim("userName", user.UserName!),
                new Claim("userEmail", user.Email!),
                new Claim("userRole", Enum.GetName(typeof(EnumRoles), user.Role!)!),
                new Claim("timeZone", user.Timezone!),
                new Claim("userStatus", Enum.GetName(typeof(EnumActiveStatus), user.IsActive!)!)
            }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public UserClaims? ValidateToken(string? token)
        {
            if (token == null)
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_projectVariables.JwtSecret);
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                var userFName = jwtToken.Claims.First(x => x.Type == "userFName").Value;
                var userLName = jwtToken.Claims.First(x => x.Type == "userLName").Value;
                var userName = jwtToken.Claims.First(x => x.Type == "userName").Value;
                var userEmail = jwtToken.Claims.First(x => x.Type == "userEmail").Value;
                var userRole = jwtToken.Claims.First(x => x.Type == "userRole").Value;
                var timeZone = jwtToken.Claims.First(x => x.Type == "timeZone").Value;
                var userStatus = jwtToken.Claims.First(x => x.Type == "userStatus").Value;

                UserClaims loggedinUser = new UserClaims()
                {
                    Id = userId,
                    UserEncId = StringCipher.EncryptId(userId),
                    FirstName = userFName,
                    LastName = userLName,
                    UserName = userName,
                    Email = userEmail,
                    Role = userRole,
                    Timezone = timeZone,
                    Status = userStatus
                };

                return loggedinUser;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}

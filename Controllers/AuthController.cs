using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
// using RestApi.Controllers.Entities;
using RestApi.Data;
using RestApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

//just checking the git connections

namespace RestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        private readonly HttpClient _httpClient;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger, HttpClient httpClient, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Log registration request details
                // _logger.LogInformation($"Received registration request: name={registerRequest.name}, email={registerRequest.email}, phone={registerRequest.phone}, roleId={registerRequest.roleId}, phonePrefix={registerRequest.phonePrefix}, ipAddress={registerRequest.ipAddress}, browser={registerRequest.browserInfo?.Browser}, browserVersion={registerRequest.browserInfo?.BrowserVersion}, os={registerRequest.os}");

                var role = await _context.Roles.FindAsync(registerRequest.roleId);
                var organization = await _context.Organization.FindAsync(registerRequest.organizationId);
                if (role == null)
                {
                    _logger.LogWarning($"Role or Organization not found for roleId={registerRequest.roleId}, organizationId={registerRequest.organizationId}. Assigning default values.");
                    registerRequest.roleId = 3;
                    registerRequest.organizationId = 1;
                }
                if (await _context.Users.AnyAsync(u => u.email == registerRequest.email))
                {
                    return Unauthorized(new
                    {
                        message = "Email address already exists.",
                        errors = new { email = "Email address already exists." }
                    });
                }
                if (await _context.Users.AnyAsync(u => u.phone == registerRequest.phone))
                {
                    return Unauthorized(new
                    {
                        message = "Phone number already exists.",
                        errors = new { phone = "Phone number already exists." }
                    });
                }
                if (registerRequest.password.Length < 8)
                {
                    return Unauthorized(new
                    {
                        message = "Password must be at least 8 characters.",
                        errors = new { password = "Password must be at least 8 characters." }
                    });
                }
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.password);
                var uid = Guid.NewGuid().ToString();

                // Create a new user object
                var newUser = new User
                {
                    name = registerRequest.name,
                    email = registerRequest.email,
                    password = hashedPassword,
                    phonePrefix = registerRequest.phonePrefix,
                    phone = registerRequest.phone,
                    street = "",
                    city = "",
                    state = "",
                    postalCode = "",
                    country = "",
                    isEmailVerified = false,
                    isPhoneNumberVerified = false,
                    isLoggedInByGoogleId = false,
                    isLoggedInByFaceBookId = false,
                    uuid = uid,
                    roleId = registerRequest.roleId ?? 2,
                    createdAt = DateTime.UtcNow,
                    modifiedAt = null,
                    organizationId = registerRequest.organizationId ?? 1,
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var loginDetails = new LoginDetails
                {
                    userId = newUser.userId,
                    ipAddress = registerRequest?.ipAddress,
                    browser = registerRequest?.browserInfo?.Browser,
                    browserVersion = registerRequest?.browserInfo?.BrowserVersion,
                    os = registerRequest?.os,
                    logState = "Register",
                    loginTime = DateTime.UtcNow
                };
                _context.LoginDetails.Add(loginDetails);
                await _context.SaveChangesAsync();
                var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, newUser.name),
                        new Claim(ClaimTypes.Email, newUser.email),
                        new Claim(ClaimTypes.Role, role?.name ?? "user"),
                        new Claim("roleId", newUser.roleId.ToString()),
                        new Claim("userId", newUser.userId.ToString())
                    };

                // Generate JWT Token
                var token = GenerateToken(authClaims);

                return Ok(new
                {
                    Message = "Registration successful",
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo,
                    User = new
                    {
                        newUser.userId,
                        newUser.email,
                        newUser.name,
                        Role = role?.name ?? "user"
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Log the DbUpdateException with all details
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                // Log any other unexpected exceptions
                _logger.LogError(ex, "An unexpected error occurred during registration.");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            var redirectUrl = Url.Action(nameof(GoogleCallbackAsync), "Auth");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items =
        {
            { "LoginProvider", "Google" }
        }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [Authorize]
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallbackAsync()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Google authentication failed." });
            }

            var claims = result.Principal?.Identities?.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (email == null)
            {
                return BadRequest(new { message = "Failed to retrieve email from Google login." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return BadRequest(new { message = "Failed to create user." });
                }
            }

            var claimsList = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("UserId", user.Id)
    };

            var token = GenerateToken(claimsList);

            return Ok(new
            {
                message = "Google login successful",
                email = email,
                token = token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                // Validate the model
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Find user by email or username
                var user = await _context.Users
                                         .Include(u => u.Role)
                                         .FirstOrDefaultAsync(u => u.email == loginRequest.EmailOrUsername || u.name == loginRequest.EmailOrUsername);

                // Check if user exists or password is invalid
                if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.password))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email/username or password.",
                        errors = new
                        {
                            credentials = "Invalid email/username or password."
                        }
                    });
                }
                var deviceDetails = loginRequest?.Device;
                var loginDetails = new LoginDetails
                {
                    userId = user.userId,
                    ipAddress = deviceDetails?.Ip,
                    browser = deviceDetails?.Browser,
                    browserVersion = deviceDetails?.BrowserVersion,
                    os = deviceDetails?.OS,
                    logState = "login",
                    loginTime = DateTime.UtcNow
                };

                _context.LoginDetails.Add(loginDetails);
                await _context.SaveChangesAsync();

                // Generate JWT Token
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.name),
                    new Claim(ClaimTypes.Email, user.email),
                    new Claim(ClaimTypes.Role, user.Role.name),
                    new Claim("roleId", user.roleId.ToString()),
                    new Claim("UserId", user.userId.ToString()),
                    new Claim("organizationId", user.organizationId.ToString())
                };

                var token = GenerateToken(authClaims);

                return Ok(new
                {
                    Message = "Login successful",
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo,
                    User = new
                    {
                        user.userId,
                        user.organizationId,
                        user.email,
                        user.name,
                        Role = user.Role.name
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred during login: {ex.Message}");
                return StatusCode(500, "Internal server error. Please try again later.");
            }
        }

        private JwtSecurityToken GenerateToken(IEnumerable<Claim> claims)

        {
            // var Key = Environment.GetEnvironmentVariable("JWT_KEY");

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")));

            if (authSigningKey.KeySize < 256)
            {
                throw new Exception("JWT Key must be at least 256 bits (32 characters).");
            }

            return new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                expires: DateTime.Now.AddHours(3),
                claims: claims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString();
                // _logger.LogInformation("Received token: {token}");

                // Console.WriteLine($"Token: {token}");

                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    // _logger.LogWarning("User not identified - UserId claim not found.");
                    return Unauthorized(new { message = "User not identified." });
                }

                if (!long.TryParse(userIdClaim, out long userId))
                {
                    // _logger.LogWarning("Invalid UserId in token.");
                    return Unauthorized(new { message = "Invalid UserId in token." });
                }

                var loginDetail = await _context.LoginDetails
                    .Where(ld => ld.userId == userId)
                    .OrderByDescending(ld => ld.loginTime)
                    .FirstOrDefaultAsync();

                if (loginDetail == null)
                {
                    return NotFound(new { message = "No active login session found." });
                }

                loginDetail.logoutTime = DateTime.UtcNow;
                loginDetail.logState = "logout";

                _context.LoginDetails.Update(loginDetail);
                await _context.SaveChangesAsync();

                TokenBlacklist.Add(token);

                return Ok(new { message = "Logout successful.", logoutTime = loginDetail.logoutTime });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred during logout: {ex.Message}");
                return StatusCode(500, "Internal server error during logout.");
            }
        }


        //api limit exceeded try feb 1

        // [HttpPost("validate-phone")]
        // public async Task<IActionResult> Validatephone([FromBody] phoneNumberRequest request)
        // {
        //     if (string.IsNullOrEmpty(request.phone))
        //     {
        //         return BadRequest("phone number is required.");
        //     }

        //     try
        //     {
        //         // Replace with your actual NumVerify API key
        //         var apiKey = "";
        //         var url = $"http://apilayer.net/api/validate?access_key={apiKey}&number={request.phone}&country_code={request.selectedCountry}&format={request.format}";

        //         var response = await _httpClient.GetStringAsync(url);
        //         var validationResponse = JsonConvert.DeserializeObject<dynamic>(response);

        //         if (validationResponse.valid == true)
        //         {
        //             // Custom success message
        //             return Ok(new { message = "✅ phone number is valid!", phoneDetails = validationResponse });
        //         }
        //         else
        //         {
        //             // Return a custom error message
        //             return BadRequest(new { message = "❌ Invalid phone number." });
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, "Internal server error: " + ex.Message);
        //     }
        // }
    }

}

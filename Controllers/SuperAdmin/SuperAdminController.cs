using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using RestApi.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/superAdmin")]
    [Authorize(Roles = "superAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminController> _logger;
        private readonly HttpClient _httpClient;

        public SuperAdminController(ApplicationDbContext context, IConfiguration configuration, ILogger<AdminController> logger, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        // Admin Dashboard Route

        [HttpGet("dashboard")]
        [Authorize(Roles = "superAdmin")]
        public IActionResult SuperAdminDashboard()
        {
            return Ok("Welcome, Admin!");
        }

        // Admin Settings Route
        [Authorize(Roles = "superAdmin")]
        [HttpGet("settings")]
        public IActionResult AdminSettings()
        {
            return Ok("Admin Settings Page");
        }

        [Authorize(Roles = "superAdmin")]
        [HttpPost("add-user")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var role = await _context.Roles.FindAsync(registerRequest.roleId);
                var organization = await _context.Organization.FindAsync(registerRequest.organizationId);

                if (role == null || organization == null)
                {
                    _logger.LogWarning($"Role or Organization not found for RoleId={registerRequest.roleId}, assigning default role.");

                    registerRequest.roleId = 2;
                    registerRequest.organizationId = 0;
                    role = await _context.Roles.FindAsync(registerRequest.roleId);
                    organization = await _context.Organization.FindAsync(registerRequest.organizationId);
                }

                // Check if the email is already registered
                if (await _context.Users.AnyAsync(u => u.email == registerRequest.email))
                {
                    return Unauthorized(new
                    {
                        message = "Email Address Already exist.",
                        errors = new
                        {
                            email = "Email Address Already exist."
                        }
                    });
                }

                // Check if the phone is already registered
                if (await _context.Users.AnyAsync(u => u.phone == registerRequest.phone))
                {
                    return Unauthorized(new
                    {
                        message = "Phone Number Already Exist.",
                        errors = new
                        {
                            phone = "Phone Number Already Exist."
                        }
                    });
                }

                // Check password length
                if (registerRequest.password.Length < 8)
                {
                    return Unauthorized(new
                    {
                        message = "Password must be at least 8 characters.",
                        errors = new
                        {
                            password = "Password must be at least 8 characters."
                        }
                    });
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.password);
                var uid = Guid.NewGuid().ToString();
                var newUser = new User
                {
                    name = registerRequest.name,
                    email = registerRequest.email,
                    password = hashedPassword,
                    phone = registerRequest.phone,
                    roleId = registerRequest.roleId ?? 2, // Fallback to default role if null
                    createdAt = DateTime.UtcNow,
                    modifiedAt = null,
                    phonePrefix = registerRequest.phonePrefix,
                    isEmailVerified = false,
                    isPhoneNumberVerified = false,
                    isLoggedInByGoogleId = false,
                    street = "",
                    city = "",
                    state = "",
                    postalCode = "",
                    country = "",
                    isLoggedInByFaceBookId = false,
                    uuid = uid,
                    organizationId = registerRequest.organizationId
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var organizationUser = new OrganizationUsers
                {
                    organizationId = newUser.organizationId,
                    roleId = newUser.roleId,
                    userId = newUser.userId
                };

                _context.OrganizationUsers.Add(organizationUser);
                await _context.SaveChangesAsync();

                // Return user along with organization user details
                var organizationUserDetails = await _context.OrganizationUsers
                    .Where(ou => ou.userId == newUser.userId)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    Message = "Registration successful",
                    User = newUser,
                    OrganizationUser = organizationUserDetails
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


        [Authorize(Roles = "superAdmin")]
        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetOrganizationDetails(int organizationId)
        {
            try
            {
                var organizationDetails = await _context.Organization
                    .Where(o => o.id == organizationId)
                    .Select(o => new
                    {
                        OrganizationId = o.id,
                        OrganizationName = o.name,
                        Users = _context.Users
                            .Where(u => u.organizationId == o.id && u.roleId != 1)
                            .Select(u => new
                            {
                                UserId = u.userId,
                                UserName = u.name,
                                UserEmail = u.email
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (organizationDetails == null)
                {
                    return NotFound(new { message = "Organization not found." });
                }

                return Ok(organizationDetails);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching organization details.");
                return StatusCode(500, new { message = "Internal server error. Please try again later." });
            }
        }

        [Authorize(Roles = "superAdmin")]
        [HttpGet("get-permissions/organizationId/{organizationId}/userId/{UserId}")]
        public async Task<IActionResult> GetOrganizationUserDetails(int UserId, int organizationId)
        {
            try
            {
                // Fetch organization details with permissions assigned to the user
                var organizationDetails = await _context.OrganizationUsers
                    .Where(ou => ou.userId == UserId && ou.organizationId == organizationId)
                    .Select(ou => new
                    {
                        OrganizationId = ou.organizationId,
                        Users = _context.Users
                            .Where(u => u.userId == ou.userId)
                            .Select(u => new
                            {
                                UserId = ou.userId,
                                UserName = u.name,
                                UserEmail = u.email,
                                Permissions = _context.OrganizationUserPermissions
                                    .Where(oup => oup.organizationUserId == ou.userId)
                                    .Join(_context.Permissions,
                                        oup => oup.permissionId,
                                        p => p.id,
                                        (oup, p) => new
                                        {
                                            PermissionId = p.id,
                                            PermissionName = p.resourceName + " " + p.action,
                                            CanRead = oup.canRead,
                                            CanWrite = oup.canWrite,
                                            CanUpdate = oup.canUpdate,
                                            CanDelete = oup.canDelete,
                                            IsVisible = oup.isVisible,
                                            IsHidden = oup.isHidden,
                                            IsDisabled = oup.isDisabled,
                                            IsRestricted = oup.isRestricted
                                        })
                                        .OrderBy(oup => oup.PermissionId)
                                    .ToList()
                            })
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();
                if (organizationDetails == null)
                {
                    return NotFound(new { message = "Organization User not found or does not have permissions." });
                }
                return Ok(organizationDetails);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching organization details and permissions.");
                return StatusCode(500, new { message = "Internal server error. Please try again later." });
            }
        }

        [HttpPost("createOrganization")]
        public async Task<IActionResult> RegisterOrganization([FromBody] RegisterRequest registerRequest)
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Log registration request details
                // _logger.LogInformation($"Received registration request: name={registerRequest.name}, email={registerRequest.email}, phone={registerRequest.phone}, roleId={registerRequest.roleId}, ipAddress={registerRequest.ipAddress}, browser={registerRequest.browserInfo?.browser}, browserVersion={registerRequest.browserInfo?.browserVersion}, os={registerRequest.os}, phonePrefix={registerRequest.phonePrefix}");
                if (await _context.Users.AnyAsync(u => u.email == registerRequest.email))
                {
                    return Unauthorized(new
                    {
                        message = "email Address Already exist.",
                        errors = new
                        {
                            email = "email Address Already exist."
                        }
                    });
                }
                if (await _context.Users.AnyAsync(u => u.phone == registerRequest.phone))
                {
                    return Unauthorized(new
                    {
                        message = "phone Number Already Exist.",
                        errors = new
                        {
                            phone = "phone Number Already Exist."
                        }
                    });
                }
                if (registerRequest.password.Length < 8)
                {
                    return Unauthorized(new
                    {
                        message = "password must be at least 8 characters.",
                        errors = new
                        {
                            password = "password must be at least 8 characters."
                        }
                    });
                }
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.password);
                var uid = Guid.NewGuid().ToString();
                var newUser = new User
                {
                    name = registerRequest.name,
                    email = registerRequest.email,
                    password = hashedPassword,
                    phone = registerRequest.phone,
                    roleId = 1,
                    createdAt = DateTime.UtcNow,
                    modifiedAt = null,
                    phonePrefix = registerRequest.phonePrefix,
                    isEmailVerified = false,
                    isPhoneNumberVerified = false,
                    isLoggedInByGoogleId = false,
                    street = "",
                    city = "",
                    state = "",
                    postalCode = "",
                    country = "",
                    isLoggedInByFaceBookId = false,
                    uuid = uid,
                    organizationId = 0
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var organization = new Organization
                {
                    userId = newUser.userId,
                    name = registerRequest?.name,
                    address = "",
                    gstNumber = "",
                    panNumber = ""
                };
                _context.Organization.Add(organization);
                await _context.SaveChangesAsync();

                var organizationId = organization.id;

                newUser.organizationId = organizationId;
                _context.Users.Update(newUser);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Organization successfully registered.",
                    user = newUser
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during registration.");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        [Authorize(Roles = "superAdmin")]
        [HttpPost("run-crew")]
        public async Task<IActionResult> FastApiUsersPost()
        {
            try
            {
                string fastApiUrl = "http://localhost:8000/run-crew";

                var requestData = new
                {
                    // topic = "AI Research",
                    // current_year = "2025"
                    query = "How many users are there?"
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(fastApiUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Error calling FastAPI service");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<dynamic>(jsonResponse);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}

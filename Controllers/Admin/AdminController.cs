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

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, IConfiguration configuration, ILogger<AdminController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // Admin Dashboard Route

        [HttpGet("dashboard")]
        [Authorize(Roles = "admin")]
        public IActionResult AdminDashboard()
        {
            return Ok("Welcome, Admin!");
        }

        // Admin Settings Route
        [Authorize(Roles = "admin")]
        [HttpGet("settings")]
        public IActionResult AdminSettings()
        {
            return Ok("Admin Settings Page");
        }

        [Authorize(Roles = "admin")]
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
                    registerRequest.organizationId = 1;
                    role = await _context.Roles.FindAsync(registerRequest.roleId);
                    organization = await _context.Organization.FindAsync(registerRequest.organizationId);
                }

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
                    roleId = registerRequest.roleId ?? 2,
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
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during registration.");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }


        [Authorize(Roles = "admin")]
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

        [Authorize(Roles = "admin")]
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
    }
}

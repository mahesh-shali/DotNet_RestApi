using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using System.Security.Claims;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize(Roles = "user")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public UserController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Endpoint for user dashboard
        [Authorize(Roles = "user")]
        [HttpGet("dashboard")]
        public IActionResult UserDashboard()
        {
            return Ok("Welcome, User!");
        }

        // Endpoint for user settings
        [Authorize(Roles = "user")]
        [HttpGet("settings")]
        public IActionResult UserSettings()
        {
            return Ok("User Settings Page");
        }

        [Authorize(Roles = "user")]
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
                            .Where(u => u.userId == ou.userId && u.roleId != 1)
                            .Select(u => new
                            {
                                UserId = u.userId,
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

                // If no organization details found, return a not found response
                if (organizationDetails == null)
                {
                    return NotFound(new { message = "Organization User not found or does not have permissions." });
                }

                // Return the organization details along with permissions
                return Ok(organizationDetails);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while updating the database during user registration.");
                return StatusCode(500, "Internal server error occurred during registration. Please try again.");
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 internal server error
                _logger.LogError(ex, "An error occurred while fetching organization details and permissions.");
                return StatusCode(500, new { message = "Internal server error. Please try again later." });
            }
        }

    }
}

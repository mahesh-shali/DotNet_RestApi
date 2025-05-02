using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RestApi.Models
{
    public class User
    {
        [Key]
        public int userId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string password { get; set; }

        [Required(ErrorMessage = "Phone is required.")]
        public string phone { get; set; }

        // [Required(ErrorMessage = "RoleId is required.")]
        public int? roleId { get; set; }

        public Role? Role { get; set; } // Navigation property

        public DateTime createdAt { get; set; }

        public DateTime? modifiedAt { get; set; }


        [JsonPropertyName("selectedCode")]
        public string? phonePrefix { get; set; }

        public bool? isEmailVerified { get; set; }

        public bool? isPhoneNumberVerified { get; set; }

        public bool? isLoggedInByGoogleId { get; set; }

        public string? street { get; set; }

        public string? city { get; set; }

        public string? state { get; set; }

        public string? postalCode { get; set; }

        public string? country { get; set; }

        public bool? isLoggedInByFaceBookId { get; set; }

        public string? uuid { get; set; }


        public int? organizationId { get; set; }

        [ForeignKey("organizationId")]
        public Organization? Organization { get; set; } // Navigation property

    }
}

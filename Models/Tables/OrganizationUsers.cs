using System;
using System.ComponentModel.DataAnnotations;

namespace RestApi.Models
{
    public class OrganizationUsers
    {
        [Key]
        public int id { get; set; }
        public int? organizationId { get; set; }
        public int? roleId { get; set; }
        public int? userId { get; set; }
    }
}

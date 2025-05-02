using System;
using System.ComponentModel.DataAnnotations;


namespace RestApi.Models
{
    public class OrganizationUserPermissions
    {
        [Key]
        public int id { get; set; }
        public int? organizationUserId { get; set; }
        public int? permissionId { get; set; }
        public bool? canRead { get; set; }
        public bool? canWrite { get; set; }
        public bool? canUpdate { get; set; }
        public bool? canDelete { get; set; }
        public bool? isVisible { get; set; }
        public bool? isHidden { get; set; }
        public bool? isDisabled { get; set; }
        public bool? isRestricted { get; set; }
        public int? organizationId { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace RestApi.Models
{
    public class Role
    {
        [Key]
        public int roleId { get; set; }
        public string? name { get; set; }
        public DateTime createdAt { get; set; }
    }
}

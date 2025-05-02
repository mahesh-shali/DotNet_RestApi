using System;
using System.ComponentModel.DataAnnotations;

namespace RestApi.Models
{
    public class Permissions
    {
        [Key]
        public int id { get; set; }
        public string? resourceName { get; set; }
        public string? action { get; set; }
        public string? description { get; set; }
    }
}

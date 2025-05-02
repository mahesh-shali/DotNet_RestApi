using System;
using System.ComponentModel.DataAnnotations;

namespace RestApi.Models
{
    public class Organization
    {
        [Key]
        public int id { get; set; }
        public int? userId { get; set; }
        public string? name { get; set; }

        public string? gstNumber { get; set; }

        public string? panNumber { get; set; }

        public string? address { get; set; }
    }
}

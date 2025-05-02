using System;
using System.Net;

namespace RestApi.Models
{
    public class LoginDetails
    {
        public int id { get; set; }

        public int userId { get; set; }

        public string? ipAddress { get; set; }
        public string? browser { get; set; }
        public string? browserVersion { get; set; }
        public string? os { get; set; }
        public string? logState { get; set; }  //register or login 
        public DateTime loginTime { get; set; }

        public DateTime? logoutTime { get; set; }
    }

}

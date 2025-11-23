using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementAPI.Models
{
    public class User
    {
        public int UserId { get; set; }  
        public int RefId { get; set; }               
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Patient"; 
        public int UserType { get; set; }         
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

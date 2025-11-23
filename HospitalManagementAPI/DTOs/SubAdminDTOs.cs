using System.Collections.Generic;

namespace HospitalManagementAPI.DTOs
{
    public class CreateSubAdminDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public List<string>? Responsibilities { get; set; } = new List<string>();
    }

    public class UpdateSubAdminDTO
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public List<string>? Responsibilities { get; set; }
        public bool? IsActive { get; set; } // if you want to toggle via Admin
    }

    public class SubAdminResponseDTO
    {
        public int SubAdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public List<string> Responsibilities { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }
}

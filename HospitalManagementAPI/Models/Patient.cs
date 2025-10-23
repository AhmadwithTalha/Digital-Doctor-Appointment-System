using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementAPI.Models
{
    public class Patient
    {
        public int PatientId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        // for now plain text to match existing workflow; later replace with hashed password
        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public string Role { get; set; } = "Patient";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<Appointment>? Appointments { get; set; }
    }
}

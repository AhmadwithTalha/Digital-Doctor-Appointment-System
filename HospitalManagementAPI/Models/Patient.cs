using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementAPI.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]

        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public string Role { get; set; } = "Patient";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<Appointment>? Appointments { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

    }
}

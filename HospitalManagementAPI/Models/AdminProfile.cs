using System.ComponentModel.DataAnnotations;

namespace HospitalManagementAPI.Models
{
    public class AdminProfile
    {
        [Key]
        public int AdminProfileId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        
        public string SystemEmail { get; set; }
        public string? PersonalEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }

        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

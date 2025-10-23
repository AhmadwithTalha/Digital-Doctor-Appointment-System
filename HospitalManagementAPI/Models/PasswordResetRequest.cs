using System;

namespace HospitalManagementAPI.Models
{
    public class PasswordResetRequest
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        // secure random token (GUID string)
        public string Token { get; set; } = string.Empty;

        // expiry and flags
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

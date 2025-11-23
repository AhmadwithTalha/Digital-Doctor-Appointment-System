namespace HospitalManagementAPI.DTOs
{
    public class PatientRegisterDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}

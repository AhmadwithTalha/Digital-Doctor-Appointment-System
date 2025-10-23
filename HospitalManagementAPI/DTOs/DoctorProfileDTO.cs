namespace HospitalManagementAPI.DTOs
{
    public class DoctorProfileDTO
    {
        public int DoctorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int RoomNo { get; set; }
        public decimal Fee { get; set; }
    }
}

namespace HospitalManagementAPI.DTOs
{
    public class CreateDoctorDTO
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Specialization { get; set; }
        public int DepartmentId { get; set; }
        public int RoomNo { get; set; }
        public decimal Fee { get; set; }
        public string Password { get; set; }
    }
}

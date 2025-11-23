namespace HospitalManagementAPI.DTOs
{
    public class UpdateDoctorDTO
    {
        public string Name { get; set; }
        public string Specialization { get; set; }
        public int DepartmentId { get; set; }
        public int RoomNo { get; set; }
        public decimal Fee { get; set; }
    }
}

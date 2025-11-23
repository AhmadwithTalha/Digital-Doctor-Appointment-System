
    public class DoctorDTO
    {
        public int DoctorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int RoomNo { get; set; }
        public decimal Fee { get; set; }
        public string Role { get; set; } = "Doctor";        
    }


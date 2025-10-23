namespace HospitalManagementAPI.DTOs
{
    public class UpdatePatientProfileDTO
    {
        
            public string FullName { get; set; }
            public string Gender { get; set; }
            public int? Age { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
        
    }
}

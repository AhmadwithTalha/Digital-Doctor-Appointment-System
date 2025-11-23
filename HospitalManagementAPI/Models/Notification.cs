namespace HospitalManagementAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserKey { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

    }
}

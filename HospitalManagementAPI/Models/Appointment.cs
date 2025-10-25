using HospitalManagementAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Appointments")]
public class Appointment
{
    [Key]
    public int AppointmentId { get; set; }

    [ForeignKey("Doctor")]
    public int DoctorId { get; set; }

    [ForeignKey("Patient")]
    public int PatientId { get; set; }

    [ForeignKey("Schedule")]
    public int? ScheduleId { get; set; }

    [Required]
    public int TokenNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime Date { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public Doctor Doctor { get; set; }
    public Patient Patient { get; set; }
    public DoctorSchedule Schedule { get; set; }
}

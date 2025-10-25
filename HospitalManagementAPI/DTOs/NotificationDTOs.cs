// DTOs/NotificationDTOs.cs
namespace HospitalManagementAPI.DTOs
{
    public record NextPatientNotification(
        int AppointmentId,
        int PatientId,
        string PatientName,
        int TokenNumber,
        string DoctorName,
        string DepartmentName,
        int RoomNo,
        string ScheduleTime,
        string Date // "yyyy-MM-dd"
    );

    public record QueueStatusNotification(
        int DoctorId,
        int TotalAppointments,
        int ServedCount,
        int RemainingCount,
        int CurrentToken // token being served now (0 if none)
    );

    public record SimpleNotification(string Title, string Message);
}

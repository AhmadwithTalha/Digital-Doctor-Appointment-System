//using HospitalManagementAPI.Models;
//using Microsoft.EntityFrameworkCore;
//using System.Numerics;

//namespace HospitalManagementAPI.Data
//{
//    public class AppDbContext : DbContext
//    {
//        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

//        public DbSet<Department> Departments { get; set; }
//        public DbSet<Doctor> Doctors { get; set; }
//        public DbSet<Admin> Admins { get; set; }
//        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
//        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }

//        public DbSet<Patient> Patients { get; set; }
//        public DbSet<Appointment> Appointments { get; set; }


//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//{
//    base.OnModelCreating(modelBuilder);

//    // ✅ Doctor → Department (Cascade delete)
//    modelBuilder.Entity<Doctor>()
//        .HasOne(d => d.Department)
//        .WithMany()
//        .HasForeignKey(d => d.DepartmentId)
//        .OnDelete(DeleteBehavior.Cascade);

//    // ✅ Schedule → Doctor (Cascade delete)
//    modelBuilder.Entity<DoctorSchedule>()
//        .HasOne(s => s.Doctor)
//        .WithMany()
//        .HasForeignKey(s => s.DoctorId)
//        .OnDelete(DeleteBehavior.Cascade);


//            modelBuilder.Entity<Appointment>()
//                .HasOne(a => a.Doctor)
//                .WithMany()
//                .HasForeignKey(a => a.DoctorId)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<Appointment>()
//                .HasOne(a => a.Patient)
//                .WithMany(p => p.Appointments)
//                .HasForeignKey(a => a.PatientId)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<Appointment>()
//                .HasOne(a => a.Schedule)
//                .WithMany()
//                .HasForeignKey(a => a.ScheduleId)
//                .OnDelete(DeleteBehavior.SetNull);

//        //    protected override void OnModelCreating(ModelBuilder modelBuilder)
//        //{
//            base.OnModelCreating(modelBuilder);

//            // Doctor → Appointment (Restrict cascade to avoid multiple paths)
//            modelBuilder.Entity<Appointment>()
//                .HasOne(a => a.Doctor)
//                .WithMany()
//                .HasForeignKey(a => a.DoctorId)
//                .OnDelete(DeleteBehavior.Restrict);  // ❌ No cascade

//            // Patient → Appointment (cascade is okay)
//            modelBuilder.Entity<Appointment>()
//                .HasOne(a => a.Patient)
//                .WithMany(p => p.Appointments)
//                .HasForeignKey(a => a.PatientId)
//                .OnDelete(DeleteBehavior.Cascade);

//            // Schedule → Appointment (SetNull is safe)
//            modelBuilder.Entity<Appointment>()
//                .HasOne(a => a.Schedule)
//                .WithMany()
//                .HasForeignKey(a => a.ScheduleId)
//                .OnDelete(DeleteBehavior.SetNull);
//        }


//    }



//}

using HospitalManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Department → Doctor (Cascade delete)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dep => dep.Doctors)

                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Doctor → Schedule (Cascade delete)
            modelBuilder.Entity<DoctorSchedule>()
                .HasOne(s => s.Doctor)
                .WithMany()
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Appointment relationships

            // Doctor → Appointment (Restrict to avoid multiple cascade paths)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient → Appointment (Cascade delete)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Schedule → Appointment (SetNull if schedule deleted)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Schedule)
                .WithMany()
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DoctorSchedule>()
    .HasOne(s => s.Doctor)
    .WithMany(d => d.DoctorSchedules) // ✅ now valid
    .HasForeignKey(s => s.DoctorId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Doctor>()
    .HasOne(d => d.Department)
    .WithMany(dep => dep.Doctors)
    .HasForeignKey(d => d.DepartmentId)
    .OnDelete(DeleteBehavior.Cascade);
        }


    }
}

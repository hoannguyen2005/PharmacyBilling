using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Models;
using PharmacyBillingService.Helpers;
using System;

namespace PharmacyBillingService.Data
{
    public class PharmacyDbContext : DbContext
    {
        public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<StockTransaction> StockTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique index for User Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure relations for PrescriptionItems
            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Medicine)
                .WithMany()
                .HasForeignKey(pi => pi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relations for Invoice
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Prescription)
                .WithMany()
                .HasForeignKey(i => i.PrescriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relations for Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relations for StockTransaction
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Medicine)
                .WithMany()
                .HasForeignKey(st => st.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed default Users (Admin, Doctor, Nurse, Receptionist, Patient)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "System Administrator",
                    Email = "admin@clinic.com",
                    PasswordHash = PasswordHasher.HashPassword("Admin@123"),
                    Role = "Admin",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 2,
                    FullName = "Dr. Nguyen Van A",
                    Email = "doctor@clinic.com",
                    PasswordHash = PasswordHasher.HashPassword("Doctor@123"),
                    Role = "Doctor",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 3,
                    FullName = "Nurse Tran Thi B",
                    Email = "nurse@clinic.com",
                    PasswordHash = PasswordHasher.HashPassword("Nurse@123"),
                    Role = "Nurse",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 4,
                    FullName = "Receptionist Le Thi C",
                    Email = "receptionist@clinic.com",
                    PasswordHash = PasswordHasher.HashPassword("Receptionist@123"),
                    Role = "Receptionist",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 5,
                    FullName = "Patient Pham Van D",
                    Email = "patient@clinic.com",
                    PasswordHash = PasswordHasher.HashPassword("Patient@123"),
                    Role = "Patient",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed default Medicines
            modelBuilder.Entity<Medicine>().HasData(
                new Medicine
                {
                    MedicineId = 1,
                    MedicineName = "Paracetamol 500mg",
                    ActiveIngredient = "Paracetamol",
                    Unit = "Viên",
                    Price = 2000m,
                    StockQuantity = 100,
                    MinStockLevel = 10,
                    ExpiryDate = new DateTime(2028, 12, 31),
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new Medicine
                {
                    MedicineId = 2,
                    MedicineName = "Vitamin C",
                    ActiveIngredient = "Ascorbic Acid",
                    Unit = "Viên",
                    Price = 6000m,
                    StockQuantity = 50,
                    MinStockLevel = 10,
                    ExpiryDate = new DateTime(2027, 6, 30),
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new Medicine
                {
                    MedicineId = 3,
                    MedicineName = "Amoxicillin 500mg",
                    ActiveIngredient = "Amoxicillin",
                    Unit = "Viên",
                    Price = 8000m,
                    StockQuantity = 120,
                    MinStockLevel = 15,
                    ExpiryDate = new DateTime(2026, 10, 15),
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

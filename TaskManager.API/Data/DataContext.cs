using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using TaskManager.API.Models;


//bu sınıf veritabanı ile ef arasında köprü görevi görür.olmazsa olmazdır.

namespace TaskManager.API.Data
{
    public class DataContext : DbContext // dbcontext sınıfından üretilmiş bir sınıf
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) 
        {
        }

        public DbSet<User> Users { get; set; } // veritabanındaki users tablosu ile ilişkili
        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<TaskCategory> TaskCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) //
        {
            // Users tablosu konfigürasyonu
            modelBuilder.Entity<User>(entity => 
            {
                entity.HasKey(e => e.UserId); 
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Tasks tablosu konfigürasyonu
            modelBuilder.Entity<Models.Task>(entity =>
            {
                entity.HasKey(e => e.TaskId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.IsCompleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // İlişkiler
                entity.HasOne(e => e.User)
                      .WithMany(u => u.CreatedTasks)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AssignedToUser)
                      .WithMany(u => u.AssignedTasks)
                      .HasForeignKey(e => e.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Categories tablosu konfigürasyonu
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Color).HasMaxLength(50);

                // İlişkiler
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Categories)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TaskCategories tablosu konfigürasyonu (Many-to-Many ilişki)
            modelBuilder.Entity<TaskCategory>(entity =>
            {
                entity.HasKey(e => new { e.TaskId, e.CategoryId });

                entity.HasOne(e => e.Task)
                      .WithMany(t => t.TaskCategories)
                      .HasForeignKey(e => e.TaskId);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.TaskCategories)
                      .HasForeignKey(e => e.CategoryId);
            });
        }
    }
}
using System;
using System.Collections.Generic;

namespace TaskManager.API.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<Task> CreatedTasks { get; set; }
        public ICollection<Task> AssignedTasks { get; set; }
        public ICollection<Category> Categories { get; set; }
    }

    public class Task
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int? Priority { get; set; }
        public int UserId { get; set; }
        public int? AssignedToUserId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public User AssignedToUser { get; set; }
        public ICollection<TaskCategory> TaskCategories { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int UserId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public ICollection<TaskCategory> TaskCategories { get; set; }
    }

    public class TaskCategory
    {
        public int TaskId { get; set; }
        public int CategoryId { get; set; }

        // Navigation properties
        public Task Task { get; set; }
        public Category Category { get; set; }
    }
}
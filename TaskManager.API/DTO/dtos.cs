using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs
{
    // User ile ilgili DTOs
    public class UserRegistrationDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Parolalar eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }

    public class UserLoginDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    // Task ile ilgili DTOs
    public class TaskDto
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
    }

    public class TaskDetailDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int? Priority { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; }
        public int? AssignedToUserId { get; set; }
        public string AssignedToUsername { get; set; }
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }

    public class CreateTaskDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(1, 3)]
        public int? Priority { get; set; }

        public int? AssignedToUserId { get; set; }

        public List<int> CategoryIds { get; set; } = new List<int>();
    }

    public class UpdateTaskDto
    {
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(1, 3)]
        public int? Priority { get; set; }

        public int? AssignedToUserId { get; set; }

        public List<int> CategoryIds { get; set; } = new List<int>();
    }

    public class TaskFilterDto
    {
        public bool? IsCompleted { get; set; }
        public int? Priority { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public List<int> CategoryIds { get; set; } = new List<int>();
    }

    // Category ile ilgili DTOs
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Color { get; set; }
    }

    public class UpdateCategoryDto
    {
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Color { get; set; }
    }

    // Sonuç DTOs
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public bool NotFound { get; set; }

        public static ServiceResult Ok(string message = "İşlem başarılı")
        {
            return new ServiceResult { IsSuccess = true, Message = message };
        }

        public static ServiceResult Fail(string message, bool notFound = false)
        {
            return new ServiceResult { IsSuccess = false, Message = message, NotFound = notFound };
        }
    }

    public class UserRegistrationResult : ServiceResult
    {
        public int? UserId { get; set; }

        public static UserRegistrationResult CreateSuccess(int userId)
        {
            return new UserRegistrationResult
            {
                IsSuccess = true,
                Message = "Kullanıcı başarıyla oluşturuldu",
                UserId = userId
            };
        }

        public static new UserRegistrationResult Fail(string message)
        {
            return new UserRegistrationResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}
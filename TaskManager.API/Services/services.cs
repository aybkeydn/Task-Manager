using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TaskManager.API.DTOs;
using TaskManager.API.Models;
using TaskManager.API.Repositories;

namespace TaskManager.API.Services
{
    // Auth Service
    public interface IAuthService
    {
        Task<UserRegistrationResult> RegisterUserAsync(UserRegistrationDto userRegistration);
        Task<User> ValidateUserAsync(UserLoginDto userLogin);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserRegistrationResult> RegisterUserAsync(UserRegistrationDto userRegistration)
        {
            // Kullanıcı adı veya email zaten kullanılıyor mu kontrol et
            if (await _userRepository.UserExistsAsync(userRegistration.Username, userRegistration.Email))
            {
                return UserRegistrationResult.Fail("Bu kullanıcı adı veya email zaten kullanılıyor.");
            }

            // Şifreyi hash'le
            CreatePasswordHash(userRegistration.Password, out byte[] passwordHash, out byte[] passwordSalt);

            // Yeni kullanıcı oluştur
            var user = new User
            {
                Username = userRegistration.Username,
                Email = userRegistration.Email,
                PasswordHash = Convert.ToBase64String(passwordHash) + ":" + Convert.ToBase64String(passwordSalt),
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Kullanıcıyı veritabanına kaydet
            var createdUser = await _userRepository.CreateUserAsync(user);

            return UserRegistrationResult.CreateSuccess(createdUser.UserId);
        }

        public async Task<User> ValidateUserAsync(UserLoginDto userLogin)
        {
            var user = await _userRepository.GetUserByUsernameAsync(userLogin.Username);
            if (user == null)
            {
                return null;
            }

            // Şifre doğrulama
            if (!VerifyPasswordHash(userLogin.Password, user.PasswordHash))
            {
                return null;
            }

            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var hash = Convert.FromBase64String(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);

            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Task Service
    public interface ITaskService
    {
        Task<IEnumerable<TaskDetailDto>> GetTasksAsync(int userId, TaskFilterDto filter = null);
        Task<IEnumerable<TaskDetailDto>> GetAssignedTasksAsync(int userId, TaskFilterDto filter = null);
        Task<TaskDetailDto> GetTaskByIdAsync(int taskId, int userId);
        Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto taskDto, int userId);
        Task<ServiceResult> UpdateTaskAsync(int taskId, UpdateTaskDto taskDto, int userId);
        Task<ServiceResult> DeleteTaskAsync(int taskId, int userId);
        Task<ServiceResult> ToggleTaskCompletionAsync(int taskId, int userId, bool isCompleted);
    }

    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ICategoryRepository _categoryRepository;

        public TaskService(ITaskRepository taskRepository, ICategoryRepository categoryRepository)
        {
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<TaskDetailDto>> GetTasksAsync(int userId, TaskFilterDto filter = null)
        {
            var tasks = await _taskRepository.GetTasksAsync(userId, filter);
            return tasks.Select(MapTaskToDetailDto);
        }

        public async Task<IEnumerable<TaskDetailDto>> GetAssignedTasksAsync(int userId, TaskFilterDto filter = null)
        {
            var tasks = await _taskRepository.GetAssignedTasksAsync(userId, filter);
            return tasks.Select(MapTaskToDetailDto);
        }

        public async Task<TaskDetailDto> GetTaskByIdAsync(int taskId, int userId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId, userId);
            return task == null ? null : MapTaskToDetailDto(task);
        }

        public async Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto taskDto, int userId)
        {
            // Yeni görev oluştur
            var task = new Models.Task
            {
                Title = taskDto.Title,
                Description = taskDto.Description,
                IsCompleted = false,
                CreatedDate = DateTime.Now,
                DueDate = taskDto.DueDate,
                Priority = taskDto.Priority,
                UserId = userId,
                AssignedToUserId = taskDto.AssignedToUserId,
                TaskCategories = new List<TaskCategory>()
            };

            // Kategori ilişkilerini ekle
            if (taskDto.CategoryIds != null && taskDto.CategoryIds.Any())
            {
                foreach (var categoryId in taskDto.CategoryIds)
                {
                    var category = await _categoryRepository.GetCategoryByIdAsync(categoryId, userId);
                    if (category != null)
                    {
                        task.TaskCategories.Add(new TaskCategory
                        {
                            CategoryId = categoryId
                        });
                    }
                }
            }

            // Görevi veritabanına kaydet
            var createdTask = await _taskRepository.CreateTaskAsync(task);

            // Detaylı DTO dönüşümünü yap
            return await GetTaskByIdAsync(createdTask.TaskId, userId);
        }

        public async Task<ServiceResult> UpdateTaskAsync(int taskId, UpdateTaskDto taskDto, int userId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId, userId);
            if (task == null)
            {
                return ServiceResult.Fail("Görev bulunamadı veya bu işlemi yapmaya yetkiniz yok.", true);
            }

            // Temel bilgileri güncelle
            if (!string.IsNullOrEmpty(taskDto.Title))
            {
                task.Title = taskDto.Title;
            }

            if (taskDto.Description != null) // null olabilir, boş string olabilir
            {
                task.Description = taskDto.Description;
            }

            task.DueDate = taskDto.DueDate;
            task.Priority = taskDto.Priority;
            task.AssignedToUserId = taskDto.AssignedToUserId;

            // Kategori ilişkilerini güncelle
            if (taskDto.CategoryIds != null)
            {
                // Mevcut kategorileri temizle
                task.TaskCategories.Clear();

                // Yeni kategorileri ekle
                foreach (var categoryId in taskDto.CategoryIds)
                {
                    var category = await _categoryRepository.GetCategoryByIdAsync(categoryId, userId);
                    if (category != null)
                    {
                        task.TaskCategories.Add(new TaskCategory
                        {
                            TaskId = task.TaskId,
                            CategoryId = categoryId
                        });
                    }
                }
            }

            // Görevi güncelle
            var success = await _taskRepository.UpdateTaskAsync(task);
            if (!success)
            {
                return ServiceResult.Fail("Görev güncellenirken bir hata oluştu.");
            }

            return ServiceResult.Ok("Görev başarıyla güncellendi.");
        }

        public async Task<ServiceResult> DeleteTaskAsync(int taskId, int userId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId, userId);
            if (task == null)
            {
                return ServiceResult.Fail("Görev bulunamadı veya bu işlemi yapmaya yetkiniz yok.", true);
            }

            // Sadece görevi oluşturan kullanıcı silebilir
            if (task.UserId != userId)
            {
                return ServiceResult.Fail("Bu görevi silme yetkiniz yok.");
            }

            var success = await _taskRepository.DeleteTaskAsync(task);
            if (!success)
            {
                return ServiceResult.Fail("Görev silinirken bir hata oluştu.");
            }

            return ServiceResult.Ok("Görev başarıyla silindi.");
        }

        public async Task<ServiceResult> ToggleTaskCompletionAsync(int taskId, int userId, bool isCompleted)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId, userId);
            if (task == null)
            {
                return ServiceResult.Fail("Görev bulunamadı veya bu işlemi yapmaya yetkiniz yok.", true);
            }

            task.IsCompleted = isCompleted;

            var success = await _taskRepository.UpdateTaskAsync(task);
            if (!success)
            {
                return ServiceResult.Fail($"Görev {(isCompleted ? "tamamlandı olarak işaretlenirken" : "tamamlanmadı olarak işaretlenirken")} bir hata oluştu.");
            }

            return ServiceResult.Ok($"Görev başarıyla {(isCompleted ? "tamamlandı" : "tamamlanmadı")} olarak işaretlendi.");
        }

        private TaskDetailDto MapTaskToDetailDto(Models.Task task)
        {
            return new TaskDetailDto
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                CreatedDate = task.CreatedDate,
                DueDate = task.DueDate,
                Priority = task.Priority,
                CreatedByUserId = task.UserId,
                CreatedByUsername = task.User?.Username,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToUsername = task.AssignedToUser?.Username,
                Categories = task.TaskCategories?.Select(tc => new CategoryDto
                {
                    CategoryId = tc.Category.CategoryId,
                    Name = tc.Category.Name,
                    Description = tc.Category.Description,
                    Color = tc.Category.Color
                }).ToList() ?? new List<CategoryDto>()
            };
        }
    }

    // Category Service
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync(int userId);
        Task<CategoryDto> GetCategoryByIdAsync(int categoryId, int userId);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto, int userId);
        Task<ServiceResult> UpdateCategoryAsync(int categoryId, UpdateCategoryDto categoryDto, int userId);
        Task<ServiceResult> DeleteCategoryAsync(int categoryId, int userId);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(int userId)
        {
            var categories = await _categoryRepository.GetCategoriesAsync(userId);
            return categories.Select(MapCategoryToDto);
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int categoryId, int userId)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId, userId);
            return category == null ? null : MapCategoryToDto(category);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto, int userId)
        {
            // Yeni kategori oluştur
            var category = new Category
            {
                Name = categoryDto.Name,
                Description = categoryDto.Description,
                Color = categoryDto.Color,
                UserId = userId
            };

            // Kategoriyi veritabanına kaydet
            var createdCategory = await _categoryRepository.CreateCategoryAsync(category);

            return MapCategoryToDto(createdCategory);
        }

        public async Task<ServiceResult> UpdateCategoryAsync(int categoryId, UpdateCategoryDto categoryDto, int userId)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId, userId);
            if (category == null)
            {
                return ServiceResult.Fail("Kategori bulunamadı veya bu işlemi yapmaya yetkiniz yok.", true);
            }

            // Bilgileri güncelle
            if (!string.IsNullOrEmpty(categoryDto.Name))
            {
                category.Name = categoryDto.Name;
            }

            if (categoryDto.Description != null) // null olabilir, boş string olabilir
            {
                category.Description = categoryDto.Description;
            }

            if (categoryDto.Color != null) // null olabilir, boş string olabilir
            {
                category.Color = categoryDto.Color;
            }

            // Kategoriyi güncelle
            var success = await _categoryRepository.UpdateCategoryAsync(category);
            if (!success)
            {
                return ServiceResult.Fail("Kategori güncellenirken bir hata oluştu.");
            }

            return ServiceResult.Ok("Kategori başarıyla güncellendi.");
        }

        public async Task<ServiceResult> DeleteCategoryAsync(int categoryId, int userId)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId, userId);
            if (category == null)
            {
                return ServiceResult.Fail("Kategori bulunamadı veya bu işlemi yapmaya yetkiniz yok.", true);
            }

            var success = await _categoryRepository.DeleteCategoryAsync(category);
            if (!success)
            {
                return ServiceResult.Fail("Kategori silinirken bir hata oluştu.");
            }

            return ServiceResult.Ok("Kategori başarıyla silindi.");
        }

        private CategoryDto MapCategoryToDto(Category category)
        {
            return new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color
            };
        }
    }
}

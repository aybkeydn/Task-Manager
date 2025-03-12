using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TaskManager.API.Data;
using TaskManager.API.DTOs;
using TaskManager.API.Models;

namespace TaskManager.API.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> UserExistsAsync(string username, string email);
        Task<User> CreateUserAsync(User user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;

        public UserRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            return await _context.Users.AnyAsync(u =>
                u.Username.ToLower() == username.ToLower() ||
                u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }

    public interface ITaskRepository
    {
        Task<IEnumerable<Models.Task>> GetTasksAsync(int userId, TaskFilterDto filter = null);
        Task<IEnumerable<Models.Task>> GetAssignedTasksAsync(int userId, TaskFilterDto filter = null);
        Task<Models.Task> GetTaskByIdAsync(int taskId, int userId);
        Task<Models.Task> CreateTaskAsync(Models.Task task);
        Task<bool> UpdateTaskAsync(Models.Task task);
        Task<bool> DeleteTaskAsync(Models.Task task);
        Task<bool> SaveChangesAsync();
        Task<IEnumerable<Models.Task>> GetTasksWithDetailsAsync(int? userId = null);
    }

    public class TaskRepository : ITaskRepository
    {
        private readonly DataContext _context;

        public TaskRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.Task>> GetTasksAsync(int userId, TaskFilterDto filter = null)
        {
            var query = _context.Tasks
                .Include(t => t.User)
                .Include(t => t.AssignedToUser)
                .Include(t => t.TaskCategories)
                    .ThenInclude(tc => tc.Category)
                .Where(t => t.UserId == userId);

            if (filter != null)
            {
                // Filtreleme işlemleri
                if (filter.IsCompleted.HasValue)
                {
                    query = query.Where(t => t.IsCompleted == filter.IsCompleted.Value);
                }

                if (filter.Priority.HasValue)
                {
                    query = query.Where(t => t.Priority == filter.Priority.Value);
                }

                if (filter.DueDateFrom.HasValue)
                {
                    query = query.Where(t => t.DueDate >= filter.DueDateFrom.Value);
                }

                if (filter.DueDateTo.HasValue)
                {
                    query = query.Where(t => t.DueDate <= filter.DueDateTo.Value);
                }

                if (filter.CategoryIds != null && filter.CategoryIds.Any())
                {
                    query = query.Where(t => t.TaskCategories.Any(tc => filter.CategoryIds.Contains(tc.CategoryId)));
                }
            }

            return await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        }

        public async Task<IEnumerable<Models.Task>> GetAssignedTasksAsync(int userId, TaskFilterDto filter = null)
        {
            var query = _context.Tasks
                .Include(t => t.User)
                .Include(t => t.AssignedToUser)
                .Include(t => t.TaskCategories)
                    .ThenInclude(tc => tc.Category)
                .Where(t => t.AssignedToUserId == userId);

            if (filter != null)
            {
                // Filtreleme işlemleri
                if (filter.IsCompleted.HasValue)
                {
                    query = query.Where(t => t.IsCompleted == filter.IsCompleted.Value);
                }

                if (filter.Priority.HasValue)
                {
                    query = query.Where(t => t.Priority == filter.Priority.Value);
                }

                if (filter.DueDateFrom.HasValue)
                {
                    query = query.Where(t => t.DueDate >= filter.DueDateFrom.Value);
                }

                if (filter.DueDateTo.HasValue)
                {
                    query = query.Where(t => t.DueDate <= filter.DueDateTo.Value);
                }

                if (filter.CategoryIds != null && filter.CategoryIds.Any())
                {
                    query = query.Where(t => t.TaskCategories.Any(tc => filter.CategoryIds.Contains(tc.CategoryId)));
                }
            }

            return await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        }

        public async Task<Models.Task> GetTaskByIdAsync(int taskId, int userId)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Include(t => t.AssignedToUser)
                .Include(t => t.TaskCategories)
                    .ThenInclude(tc => tc.Category)
                .FirstOrDefaultAsync(t => t.TaskId == taskId && (t.UserId == userId || t.AssignedToUserId == userId));
        }

        public async Task<Models.Task> CreateTaskAsync(Models.Task task)
        {
            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> UpdateTaskAsync(Models.Task task)
        {
            _context.Tasks.Update(task);
            return await SaveChangesAsync();
        }

        public async Task<bool> DeleteTaskAsync(Models.Task task)
        {
            _context.Tasks.Remove(task);
            return await SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Models.Task>> GetTasksWithDetailsAsync(int? userId = null)
        {
            var query = _context.Tasks
                .Include(t => t.User)
                .Include(t => t.AssignedToUser)
                .Include(t => t.TaskCategories)
                    .ThenInclude(tc => tc.Category)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(t => t.UserId == userId.Value || t.AssignedToUserId == userId.Value);
            }

            return await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        }
    }

    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategoriesAsync(int userId);
        Task<Category> GetCategoryByIdAsync(int categoryId, int userId);
        Task<Category> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(Category category);
        Task<bool> SaveChangesAsync();
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly DataContext _context;

        public CategoryRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync(int userId)
        {
            return await _context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int categoryId, int userId)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.UserId == userId);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            return await SaveChangesAsync();
        }

        public async Task<bool> DeleteCategoryAsync(Category category)
        {
            _context.Categories.Remove(category);
            return await SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManager.API.DTOs;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDetailDto>>> GetTasks([FromQuery] TaskFilterDto filter)// giriş yapmış kullanıcının görevlerini filtreleyerek getirir.
         //Fromquery: http isteğinden gelen parametreleri alır.
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var tasks = await _taskService.GetTasksAsync(userId, filter);
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDetailDto>> GetTask(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var task = await _taskService.GetTaskByIdAsync(id, userId);

            if (task == null)
            {
                return NotFound(new { Message = "Görev bulunamadı" });
            }

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskDetailDto>> CreateTask(CreateTaskDto taskDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var createdTask = await _taskService.CreateTaskAsync(taskDto, userId);

            return CreatedAtAction(nameof(GetTask), new { id = createdTask.TaskId }, createdTask);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto taskDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _taskService.UpdateTaskAsync(id, taskDto, userId);

            if (!result.IsSuccess)
            {
                if (result.NotFound)
                {
                    return NotFound(new { Message = result.Message });
                }
                return BadRequest(new { Message = result.Message });
            }

            return NoContent();
        }

        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _taskService.ToggleTaskCompletionAsync(id, userId, true);

            if (!result.IsSuccess)
            {
                if (result.NotFound)
                {
                    return NotFound(new { Message = result.Message });
                }
                return BadRequest(new { Message = result.Message });
            }

            return NoContent();
        }

        [HttpPatch("{id}/uncomplete")]
        public async Task<IActionResult> UncompleteTask(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _taskService.ToggleTaskCompletionAsync(id, userId, false);

            if (!result.IsSuccess)
            {
                if (result.NotFound)
                {
                    return NotFound(new { Message = result.Message });
                }
                return BadRequest(new { Message = result.Message });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _taskService.DeleteTaskAsync(id, userId);

            if (!result.IsSuccess)
            {
                if (result.NotFound)
                {
                    return NotFound(new { Message = result.Message });
                }
                return BadRequest(new { Message = result.Message });
            }

            return NoContent();
        }

        [HttpGet("assigned")]
        public async Task<ActionResult<IEnumerable<TaskDetailDto>>> GetAssignedTasks([FromQuery] TaskFilterDto filter)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var tasks = await _taskService.GetAssignedTasksAsync(userId, filter);
            return Ok(tasks);
        }
    }
}
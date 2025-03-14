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
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));//jwt veya authentication sisteminde oturum açılan id 'yi alır.
            var categories = await _categoryService.GetCategoriesAsync(userId);
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var category = await _categoryService.GetCategoryByIdAsync(id, userId);

            if (category == null)
            {
                return NotFound(new { Message = "Kategori bulunamadı" });
            }

            return Ok(category);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var createdCategory = await _categoryService.CreateCategoryAsync(categoryDto, userId);

            return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.CategoryId }, createdCategory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _categoryService.UpdateCategoryAsync(id, categoryDto, userId);

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
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _categoryService.DeleteCategoryAsync(id, userId);

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
    }
}
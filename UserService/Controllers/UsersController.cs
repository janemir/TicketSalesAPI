using Microsoft.AspNetCore.Mvc;
using UserService.Models.Dto;
using UserService.Services;
using Microsoft.AspNetCore.Authorization;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly Services.UserService _userService;

    public UsersController(Services.UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = await _userService.GetAsync();
        return Ok(users.Select(UserResponseDto.FromUser));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(string id)
    {
        var user = await _userService.GetAsync(id);
        if (user == null) return NotFound();
        return Ok(UserResponseDto.FromUser(user));
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto dto)
    {
        if (await _userService.GetByNameAsync(dto.Name) != null)
            return Conflict(new { message = "Пользователь с таким именем уже существует" });

        var newUser = await _userService.CreateWithPasswordAsync(dto.Name, dto.Password);
        return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, UserResponseDto.FromUser(newUser));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserDto dto)
    {
        if (await _userService.GetAsync(id) == null) return NotFound();

        if (await _userService.GetByNameAsync(dto.Name) is { } existing && existing.Id != id)
            return Conflict(new { message = "Пользователь с таким именем уже существует" });

        var updated = await _userService.UpdateProfileAsync(id, dto.Name, dto.Password);
        return Ok(UserResponseDto.FromUser(updated!));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userService.GetAsync(id);
        if (user == null) return NotFound();
        await _userService.RemoveAsync(id);
        return Ok(UserResponseDto.FromUser(user));
    }
}

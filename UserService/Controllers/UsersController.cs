using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Models.Dto;
using UserService.Services;

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
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _userService.GetAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(string id)
    {
        var user = await _userService.GetAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserDto dto)
    {
        var newUser = new User
        {
            Name = dto.Name,
            RegisteredObjects = 0
        };
        await _userService.CreateAsync(newUser);
        return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, CreateUserDto dto)
    {
        var user = await _userService.GetAsync(id);
        if (user == null) return NotFound();
        user.Name = dto.Name;
        await _userService.UpdateAsync(id, user);
        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userService.GetAsync(id);
        if (user == null) return NotFound();
        await _userService.RemoveAsync(id);
        return NoContent();
    }
}
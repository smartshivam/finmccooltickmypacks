using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class GuidesController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public GuidesController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: api/guides
    [HttpGet]
    public async Task<IActionResult> GetGuides()
    {
        var allUsers = _userManager.Users.ToList();
        var guideUsers = new List<IdentityUser>();

        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Guide"))
                guideUsers.Add(user);
        }

        var result = guideUsers.Select(u => new {
            Id = u.Id,
            Email = u.Email,
            UserName = u.UserName
        });
        return Ok(result);
    }

    // POST: api/guides Create a new guide
    [HttpPost]
    public async Task<IActionResult> CreateGuide([FromBody] GuideDto model)
    {
        if (!await _roleManager.RoleExistsAsync("Guide"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Guide"));
        }

        var user = new IdentityUser
        {
            UserName = model.UserName,
            Email = model.Email
        };
        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        await _userManager.AddToRoleAsync(user, "Guide");

        return Ok(new { Message = "Guide created", user.Id, user.Email });
    }

    // PUT: api/guides/{id} (update email, password)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGuide(string id, [FromBody] GuideDto model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound("Guide not found");

        user.Email = model.Email;
        user.UserName = model.UserName;

        if (!string.IsNullOrEmpty(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!resetResult.Succeeded)
                return BadRequest(resetResult.Errors);
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        return Ok(new { Message = "Guide updated" });
    }

    // DELETE: api/guides/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGuide(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound("Guide not found");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { Message = "Guide deleted" });
    }

    public class GuideDto
    {
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
}

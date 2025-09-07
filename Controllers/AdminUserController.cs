
using DoConnect.Api.Dtos;
using DoConnect.Api.Data;
using DoConnect.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoConnect.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AdminUsersController> _log;

        public AdminUsersController(AppDbContext db, ILogger<AdminUsersController> log)
        {
            _db = db;
            _log = log;
        }

        // GET: /api/admin/users?search=alice
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> List([FromQuery] string? search, CancellationToken ct)
        {
            var q = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(u => u.Username.Contains(term) || u.Email.Contains(term));
            }

            var data = await q
                .OrderBy(u => u.Username)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(data);
        }

        // GET: /api/admin/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserSummaryDto>> Get(Guid id, CancellationToken ct)
        {
            var u = await _db.Users.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new UserSummaryDto
                {
                    Id = x.Id, Username = x.Username, Email = x.Email, Role = x.Role, CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            return u is null ? NotFound() : Ok(u);
        }

        // POST: /api/admin/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var username = dto.Username.Trim();

            if (await _db.Users.AnyAsync(u => u.Email == email || u.Username == username, ct))
                return Conflict(new { message = "Email or Username already exists." });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            var result = new UserSummaryDto
            {
                Id = user.Id, Username = user.Username, Email = user.Email, Role = user.Role, CreatedAt = user.CreatedAt
            };
            return CreatedAtAction(nameof(Get), new { id = user.Id }, result);
        }

        // PUT: /api/admin/users/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (u is null) return NotFound();

            var email = dto.Email.Trim().ToLowerInvariant();
            var username = dto.Username.Trim();

            var duplicate = await _db.Users.AnyAsync(x => x.Id != id && (x.Email == email || x.Username == username), ct);
            if (duplicate) return Conflict(new { message = "Email or Username already exists." });

            // Prevent demoting the last admin
            if (u.Role == RoleType.Admin && dto.Role != RoleType.Admin)
            {
                var otherAdmins = await _db.Users.CountAsync(x => x.Id != id && x.Role == RoleType.Admin, ct);
                if (otherAdmins == 0)
                    return BadRequest(new { message = "Cannot demote the last remaining admin." });
            }

            u.Username = username;
            u.Email = email;
            u.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: /api/admin/users/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (u is null) return NotFound();

            // Prevent self-delete
            var currentUserId = User?.Identity?.Name; // you stored NameIdentifier as NameClaimType in Program.cs
            if (Guid.TryParse(currentUserId, out var me) && me == id)
                return BadRequest(new { message = "Admins cannot delete their own account." });

            // Prevent deleting last admin
            if (u.Role == RoleType.Admin)
            {
                var otherAdmins = await _db.Users.CountAsync(x => x.Id != id && x.Role == RoleType.Admin, ct);
                if (otherAdmins == 0)
                    return BadRequest(new { message = "Cannot delete the last remaining admin." });
            }

            _db.Users.Remove(u);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
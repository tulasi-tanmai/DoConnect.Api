using System.ComponentModel.DataAnnotations;
using DoConnect.Api.Models;

namespace DoConnect.Api.Dtos
{
    public class CreateUserDto
    {
        [Required, StringLength(30, MinimumLength = 3)]
        public string Username { get; set; } = default!;

        [Required, EmailAddress, StringLength(128)]
        public string Email { get; set; } = default!;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = default!;

        [Required]
        public RoleType Role { get; set; } = RoleType.User;
    }

    public class UpdateUserDto
    {
        [Required, StringLength(30, MinimumLength = 3)]
        public string Username { get; set; } = default!;

        [Required, EmailAddress, StringLength(128)]
        public string Email { get; set; } = default!;

        [Required]
        public RoleType Role { get; set; } = RoleType.User;

        // Optional: set a new password
        [StringLength(100, MinimumLength = 8)]
        public string? NewPassword { get; set; }
    }

    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public RoleType Role { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
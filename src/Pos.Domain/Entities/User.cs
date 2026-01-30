using Pos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Pos.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public string PasswordHash { get; set; } = "";
        public bool IsActive { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public UserRole Role { get; set; }
    }
}
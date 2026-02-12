using Pos.Domain.Common;
using Pos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Pos.Domain.Entities
{
    public class User : BaseEntity
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Email { get; set; }
        public string PasswordHash { get; set; } = "";
        public UserStatus Status { get; set; } = UserStatus.Active;
        public UserRole Role { get; set; }
        public ICollection<Pos.Domain.Security.UserRole> UserRoles { get; set; } = new List<Pos.Domain.Security.UserRole>();
    }
}
using Pos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public string TokenHash { get; set; } = "";
        public DateTime ExpiresDate { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
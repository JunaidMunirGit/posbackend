using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Domain.Entities
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public string TokenHash { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiresDate { get; set; }
        public DateTime? UsedAt { get; set; }

        public bool IsActive => UsedAt is null && DateTime.UtcNow < ExpiresDate;
    }
}

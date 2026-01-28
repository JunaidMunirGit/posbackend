using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string TokenHash { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresDate { get; set; }
        public DateTime? RevokedDate { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public bool IsActive { get; set; }
    }
}
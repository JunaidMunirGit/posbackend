using Pos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Domain.Entities
{
    public class Product : BaseEntity
    {
        public required string Name { get; set; }
        public string Barcode { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
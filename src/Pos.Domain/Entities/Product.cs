using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Barcode { get; set; } = null!;
        public decimal Price { get; set; }
    }
}

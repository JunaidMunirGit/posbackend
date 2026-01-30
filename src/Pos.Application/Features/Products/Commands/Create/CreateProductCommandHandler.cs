using MediatR;
using Pos.Domain.Entities;
using Pos.Infrastructure.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
    {
        private readonly IAppDbContext _db;
        
        public CreateProductCommandHandler(IAppDbContext db) => _db = db;


        public async Task<int> Handle(CreateProductCommand request, CancellationToken ct)
        {
            var product = new Product
            {
                Name = request.Name,
                Barcode = request.Barcode,
                Price = request.Price
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);

            return product.Id;
        }

    }
}

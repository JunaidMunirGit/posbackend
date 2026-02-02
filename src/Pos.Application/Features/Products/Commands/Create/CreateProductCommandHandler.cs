using MediatR;
using Pos.Application.Common.Exceptions;
using Pos.Application.Security;
using Pos.Domain.Entities;
using Pos.Domain.Security;
using Pos.Infrastructure.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandler(IAppDbContext db,ICurrentUser currentUser) : IRequestHandler<CreateProductCommand, int>
    {
        private readonly IAppDbContext _db = db;
        private readonly ICurrentUser _currentUser = currentUser;

        public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {

            if (!_currentUser.HasPermission(Permission.ManageProducts))
                throw new ForbiddenException("Not allowed to create products.");

            var product = new Product
            {
                Name = request.Name,
                Barcode = request.Barcode,
                Price = request.Price
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(cancellationToken);

            return product.Id;
        }

    }
}

using MediatR;

namespace Pos.Application.Features.Products.Commands.CreateProduct
{
    public record CreateProductCommand(
     string Name,
     string Barcode,
     decimal Price
 ) : IRequest<int>;
}
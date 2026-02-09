using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Auth.Commands.AssignRoleCommand;
using Pos.Application.Features.Products.Commands.CreateProduct;

namespace Pos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;


        public async Task<IActionResult> AssignRole([FromBody] AssignRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
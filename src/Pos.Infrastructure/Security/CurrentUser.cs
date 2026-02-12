using Microsoft.AspNetCore.Http;
using Pos.Application.Security;
using Pos.Domain.Security;

namespace Pos.Infrastructure.Security;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private readonly IHttpContextAccessor _http = http;

    public bool HasPermission(Permission permission)
    {
        return _http.HttpContext?.User?.Claims.Any(c =>
            c.Type == "permission" &&
            c.Value == permission.ToString()) == true;
    }
}
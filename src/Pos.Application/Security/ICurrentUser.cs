using Pos.Domain.Security;

namespace Pos.Application.Security;

public interface ICurrentUser
{
    bool HasPermission(Permission permission);
}
namespace Pos.Domain.Security;

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public Permission Permission { get; set; }
}
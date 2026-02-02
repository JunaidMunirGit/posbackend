namespace Pos.Domain.Security;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}

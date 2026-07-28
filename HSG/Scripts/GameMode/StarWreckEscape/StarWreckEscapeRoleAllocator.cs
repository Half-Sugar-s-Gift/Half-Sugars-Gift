using Nebula.Roles.Assignment;
using Virial.Assignable;
using Virial.Events.Role;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// StarWreckEscape 角色分配器
/// 第一阶段：内鬼列表中的玩家为内鬼，其他人是船员
/// </summary>
public class StarWreckEscapeRoleAllocator : IRoleAllocator
{
    void IRoleAllocator.Assign(List<byte> impostors, List<byte> others)
    {
        RoleTable table = new();

        foreach (var id in impostors)
            table.SetRole(id, Impostor.MyRole);

        foreach (var id in others)
            table.SetRole(id, Crewmate.MyRole);

        GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(table));
        table.Determine();
    }
}

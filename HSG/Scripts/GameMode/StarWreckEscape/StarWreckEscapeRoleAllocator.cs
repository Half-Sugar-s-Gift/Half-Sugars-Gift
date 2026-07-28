using Nebula.Roles.Assignment;
using Virial.Assignable;
using Virial.Events.Role;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// StarWreckEscape 角色分配器
/// 通过 Nebula 的 InitializeRolePatch 在 RoleManager.SelectRoles 时调用。
/// 第一阶段：内鬼列表中的玩家为内鬼，其他人是船员。
/// </summary>
public class StarWreckEscapeRoleAllocator : IRoleAllocator
{
    void IRoleAllocator.Assign(List<byte> impostors, List<byte> others)
    {
        try
        {
            RoleTable table = new();

            foreach (var id in impostors)
                table.SetRole(id, Impostor.MyRole);

            foreach (var id in others)
                table.SetRole(id, Crewmate.MyRole);

            GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(table));
            table.Determine();
        }
        catch (System.Exception ex)
        {
            HsgDebug.Log($"[RoleAllocator] 主要分配流程异常: {ex.GetType().Name}: {ex.Message}");
            HsgDebug.Log("[RoleAllocator] 尝试备用分配...");
            FallbackAssign(impostors, others);
        }
    }

    /// <summary>
    /// 备用分配
    /// </summary>
    private static void FallbackAssign(List<byte> impostors, List<byte> others)
    {
        try
        {
            RoleTable table = new();
            foreach (var id in impostors) table.SetRole(id, Impostor.MyRole);
            foreach (var id in others) table.SetRole(id, Crewmate.MyRole);
            GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(table));
            table.Determine();
            HsgDebug.Log("[RoleAllocator] 备用分配完成");
        }
        catch (System.Exception ex2)
        {
            HsgDebug.Log($"[RoleAllocator] 备用分配也失败: {ex2.Message}，将使用 vanilla 角色分配");
        }
    }
}

using Nebula.Roles.Assignment;
using NebulaN.Roles.Crewmate;
using NebulaN.Roles.Impostor;
using Virial.Assignable;
using Virial.Events.Role;
using Virial.Game;

namespace hvtXsvc.GameMode.Framework;

/// <summary>
/// 默认角色分配器 — 1 内鬼（Vanilla Impostor），其余船员（Vanilla Crewmate）
/// 大多数游戏模式可直接使用，无需自建分配器
/// </summary>
public class DefaultRoleAllocator : IRoleAllocator
{
    private readonly int impostorCount;

    /// <param name="impostorCount">内鬼数量，默认 1</param>
    public DefaultRoleAllocator(int impostorCount = 1)
    {
        this.impostorCount = impostorCount;
    }

    void IRoleAllocator.Assign(List<byte> impostors, List<byte> others)
    {
        // 如果系统分配的内鬼数超过设定值，截断
        var actualImpostors = impostors.Take(impostorCount).ToList();
        // 剩下的归入船员
        var allOthers = new List<byte>(others);
        allOthers.AddRange(impostors.Skip(impostorCount));

        var table = new RoleTable();
        foreach (var id in actualImpostors)
            table.SetRole(id, Impostor.MyRole);
        foreach (var id in allOthers)
            table.SetRole(id, Crewmate.MyRole);

        GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(table));
        table.Determine();
    }

    IRoleDraftAllocator? IRoleAllocator.GetDraftAllocator() => null;
    DefinedGhostRole? IRoleAllocator.AssignToGhost(GamePlayer player) => null;
}

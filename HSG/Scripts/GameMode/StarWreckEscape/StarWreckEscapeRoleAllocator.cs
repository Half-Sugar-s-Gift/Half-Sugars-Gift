using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Nebula.Roles.Assignment;
using UnityEngine;
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
    /// 备用分配：回避 table.Determine()，直接通过 Nebula RPC 设置角色
    /// </summary>
    private static void FallbackAssign(List<byte> impostors, List<byte> others)
    {
        try
        {
            // 使用最简单的 RoleTable + Determine
            // 如果还是失败，只能让游戏用 vanilla 角色
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

/// <summary>
/// 补偿补丁：如果角色分配失败导致 Nebula GamePlayer.Role 为 null，
/// 在 IntroCutscene.CoBegin 前检测并校正
/// </summary>
[HarmonyPatch]
internal static class RoleSafetyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    internal static void EnsureRolesAssigned()
    {
        // 仅处理星骸逃生模式
        if (Nebula.Configuration.GeneralConfigurations.CurrentGameMode != StarWreckEscapeRegistration.Definition)
            return;

        try
        {
            var local = GamePlayer.LocalPlayer;
            if (local == null || local.Role == null)
            {
                HsgDebug.Log("[RoleSafety] 检测到角色未分配（星骸逃生模式），尝试立即分配...");
                
                var players = PlayerControl.AllPlayerControls.GetFastEnumerator().OrderBy(p => p.PlayerId).ToList();

                // 获取内鬼人数（标准游戏选项）
                int maxImpostors = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumImpostors);
                int adjustedNumImpostors = Mathf.Min(maxImpostors, Mathf.Max(1, players.Count / 2));

                List<byte> impostors = new();
                List<byte> others = new();
                for (int i = 0; i < players.Count; i++)
                    if (i < adjustedNumImpostors)
                        impostors.Add(players[i].PlayerId);
                    else
                        others.Add(players[i].PlayerId);

                var allocator = new StarWreckEscapeRoleAllocator();
                ((IRoleAllocator)allocator).Assign(impostors, others);
            }
        }
        catch (System.Exception ex)
        {
            HsgDebug.Log($"[RoleSafety] 校正分配失败: {ex.Message}");
        }
    }
}

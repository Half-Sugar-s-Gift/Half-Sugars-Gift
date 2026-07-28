using HarmonyLib;
using Nebula.Game;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 游戏启动补丁 — 确保星骸逃生模式在游戏开始时正确启动。
/// 不依赖 GameStartEvent 事件链，直接在 GameManager.StartGame 触发。
/// </summary>
[HarmonyPatch]
internal static class GameStartPatch
{
    // ========== 拦截 GameManager.StartGame（intro 播放完毕后触发） ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    internal static void OnGameManagerStart()
    {
        TryStartMode();
    }

    /// <summary>
    /// 检测当前是否为星骸逃生模式，是则启动状态机。
    /// 内部防重复，仅首次调用生效。
    /// </summary>
    private static void TryStartMode()
    {
        // 防重复：已启动则跳过
        if (PhaseStateMachine.CurrentPhase != StarWreckPhase.Inactive)
            return;

        // 检测模式
        var currentMode = Nebula.Configuration.GeneralConfigurations.CurrentGameMode;
        if (currentMode != StarWreckEscapeRegistration.Definition)
            return;

        HsgDebug.Log("[GameStartPatch] 检测到星骸逃生模式，启动状态机");
        PhaseStateMachine.StartGame();
    }
}

using System.Collections;
using HarmonyLib;
using Nebula.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// StarWreckEscape Intro 补丁 — 使用 Postfix + Priority.Last 模式。
/// 
/// 关键教训（参考 ImmersionDream DraftMode）：
/// 1. 不能用 Prefix 拦截 CoBegin — Nebula 的 ShowIntroPatch 也是 Prefix，
///    多个 Prefix 可能同时运行并互相覆盖 __result。
/// 2. 必须用 Postfix + Priority.Last — 在所有 Prefix 结束后替换 __result。
/// 3. 包装模式：先让原生 intro 协程完整播放（含角色揭示动画），
///    结束后再启动我们的 PhaseStateMachine。
/// 
/// Finalizer 作为兜底：如果 Nebula 的模块实例化仍抛出异常，吞掉并降级初始化。
/// </summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
internal static class IntroReplacePatch
{
    // ──── Postfix（正常路径：包装原生 intro）────
    [HarmonyPriority(Priority.Last)]
    static void Postfix(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (Nebula.Configuration.GeneralConfigurations.CurrentGameMode !=
            StarWreckEscapeRegistration.Definition)
            return;

        HsgDebug.Log("[StarWreckIntro] Postfix — 包装原生 intro 协程，将在其结束后启动游戏模式");

        var native = __result;
        __result = CoWrap(native).WrapToIl2Cpp();
    }

    /// <summary>
    /// 包装协程：先执行原生 intro（含角色揭示 + 动画），完成后启动星骸逃生模式。
    /// </summary>
    static IEnumerator CoWrap(Il2CppSystem.Collections.IEnumerator nativeIntro)
    {
        // ── 阶段 1：原生 intro ──
        // 此时 Nebula 的 ShowIntroPatch 已将 __result 设为原生揭示协程，
        // 角色会正常显示 "Crewmate" / "Impostor" 等动画。
        while (nativeIntro.MoveNext())
            yield return nativeIntro.Current;

        HsgDebug.Log("[StarWreckIntro] 原生 intro 完成，启动 PhaseStateMachine");

        // ── 阶段 2：启动我们的游戏模式 ──
        // 此时 intro 已结束，HUD 正常显示，游戏世界已加载。
        // PhaseStateMachine.StartGame() 防重复，仅首次调用生效。
        if (PhaseStateMachine.CurrentPhase == StarWreckPhase.Inactive)
            PhaseStateMachine.StartGame();
    }

    // ──── Finalizer（异常兜底）────
    static System.Exception Finalizer(System.Exception __exception)
    {
        if (__exception == null) return null;

        if (Nebula.Configuration.GeneralConfigurations.CurrentGameMode !=
            StarWreckEscapeRegistration.Definition)
            return __exception;

        HsgDebug.Log($"[StarWreckIntro] Finalizer 拦截异常: {__exception.GetType().Name}: {__exception.Message}");

        try
        {
            NebulaGameManager.Instance?.OnGameStart();
            HudManager.Instance.ShowVanillaKeyGuide();

            if (PhaseStateMachine.CurrentPhase == StarWreckPhase.Inactive)
                PhaseStateMachine.StartGame();
        }
        catch (System.Exception ex2)
        {
            HsgDebug.Log($"[StarWreckIntro] 降级初始化也失败: {ex2.Message}");
        }

        try
        {
            var intro = IntroCutscene.Instance;
            if (intro != null)
                UnityEngine.Object.Destroy(intro.gameObject);
        }
        catch { }

        return null;
    }
}

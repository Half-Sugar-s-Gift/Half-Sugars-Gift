using HarmonyLib;
using Nebula.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// Intro 安全补丁。
/// 
/// 背景：StarWreckEscapeModule 不直接实现 IGameModeModule（因 AddModule 为 internal），
/// 而是通过 GameModeModuleProxy（Reflection.Emit 动态代理）在 DIManager 中注册。
/// 正常情况下 Nebula 的 ShowIntroPatch 能通过代理正常调用模块方法，intro 流程不会崩溃。
/// 
/// 此补丁作为安全网：如果因未知原因 Nebula 的 ShowIntroPatch 抛异常，
/// Finalizer 会吞掉异常并执行降级初始化（触发 GameStartEvent + 销毁 intro）。
/// 这确保即使发生异常，游戏也能正常开始，不会黑屏卡死。
/// </summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
internal static class IntroSafetyPatch
{
    static Exception Finalizer(Exception __exception)
    {
        if (__exception == null) return null;

        // 仅处理星骸逃生模式下的异常
        if (Nebula.Configuration.GeneralConfigurations.CurrentGameMode !=
            StarWreckEscapeRegistration.Definition)
            return __exception; // 不是我们的模式，让异常正常传播

        HsgDebug.Log($"[IntroSafety] CoBegin 异常被拦截: {__exception.GetType().Name}: {__exception.Message}");

        try
        {
            // 降级：手动触发 GameStartEvent（模拟 ShowIntroPatch 的清理逻辑）
            NebulaGameManager.Instance?.OnGameStart();
            HudManager.Instance.ShowVanillaKeyGuide();
        }
        catch (System.Exception ex2)
        {
            HsgDebug.Log($"[IntroSafety] 降级初始化也失败: {ex2.Message}");
        }

        // 确保 intro 对象被销毁
        try
        {
            var intro = IntroCutscene.Instance;
            if (intro != null)
                GameObject.Destroy(intro.gameObject);
        }
        catch { }

        return null; // 吞掉异常
    }
}

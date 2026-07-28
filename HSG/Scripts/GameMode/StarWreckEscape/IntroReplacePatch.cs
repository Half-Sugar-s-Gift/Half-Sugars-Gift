using HarmonyLib;
using Nebula.Game;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 星骸逃生模式的 Intro 替换补丁。
/// 因 Nebula 的 ShowIntroPatch 调用 InstantiateModule() 时，
/// StarWreckEscapeModule 不实现 IGameModeModule 导致返回 null → NRE 崩溃，
/// 故用 [HarmonyPriority(Priority.First)] 在此方法上抢先拦截，
/// 完全代替 Nebula 和原版的 intro 流程。
/// </summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
[HarmonyPriority(Priority.First)]
internal static class IntroReplacePatch
{
    static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        // 仅拦截星骸逃生模式
        if (Nebula.Configuration.GeneralConfigurations.CurrentGameMode != StarWreckEscapeRegistration.Definition)
            return true; // 不是我们的模式，交给后续 patch 处理

        HsgDebug.Log("[StarWreckIntro] 拦截 IntroCutscene.CoBegin，替换为星骸逃生模式流程");

        // 设置 IntroCutscene 静态实例（NebulaGameManager.OnGameStart 依赖它）
        IntroCutscene.Instance = __instance;

        __result = CoBeginStarWreck(__instance).WrapToIl2Cpp();
        return false; // 阻止 Nebula 和原版 intro 执行
    }

    static System.Collections.IEnumerator CoBeginStarWreck(IntroCutscene __instance)
    {
        // 播放 intro 音效
        AmongUsLLImpl.SoundManagerInstance.PlaySound(__instance.IntroStinger, false, 1f, null);

        // 隐藏原版 intro UI 元素（避免闪烁）
        __instance.HideAndSeekPanels.SetActive(false);
        __instance.CrewmateRules.SetActive(false);
        __instance.ImpostorRules.SetActive(false);
        __instance.ImpostorName.gameObject.SetActive(false);
        __instance.ImpostorTitle.gameObject.SetActive(false);
        __instance.ImpostorText.gameObject.SetActive(false);

        // 短暂延迟确保音效播放 + UI 隐藏生效
        yield return new UnityEngine.WaitForSeconds(0.3f);

        // 触发 Nebula 的 OnGameStart（对应 ShowIntroPatch.OnDestroy 中的调用）
        // 这会触发 GameStartEvent，让订阅该事件的各系统（如 StarWreckEscapeGameStarter）正常初始化
        NebulaGameManager.Instance?.OnGameStart();
        HudManager.Instance.ShowVanillaKeyGuide();

        // 销毁 intro 对象
        GameObject.Destroy(__instance.gameObject);
    }
}

using HalfSugarGift.Roles.Neutral;

namespace HalfSugarGift.Core;

/// <summary>
/// 游戏加载时自动执行的入口点。
/// 在 Nebula 预处理阶段（加载所有插件后）触发。
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
public static class GameLoader
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        Execute();
    }

    static void Execute()
    {
        HsgDebug.Log($"[Debug] 插件加载成功");

        // 应用魔女审判长投票匿名补丁
        var harmony = new Harmony("HalfSugarGift.WitchJudge");

        // 重置计数器：PopulateResults 是 public 方法
        var populateResults = AccessTools.Method(typeof(MeetingHud), nameof(MeetingHud.PopulateResults));
        harmony.Patch(populateResults, new HarmonyMethod(typeof(WitchJudgeVoteIconPatch).GetMethod(nameof(WitchJudgeVoteIconPatch.PopulateResultsPrefix))));

        // 第二票匿名：ModBloopAVoteIcon 在 internal 类中，用名称查找
        var modBloopType = AccessTools.TypeByName("Nebula.Patches.PopulateResultPatch");
        if (modBloopType != null)
        {
            var modBloopMethod = AccessTools.Method(modBloopType, "ModBloopAVoteIcon");
            if (modBloopMethod != null)
            {
                harmony.Patch(modBloopMethod, new HarmonyMethod(typeof(WitchJudgeVoteIconPatch).GetMethod(nameof(WitchJudgeVoteIconPatch.ModBloopAVoteIconPrefix))));
            }
        }
    }
}
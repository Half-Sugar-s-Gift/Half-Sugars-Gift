using HalfSugarGift.Roles.Impostor;
using Nebula;
using Nebula.Extensions;

namespace HalfSugarGift.Roles.Modifier;

public class Weakness : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier,
    RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private Weakness() : base(
        "weakness", "WK", new Virial.Color(0.5f, 0.8f, 1f),
        []
    )
    { }

    public Citation Citation => Citations.hvtXsvc_hsg;
    public static readonly Weakness MyRole = new();
    bool ISpawnable.IsSpawnable => false;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        public Instance(GamePlayer player) : base(player) { }

        bool RuntimeAssignable.CanBeAwareAssignment => false; // 
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private int _remainingRounds;

        void RuntimeAssignable.OnActivated()
        {
            _remainingRounds = Mage.WeaknessExpiryRounds;
        }

        [Local]
        void OnTaskComplete(PlayerTaskCompleteLocalEvent ev)
        {
            if (ev.Player != MyPlayer || !AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        [Local]
        void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            if (ev.Murderer != MyPlayer) return;
            if (ev.Murderer == ev.Dead) return;
            NebulaManager.Instance.StartDelayAction(0.1f, () =>
            {
                if (!MyPlayer.IsDead)
                    MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
            });
        }

        [OnlyMyPlayer]
        [Local]
        void OnVentEnter(PlayerVentEnterEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (!AmOwner || ev.Player != MyPlayer) return;
            MyPlayer.RemoveModifier(MyRole);
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            if (!Mage.WeaknessEnableRoundExpiry) return;
            _remainingRounds--;
            if (_remainingRounds <= 0)
                MyPlayer.RemoveModifier(MyRole);
        }
    }
}

/// <summary>
/// Harmony 补丁：脆弱的玩家点击任意技能按钮时，优先触发诅咒死亡（阻止技能生效）
/// 感谢AI和plana。谢谢谢谢谢谢谢谢谢谢谢谢谢谢谢谢
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
internal static class WeaknessPatch
{
    static Harmony? harmony;

    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        harmony = new Harmony("hsg.weakness.detect");

        var doClick = typeof(ModAbilityButtonImpl).GetMethod("DoClick");
        if (doClick != null)
            harmony.Patch(doClick, prefix: new HarmonyMethod(typeof(WeaknessPatch).GetMethod(nameof(BeforeClick))));

        var doSubClick = typeof(ModAbilityButtonImpl).GetMethod("DoSubClick");
        if (doSubClick != null)
            harmony.Patch(doSubClick, prefix: new HarmonyMethod(typeof(WeaknessPatch).GetMethod(nameof(BeforeSubClick))));
    }

    /// <summary>
    /// 主技能按钮拦截：如果有脆弱修饰器，先自杀再阻止技能执行
    /// </summary>
    public static bool BeforeClick(ModAbilityButtonImpl __instance)
    {
        return TrySuicideWeakness();
    }

    /// <summary>
    /// 副技能按钮拦截
    /// </summary>
    public static bool BeforeSubClick(ModAbilityButtonImpl __instance)
    {
        return TrySuicideWeakness();
    }

    /// <returns>true=继续执行技能, false=被诅咒拦截</returns>
    private static bool TrySuicideWeakness()
    {
        var player = GamePlayer.LocalPlayer;
        if (player == null || player.IsDead) return true;
        if (!player.Modifiers.Any(m => m.Modifier == Weakness.MyRole)) return true;

        // 诅咒优先：先自杀，再阻止技能（假死等技能不会执行）
        player.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        return false;
    }
}

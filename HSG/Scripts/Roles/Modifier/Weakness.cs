using HalfSugarGift.Roles.Impostor;
using Nebula;
using Nebula.Extensions;

namespace HalfSugarGift.Roles.Modifier;

/// <summary>
/// 脆弱附加，由魔法师施加。执行任务或使用杀人技能时立即死亡。
/// 仅魔法师可见灰色名字。
/// 配置项已迁移至 Mage 类中。
/// </summary>
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

        bool RuntimeAssignable.CanBeAwareAssignment => false; // 玩家不知情
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private int _remainingRounds;

        void RuntimeAssignable.OnActivated()
        {
            _remainingRounds = Mage.WeaknessExpiryRounds;
        }

        // 完成任务时死亡
        [Local]
        void OnTaskComplete(PlayerTaskCompleteLocalEvent ev)
        {
            if (ev.Player != MyPlayer || !AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        // 使用杀人技能时死亡（延迟执行避免与当前杀流程冲突）
        [Local]
        void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            if (ev.Murderer != MyPlayer) return;
            NebulaManager.Instance.StartDelayAction(0.1f, () =>
            {
                if (!MyPlayer.IsDead)
                    MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
            });
        }

        // 使用管道技能时死亡（如工程师钻管道）
        [OnlyMyPlayer]
        [Local]
        void OnVentEnter(PlayerVentEnterEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        // 使用通用角色技能时死亡（覆盖 Nebula 自定义技能动作）
        [OnlyMyPlayer]
        [Local]
        void OnDoGameAction(PlayerDoGameActionEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        // 回合到期自动清除（仅当启用时）
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

using HalfSugarGift.Roles.Impostor;

namespace HalfSugarGift.Roles.Modifier;

public class Weakness : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier,
    RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private Weakness() : base("weakness", "WK", new Virial.Color(0.5f, 0.8f, 1f), []) { }

    public Citation Citation => Citations.hvtXsvc_hsg;
    public static readonly Weakness MyRole = new();
    bool ISpawnable.IsSpawnable => false;
    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        public Instance(GamePlayer player) : base(player) { }
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        private int _remainingRounds;

        void RuntimeAssignable.OnActivated() { _remainingRounds = Mage.WeaknessExpiryRounds; }

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
            NebulaManager.Instance.StartDelayAction(0.1f, () =>
            {
                if (!MyPlayer.IsDead)
                    MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
            });
        }

        [OnlyMyPlayer][Local]
        void OnVentEnter(PlayerVentEnterEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        [OnlyMyPlayer][Local]
        void OnDoGameAction(PlayerDoGameActionEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            MyPlayer.Suicide(Mage.WeaknessState, EventDetail.Kill, KillParameter.NormalKill);
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead) return;
            if (!Mage.WeaknessEnableRoundExpiry) return;
            _remainingRounds--;
            if (_remainingRounds <= 0) MyPlayer.RemoveModifier(MyRole);
        }
    }
}

using HalfSugarGift.Roles.Neutral;

namespace HalfSugarGift.Roles.Modifier;

/// <summary>
/// 护身符，由道士的「画符护身」施加。
/// 当持有者被击杀时，若道士存活，取消击杀，道士传送到杀手位置与杀手同归于尽。
/// 道士死亡后护身符失效。
/// </summary>
public class Amulet : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier,
    RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private Amulet() : base(
        "amulet", "符", new Virial.Color(0.8f, 0.7f, 0.2f),
        []
    )
    { }

    public Citation Citation => Citations.Hellos497;
    public static readonly Amulet MyRole = new();
    bool ISpawnable.IsSpawnable => false;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        public Instance(GamePlayer player) : base(player) { }

        bool RuntimeAssignable.CanBeAwareAssignment => true; // 玩家知情
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private bool _killBlockSubscribed;

        private void EnsureKillBlockSubscribed()
        {
            if (_killBlockSubscribed || GameOperatorManager.Instance == null) return;
            _killBlockSubscribed = true;
            GameOperatorManager.Instance.Subscribe<PlayerTryVanillaKillLocalEventAbstractPlayerEvent>(ev =>
            {
                // Target 是 IPlayerlike，需转为 GamePlayer 比较 PlayerId
                if (ev.Target is not GamePlayer target || target != MyPlayer) return;
                // 持有者已死
                if (MyPlayer.IsDead) return;

                // 查找存活的道士
                GamePlayer? taoist = null;
                foreach (var p in GamePlayer.AllPlayerlikes)
                {
                    if (p is GamePlayer gp && gp.Role?.Role == Taoist.MyRole && !gp.IsDead)
                    {
                        taoist = gp;
                        break;
                    }
                }
                if (taoist == null) return; // 道士已死，护身符失效

                // ev.Player 是凶手（AbstractPlayerEvent.Player 为杀手）
                var killer = ev.Player;
                if (killer == null) return;

                // 取消击杀，触发同归于尽
                ev.Cancel();
                Taoist.RpcTaoistSacrifice.Invoke((taoist.PlayerId, killer.PlayerId));

                // 护身符持有者本地显示屏幕标题
                if (MyPlayer.AmOwner)
                {
                    var title = NebulaAPI.CurrentGame?.GetModule<TitleShower>();
                    if (title != null)
                        title.SetText(Language.Translate("role.taoist.amuletSaved"),
                            new Virial.Color(0.8f, 0.7f, 0.2f), 2f, false);
                }
            }, this);
        }

        void RuntimeAssignable.OnActivated()
        {
            EnsureKillBlockSubscribed();

            // 被施加护身符的玩家显示"你获得了道士的护身符！"
            if (AmOwner)
            {
                var title = NebulaAPI.CurrentGame?.GetModule<TitleShower>();
                if (title != null)
                    title.SetText(Language.Translate("role.taoist.amuletApplied"),
                        new Virial.Color(0.8f, 0.7f, 0.2f), 2f, false);
            }
        }

        // 持有者因非击杀原因死亡时（如会议投票），移除护身符
        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (!AmOwner || ev.Player != MyPlayer) return;
            MyPlayer.RemoveModifier(MyRole);
        }
    }
}

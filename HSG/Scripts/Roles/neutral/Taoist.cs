using HalfSugarGift.Roles.Modifier;
using Nebula.Extensions;
using HSGTeam = HalfSugarGift.Core.Patch.Team;
using static HalfSugarGift.Core.Patch.Cor;

namespace HalfSugarGift.Roles.Neutral;

/// <summary>
/// 道士，中立阵营。
/// 画符护身：可使用1次，对目标施加护身符。
/// 目标被击杀时道士传送到杀手位置，与杀手同归于尽。
/// 死亡后跟随护身符目标胜利。
/// </summary>
public class Taoist : DefinedRoleTemplate, DefinedRole, HasCitation,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{
    // 画符护身冷却
    static FloatConfiguration AmuletCooldown = NebulaAPI.Configurations.Configuration(
        "options.role.taoist.amuletCooldown", (5f, 60f, 1f), 20f, FloatConfigurationDecorator.Second
    );

    private Taoist() : base(
        "taoist",
        new Virial.Color(0.8f, 0.7f, 0.2f),
        RoleCategory.NeutralRole,
        HSGTeam.TaoistTeam,
        [AmuletCooldown]
    ) { }

    public static readonly Taoist MyRole = new();
    public Citation Citation => Citations.Hellos497;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    Virial.Media.Image? DefinedAssignable.IconImage =>
        NebulaAPI.AddonAsset.GetResource("Smallicon/TaoistIcon.png")?.AsImage();

    // 死因标签
    public static readonly TranslatableTag SacrificedState = new TranslatableTag("state.taoistSacrifice");
    public static readonly TranslatableTag AmuletTriggeredState = new TranslatableTag("state.amuletTriggered");

    // 文档用技能图标（暂时采用击杀图标）
    internal static readonly Virial.Media.Image? AmuletDocIcon =
        NebulaAPI.AddonAsset.GetResource("ExecuteButton.png")?.AsImage(115f);

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(AmuletDocIcon, "role.taoist.doc.amulet");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield break;
    }

    // === 传送双杀 RPC ===
    internal static readonly RemoteProcess<(byte taoistId, byte killerId)> RpcTaoistSacrifice = new(
        "Taoist.Sacrifice",
        (data, _) =>
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var taoist = GamePlayer.GetPlayer(data.taoistId);
            var killer = GamePlayer.GetPlayer(data.killerId);
            if (taoist == null || taoist.IsDead) return;
            if (killer == null || killer.IsDead) return;

            // 传送到杀手位置
            taoist.RpcTeleport(killer.Position);

            // 延迟 0.3s 后双杀（给传送动画时间）
            NebulaManager.Instance.StartDelayAction(0.3f, () =>
            {
                if (!taoist.IsDead)
                    taoist.Suicide(SacrificedState, EventDetail.Kill, KillParameter.NormalKill);
                if (!killer.IsDead)
                    killer.Suicide(AmuletTriggeredState, EventDetail.Kill, KillParameter.NormalKill);
            });
        }
    );

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable,
        IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        bool RuntimeRole.HasVanillaKillButton => false;

        // 技能按钮图标
        private static readonly Virial.Media.Image? AmuletButtonIcon =
            NebulaAPI.AddonAsset.GetResource("ExecuteButton.png")?.AsImage(100f);

        // 状态
        private bool _used;
        private byte _amuletTargetId = byte.MaxValue; 

        private ModAbilityButton? _amuletButton;

        public Instance(GamePlayer player) : base(player) { }

        // 显示带淡入+保持+淡出效果的标题
        private static void SetTextWithFade(TitleShower? shower, string text, Virial.Color color, float holdDuration = 1f, bool shake = true)
        {
            if (shower == null) return;

            const float fadeInDuration = 0.3f;
            float fadeInTimer = fadeInDuration;
            float holdTimer = holdDuration;
            float fadeOutAlpha = 1f;
            float shakeTimer = 0f;

            shower.SetText(text, color, new TitleTrait(s =>
            {
                var dt = Time.deltaTime;

                // 抖动效果
                if (shake)
                {
                    shakeTimer -= dt;
                    if (shakeTimer < 0f)
                    {
                        shakeTimer = 0.08f;
                        s.Transform.localPosition = new(
                            ((float)System.Random.Shared.NextDouble() - 0.5f) * 0.06f,
                            ((float)System.Random.Shared.NextDouble() - 0.5f) * 0.06f);
                    }
                }

                // 透明度动画：淡入 → 保持 → 淡出
                if (fadeInTimer > 0f)
                {
                    fadeInTimer -= dt;
                    s.SetTextAlpha(Mathn.Clamp01(1f - fadeInTimer / fadeInDuration));
                }
                else if (holdTimer > 0f)
                {
                    holdTimer -= dt;
                    s.SetTextAlpha(1f);
                }
                else
                {
                    fadeOutAlpha -= dt * 0.5f;
                    s.SetTextAlpha(Mathn.Clamp01(fadeOutAlpha));
                }
            }));
        }

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;

            _used = false;
            _amuletTargetId = byte.MaxValue;

            var tracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

            // === 画符护身按钮 ===
            _amuletButton = NebulaAPI.Modules.AbilityButton(
                this, MyPlayer,
                VirtualKeyInput.Ability,
                AmuletCooldown,
                "taoist.amulet",
                AmuletButtonIcon,
                _ => !_used && MyPlayer.CanMove
                    && tracker.CurrentTarget != null && tracker.CurrentTarget != MyPlayer
                    && !tracker.CurrentTarget.IsDead,
                _ => !MyPlayer.IsDead && !_used
            );
            _amuletButton.ShowUsesIcon(1, "1");
            _amuletButton.OnClick = _ =>
            {
                var target = tracker.CurrentTarget;
                if (_used || target == null || target == MyPlayer || target.IsDead) return;
                target.AddModifier(Amulet.MyRole);
                _amuletTargetId = target.PlayerId;
                _used = true;
                _amuletButton.UpdateUsesIcon("0");

                // 屏幕标题提示
                SetTextWithFade(NebulaAPI.CurrentGame?.GetModule<TitleShower>(),
                    Language.Translate("role.taoist.amuletApplied"),
                    new Virial.Color(0.8f, 0.7f, 0.2f), 1.5f);
            };
        }

        // === 道士死亡 → 移除护身符 ===
        [Local]
        void OnMyDeath(PlayerDieEvent ev)
        {
            if (ev.Player != MyPlayer) return;
            if (_amuletTargetId == byte.MaxValue) return;
            var target = GamePlayer.GetPlayer(_amuletTargetId);
            if (target != null && !target.IsDead)
                target.RemoveModifier(Amulet.MyRole);
        }

        // === 道士死亡后跟随护身符目标胜利 ===
        [OnlyMyPlayer]
        void CheckExtraWins(PlayerCheckExtraWinEvent ev)
        {
            if (ev.Phase != ExtraWinCheckPhase.OpportunistPhase) return;
            if (!MyPlayer.IsAlive && _used && _amuletTargetId != 0)
            {
                var target = GamePlayer.GetPlayer(_amuletTargetId);
                if (target != null && target.IsAlive)
                {
                    ev.ExtraWinMask.Add(HSGTeam.ExtraTaoistWin);
                    ev.IsExtraWin = true;
                }
            }
        }

        void IGameOperator.OnReleased() { }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene) { }
    }
}

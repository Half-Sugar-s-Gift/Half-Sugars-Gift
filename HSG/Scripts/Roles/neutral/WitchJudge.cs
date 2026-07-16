using Nebula.Extensions;
using Nebula.Modules.Cosmetics;
using HSGTeam = HalfSugarGift.Core.Patch.Team;
using static HalfSugarGift.Core.Patch.Cor;

namespace HalfSugarGift.Roles.Neutral;

/// <summary>
/// 魔女审判长：中立执法型职业。
/// 被动：投票翻倍；平票驱逐；魔女之眼（透视被惩罚者阵营）；魔女诅咒（封印被惩罚者能力）；投票透视。
/// 主动：惩罚！锁定玩家；会议内可处刑被惩罚玩家。
/// </summary>
public class WitchJudge : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{
    // === 1. 角色配置 ===
    private static readonly FloatConfiguration PunishCooldown = NebulaAPI.Configurations.Configuration(
        "options.role.witchJudge.punishCooldown",
        (5f, 120f, 1f),
        20f,
        FloatConfigurationDecorator.Second
    );
    private static readonly FloatConfiguration PunishDuration = NebulaAPI.Configurations.Configuration(
        "options.role.witchJudge.punishDuration",
        (5f, 120f, 1f),
        30f,
        FloatConfigurationDecorator.Second
    );
    private static readonly IntegerConfiguration PunishMaxUses = NebulaAPI.Configurations.Configuration(
        "options.role.witchJudge.punishMaxUses",
        (1, 10, 1),
        3
    );
    private static readonly BoolConfiguration ShowVoteRecords = NebulaAPI.Configurations.Configuration(
        "options.role.witchJudge.showVoteRecords",
        true
    );

    // === 2. 角色注册 ===
    private WitchJudge() : base(
        "witchJudge",
        PurpleWitchJudge,
        RoleCategory.NeutralRole,
        HSGTeam.WitchJudgeTeam,
        new Virial.Configuration.IConfiguration[] {
            PunishCooldown, PunishDuration, PunishMaxUses,
            ShowVoteRecords
        }
    ) { }

    public static readonly WitchJudge MyRole = new();
    public Citation Citation => Citations.hvtXsvc_hsg;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    // 角色文档用图标
    private static readonly Virial.Media.Image? ExecuteDocIcon =
        NebulaAPI.AddonAsset.GetResource("ExecuteButton.png")?.AsImage(115f);

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ExecuteDocIcon, "role.witchJudge.doc.execute");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield break;
    }

    // 被惩罚玩家到期时间（全局追踪，所有客户端同步）
    internal static readonly Dictionary<byte, float> JailedUntil = new();

    // === 3. 角色实例 ===
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;

        // 处刑死因标签
        private static readonly TranslatableTag ExecutionState = State.ExecutedByJudge;

        // 处刑按钮图标
        private static readonly Virial.Media.Image? ExecuteIcon =
            NebulaAPI.AddonAsset.GetResource("ExecuteButton.png")?.AsImage(170f);

        // 惩罚按钮图标
        private static readonly Virial.Media.Image? PunishIcon =
            NebulaAPI.AddonAsset.GetResource("Smallicon/WitchJudgeIcon.png")?.AsImage(115f);

        // 惩罚RPC：使用惩罚，同步到所有客户端
        private static readonly RemoteProcess<(byte judgeId, byte targetId, int usesLeft, float duration)> RpcUsePunish = new(
            "WitchJudge.UsePunish",
            (msg, _) =>
            {
                var judge = GamePlayer.GetPlayer(msg.judgeId);
                if (judge == null) return;
                if (judge.Role is not Instance inst) return;
                var target = GamePlayer.GetPlayer(msg.targetId);
                if (target == null) return;
                inst._usesLeft = msg.usesLeft;
                inst._jailed = target;
                inst._punishExpire = NebulaGameManager.Instance!.CurrentTime + msg.duration;
                inst._locked = true;

                // 魔女诅咒：记录被惩罚玩家及其到期时间（全局追踪）
                JailedUntil[msg.targetId] = NebulaGameManager.Instance.CurrentTime + msg.duration;

                // 订阅杀拦截（仅首次）
                inst.EnsureKillBlockSubscribed();

                // 魔女之眼：审判长本地显示目标阵营信息
                if (judge.AmOwner)
                {
                    string teamLabel = target.IsKiller
                        ? Language.Translate("role.witchJudge.eye.killer")
                        : Language.Translate("role.witchJudge.eye.notKiller");
                    string msg2 = Language.Translate("role.witchJudge.eye.message")
                        .Replace("%PLAYER%", target.Name)
                        .Replace("%TEAM%", teamLabel);
                    PlayerControl.LocalPlayer.RpcSendChat(msg2);
                }
            }
        );

        // 封印RPC：会议中封印被惩罚玩家（参考 Jailor RpcInsulate）
        private static readonly RemoteProcess<byte> RpcInsulate = new(
            "WitchJudge.Insulate",
            (playerId, _) =>
            {
                MeetingHudExtension.AddSealedMask(1 << playerId);
                var player = GamePlayer.GetPlayer(playerId);
                if (player != null && player.AmOwner)
                    MeetingHudExtension.CanUseAbility = false;
                if (MeetingHud.Instance != null)
                    MeetingHud.Instance.ResetPlayerState();
            }
        );

        // === 状态字段 ===
        private GamePlayer? _jailed;
        private float _punishExpire;
        private bool _locked;
        private int _executedImpostorCount;
        public int _usesLeft;

        // 投票记录（用于投票透视）
        private readonly Dictionary<byte, byte?> _voteRecords = new();
        // 杀拦截是否已订阅
        private bool _killBlockSubscribedLocally;
        // 是否已暴露身份
        private bool _exposed;

        private ModAbilityButton? _punishButton;
        private ModAbilityButton? _executeButton;

        // 处刑同步RPC：同步暴露状态到所有客户端
        private static readonly RemoteProcess<(byte judgeId, bool isKiller)> RpcSyncExecute = new(
            "WitchJudge.SyncExecute",
            (msg, _) =>
            {
                var judge = GamePlayer.GetPlayer(msg.judgeId);
                if (judge == null) return;
                if (judge.Role is not Instance inst) return;
                inst._jailed = null;
                inst._locked = false;
                if (!msg.isKiller)
                {
                    inst._exposed = true;
                    inst._usesLeft = 0;
                }
            }
        );

        public Instance(GamePlayer player) : base(player) { }

        // 确保杀拦截已订阅（全局一次）
        private void EnsureKillBlockSubscribed()
        {
            if (_killBlockSubscribedLocally || GameOperatorManager.Instance == null) return;
            _killBlockSubscribedLocally = true;
            GameOperatorManager.Instance.Subscribe<PlayerTryVanillaKillLocalEventAbstractPlayerEvent>(ev =>
            {
                if (NebulaGameManager.Instance == null) return;
                if (JailedUntil.TryGetValue(ev.Player.PlayerId, out var expiry))
                {
                    if (NebulaGameManager.Instance.CurrentTime < expiry)
                    {
                        ev.Cancel(); // 魔女诅咒：被惩罚者无法杀人
                    }
                    else
                    {
                        JailedUntil.Remove(ev.Player.PlayerId);
                    }
                }
            }, this);
        }

        // === 投票翻倍 + 平票驱逐 ===
        [OnlyHost]
        void OnFixVote(PlayerFixVoteHostEvent ev)
        {
            // 审判长投的票算两票，第二票显示为匿名
            if (ev.Player == MyPlayer && ev.DidVote && ev.VoteTo != null)
            {
                ev.Vote = 2;
                MeetingHudExtension.WeightMap[MyPlayer.PlayerId] = 2;
            }
        }

        [OnlyHost]
        void OnTieVote(MeetingTieVoteHostEvent ev)
        {
            // 普通会议平票：开启平票也驱逐，使所有最高票玩家一并出局
            MeetingHudExtension.ExileEvenIfTie = true;
        }

        // 处刑后追踪是否杀的是带刀职业，处决2个带刀职业后独立获胜
        [Local]
        void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Dead.PlayerState != ExecutionState) return;
            if (ev.Murderer != MyPlayer) return;
            if (ev.Dead.Role?.Role?.IsKiller ?? false)
            {
                _executedImpostorCount++;
                if (_executedImpostorCount >= 2 && MyPlayer.IsAlive)
                {
                    int mask = 1 << MyPlayer.PlayerId;
                    NebulaGameEnd.RpcSendGameEnd(HSGTeam.WitchJudgeWin, mask, 0, GameEndReason.Situation, HSGTeam.WitchJudgeWin, GameEndReason.Situation);
                }
            }
        }

        // 活到最后可跟随胜利
        [OnlyMyPlayer]
        void CheckExtraWins(PlayerCheckExtraWinEvent ev)
        {
            if (ev.Phase != ExtraWinCheckPhase.OpportunistPhase) return;
            if (MyPlayer.IsAlive)
            {
                ev.ExtraWinMask.Add(HSGTeam.ExtraWitchJudgeWin);
                ev.IsExtraWin = true;
            }
        }

        // === 投票透视 ===
        [Local]
        void OnVoteCast(PlayerVoteCastEvent ev)
        {
            if (!ShowVoteRecords) return;
            _voteRecords[ev.Voter.PlayerId] = ev.VoteFor?.PlayerId;
        }

        // 会议结束时显示投票记录
        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            // 投票透视
            if (ShowVoteRecords && _voteRecords.Count > 0 && MyPlayer.IsAlive)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(Language.Translate("role.witchJudge.vote.title"));
                foreach (var (voterId, targetId) in _voteRecords)
                {
                    var voter = GamePlayer.GetPlayer(voterId);
                    string voterName = voter?.Name ?? "?";
                    string targetName;
                    if (targetId.HasValue)
                    {
                        var target = GamePlayer.GetPlayer(targetId.Value);
                        targetName = target?.Name ?? "?";
                    }
                    else
                    {
                        targetName = Language.Translate("role.witchJudge.vote.skip");
                    }
                    sb.AppendLine($"{voterName} → {targetName}");
                }
                PlayerControl.LocalPlayer.RpcSendChat(sb.ToString().TrimEnd());
                _voteRecords.Clear();
            }
        }

        // 会议开始时封印被惩罚玩家、强制打开聊天框
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (MyPlayer.IsAlive)
            {
                HudManager.Instance.Chat.SetVisible(true);
            }

            // 魔女诅咒：封印被惩罚玩家的会议能力
            if (_jailed != null && _jailed.IsAlive)
            {
                RpcInsulate.Invoke(_jailed.PlayerId);
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                _usesLeft = PunishMaxUses;
                var tracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                // 惩罚按钮（游戏阶段）
                _punishButton = NebulaAPI.Modules.AbilityButton(
                    this, MyPlayer,
                    VirtualKeyInput.Ability,
                    PunishCooldown,
                    "witchJudge.punish",
                    PunishIcon,
                    (ModAbilityButton _) => tracker.CurrentTarget != null && tracker.CurrentTarget != MyPlayer && !tracker.CurrentTarget.IsDead,
                    (button) => !MyPlayer.IsDead && _usesLeft > 0,
                    false
                );
                _punishButton.OnClick = (button) =>
                {
                    var target = tracker.CurrentTarget;
                    if (target == null || target == MyPlayer || target.IsDead) return;
                    if (_usesLeft <= 0 || _locked) return;
                    int nextUses = _usesLeft - 1;
                    RpcUsePunish.Invoke((MyPlayer.PlayerId, target.PlayerId, nextUses, PunishDuration));
                    _usesLeft = nextUses;
                    _locked = true;
                };

                // 处刑按钮（会议阶段）- 直接处刑，参考 Jailor
                _executeButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, true)
                    .BindKey(VirtualKeyInput.SidekickAction)
                    .SetLabel("witchJudge.execute")
                    .SetLabelType(ModAbilityButton.LabelType.Impostor)
                    .SetColorLabel(MyRole.UnityColor);
                _executeButton.Visibility = _ => MyPlayer.IsAlive && AmongUsUtil.InMeeting && _jailed != null;
                _executeButton.Availability = _ => _jailed != null && _jailed.IsAlive;
                _executeButton.SetImage(ExecuteIcon!);
                _executeButton.OnClick = _ =>
                {
                    if (_jailed == null || _jailed.IsDead) return;
                    var target = _jailed;
                    bool isKiller = target.IsKiller;

                    // 直接处刑（不依赖投票）
                    MyPlayer.MurderPlayer(target, ExecutionState, EventDetail.Kill, KillParameter.MeetingKill, KillCondition.BothAlive);

                    // 同步暴露状态
                    RpcSyncExecute.Invoke((MyPlayer.PlayerId, isKiller));

                    // 清理本地状态
                    _jailed = null;
                    _locked = false;
                    if (!isKiller)
                    {
                        _exposed = true;
                        _usesLeft = 0;
                    }

                    // 结束会议
                    MeetingHud.Instance.RpcClose();
                };
            }
        }

        void IGameOperator.OnReleased() { }

        // === 装饰 ===
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (_exposed)
                name += Language.Translate("role.witchJudge.exposedTag");
        }
    }
}

/// <summary>
/// HSG 内部 Harmony 补丁：让魔女审判长投票翻倍的第二票渲染为匿名灰色。
/// 由于目标类是 internal，通过 GameLoader 用 AccessTools 手动打补丁。
/// </summary>
public static class WitchJudgeVoteIconPatch
{
    // 记录本轮 PopulateResults 中每个玩家已经渲染过的投票图标数
    internal static readonly Dictionary<byte, int> RenderedVoteCounts = new();

    // ModBloopAVoteIcon 的前置补丁
    public static void ModBloopAVoteIconPrefix(ref NetworkedPlayerInfo? voterPlayer)
    {
        if (voterPlayer == null) return;
        byte id = voterPlayer.PlayerId;
        RenderedVoteCounts.TryGetValue(id, out int count);
        RenderedVoteCounts[id] = count + 1;

        // 仅对魔女审判长的第二票及以后的票做匿名处理
        if (count > 0 && IsWitchJudgeDoubleVote(id))
            voterPlayer = null;
    }

    // PopulateResults 的前置补丁，用于重置计数器
    public static void PopulateResultsPrefix()
    {
        RenderedVoteCounts.Clear();
    }

    private static bool IsWitchJudgeDoubleVote(byte playerId)
    {
        var player = GamePlayer.GetPlayer(playerId);
        if (player?.Role?.Role != WitchJudge.MyRole) return false;
        return MeetingHudExtension.WeightMap.GetValueOrDefault(playerId, 1) > 1;
    }
}

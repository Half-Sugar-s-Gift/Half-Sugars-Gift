/*
 * 赛博佛祖 镇楼
 * 永无BUG
 * 
 *                  _ooOoo_
 *                 o8888888o
 *                 88" . "88
 *                 (| -_- |)
 *                 O\  =  /O
 *              ____/`---'\____
 *            .'  \\|     |//  `.
 *           /  \\|||  :  |||//  \
 *          /  _||||| -:- |||||_  \
 *          |   | \\\  -  /// |   |
 *          | \_|  ''\---/''  |_/ |
 *          \  .-\__  `-`  ___/-. /
 *        ___`. .'  /--.--\  `. .'___
 *      ."" '<  `.___\_<|>_/___.' >' "".
 *     | | :  `- \`.;`\ _ /`;.`/ - ` : | |
 *     \  \ `-.   \_ __\ /__ _/   .-` /  /
 *======`-.____`-.___\_____/___.-`____.-'======
 *                   `=---='
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 *          菩提本无树    明镜亦非台
 *          本来无BUG    何必常修改
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
namespace HalfSugarGift.Core.Settings;

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
[NebulaRPCHolder]
public class MoreVoteSettings : AbstractModule<Game>, IGameOperator
{
    bool _hasAdjusted = false;
    bool _inVotingPhase = false;
    float _votePhaseStartTime = 0f;
    int _voteTotalTime = 0;

    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new MoreVoteSettings());
    }
    protected override void OnInjected(Game container) => this.Register(container);

    public static BoolConfiguration EnableVoteTimeChange = NebulaAPI.Configurations.Configuration(
        "options.hsg.mvs.enablevotetimechange", false);

    public static IntegerConfiguration TriggerCount = NebulaAPI.Configurations.Configuration(
        "options.hsg.mvs.triggercount", (1, 24), 1, () => EnableVoteTimeChange);

    public static FloatConfiguration VoteDuration = NebulaAPI.Configurations.Configuration(
        "options.hsg.mvs.voteduration", (5f, 120f, 5f), 10f,
        FloatConfigurationDecorator.Second, () => EnableVoteTimeChange);

    public static IntegerConfiguration DisableOnThresholdReached = NebulaAPI.Configurations.Configuration(
        "options.hsg.mvs.disableonthresholdreached", (1, 24), 5, () => EnableVoteTimeChange);

    public static IntegerConfiguration VoteTime = NebulaAPI.Configurations.Configuration(
        "options.hsg.mvs.votetime", (0, 600, 15), 225, () => EnableVoteTimeChange);

    [OnlyHost]
    public void OnMeetingStart(MeetingStartEvent ev)
    {
        _hasAdjusted = false;
        _inVotingPhase = false;
        _votePhaseStartTime = 0f;
        _voteTotalTime = VoteTime;
        if (!EnableVoteTimeChange) return;
        NebulaManager.Instance.StartCoroutine(WaitForVotingPhase().WrapToIl2Cpp());
    }

    IEnumerator WaitForVotingPhase()
    {
        HsgDebug.Log("[MVS] : 等待投票阶段开始");
        while (MeetingHud.Instance == null || MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion)
            yield return null;

        HsgDebug.Log($"[MVS] : 讨论已结束，state为{MeetingHud.Instance?.state}");

        while (MeetingHud.Instance != null &&
               MeetingHud.Instance.state != MeetingHud.VoteStates.NotVoted &&
               MeetingHud.Instance.state != MeetingHud.VoteStates.Voted)
        {
            yield return null;
        }

        HsgDebug.Log($"[MVS] : 已进入投票阶段，state为{MeetingHud.Instance?.state}");

        if (!AmongUsClient.Instance.AmHost)
        {
            HsgDebug.Log("[MVS] : 非房主，退出");
            yield break;
        }

        _votePhaseStartTime = Time.time;
        CheckAndAdjust();
    }

    [OnlyHost]
    void OnPlayerDieOrDisconnect(PlayerDieEvent ev)
    {
        if (_inVotingPhase && !_hasAdjusted && AmongUsClient.Instance.AmHost)
            CheckAndAdjust();
    }

    [OnlyHost]
    void OnPlayerDisconnect(PlayerDisconnectEvent ev)
    {
        if (_inVotingPhase && !_hasAdjusted && AmongUsClient.Instance.AmHost)
            CheckAndAdjust();
    }

    [OnlyHost]
    void OnPlayerVote(PlayerVoteCastEvent ev)
    {
        if (_inVotingPhase && !_hasAdjusted && AmongUsClient.Instance.AmHost)
            CheckAndAdjust();
    }

    private void CheckAndAdjust()
    {
        HsgDebug.Log("[MVS] : CheckAndAdjust调用");

        if (_hasAdjusted)
        {
            HsgDebug.Log("[MVS] : _hasAdjusted为true，退出");
            return;
        }
        if (!EnableVoteTimeChange)
        {
            HsgDebug.Log("[MVS] : 未启用配置项。退出");
            return;
        }

        int totalAlive = 0;
        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            HsgDebug.Log("[MVS] : MeetingHud.Instance为null。");
            return;
        }

        HsgDebug.Log($"[MVS] : playerStates数量为{meeting.playerStates.Count}");

        foreach (var state in meeting.playerStates)
        {
            if (state == null) continue;
            var player = GamePlayer.GetPlayer(state.TargetPlayerId);
            if (player == null) continue;
            if (player.IsDead || player.IsDisconnected) continue;
            totalAlive++;
        }

        HsgDebug.Log($"[MVS] : 总存活人数为{totalAlive}");
        int threshold = DisableOnThresholdReached;
        if (totalAlive <= threshold)
        {
            HsgDebug.Log($"[MVS] : 达到阈值。不调整");
            return;
        }
        float currentTime = GetCurrentVoteTime();
        float targetTime = VoteDuration;
        HsgDebug.Log($"[MVS] : 当前投票时间为{currentTime}秒，目标时长为{targetTime}秒");

        if (currentTime <= targetTime)
        {
            HsgDebug.Log($"[MVS] : 当前时间小于等于目标，不调整");
            return;
        }
        var meetingObj = NebulaAPI.CurrentGame?.CurrentMeeting;
        if (meetingObj != null)
        {
            float current = GetCurrentVoteTime();
            float target = VoteDuration;
            if (current > target)
            {
                int steps = (int)Mathf.Ceil(current - target);
                int maxSteps = 1000;
                steps = Math.Min(steps, maxSteps);
                int reduced = 0;
                while (reduced < steps)
                {
                    var meetings = NebulaAPI.CurrentGame?.CurrentMeeting;
                    if (meetings == null) break;
                    meetings.EditMeetingTime(-1);
                    MeetingHud.Instance?.ResetPlayerState();
                    reduced++;
                }
                _hasAdjusted = true;
                HsgDebug.Log($"[MVS] : 减少{reduced}秒");
            }
        }
        else
        {
            HsgDebug.Log("[MVS] : CurrentMeeting为空");
        }
    }

    float GetCurrentVoteTime()
    {
        if (_votePhaseStartTime <= 0f) return 0f;
        float elapsed = Time.time - _votePhaseStartTime;
        return Mathf.Max(0f, _voteTotalTime - elapsed);
    }

    [OnlyHost]
    public void OnMeetingEnd(MeetingEndEvent ev)
    {
        _inVotingPhase = false;
        _hasAdjusted = false;
        _votePhaseStartTime = 0f;
    }
}
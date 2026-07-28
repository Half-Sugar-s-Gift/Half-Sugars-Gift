using System.Collections;
using System.Linq;
using UnityEngine;
using Virial.Events.Game;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 阶段二管理器 - 障碍赛阶段
/// 初始化体温系统、分配任务、监控任务完成、触发登船流程
/// </summary>
[NebulaRPCHolder]
public class PhaseTwoManager : IGameOperator
{
    private static PhaseTwoManager? instance;

    private bool phaseActive = false;
    private bool tasksCompleted = false;
    private bool boardingStarted = false;
    private float boardingCountdown = 0f;

    // 登录艇中心坐标（玩家需到达此区域登船）
    // TODO: 根据实际地图精确调整
    private static readonly Vector2 ShuttlePosition = new(0f, 0f);
    private const float ShuttleRadius = 5f;

    /// <summary>
    /// 启动阶段二
    /// </summary>
    public static void Begin()
    {
        if (instance != null) return;

        instance = new PhaseTwoManager();
        instance.StartPhase();
    }

    private void StartPhase()
    {
        // 初始化并重置体温系统
        TemperatureSystem.Initialize();
        TemperatureSystem.Reset();

        // 分配任务（通过 RPC 让每名玩家自行设定）
        RpcAssignPhaseTwoTasks.Invoke();

        // 订阅每帧更新
        GameOperatorManager.Instance?.Subscribe<GameHudUpdateEvent>(OnHudUpdate, NebulaAPI.CurrentGame!);

        // 显示阶段标题
        var (title, subtitle) = TitleCardUI.GetPhaseTitle(1);
        TitleCardUI.ShowTitle(title, subtitle);

        phaseActive = true;
        HsgDebug.Log("[Phase2] 障碍赛阶段开始");
    }

    /// <summary>
    /// RPC：要求所有存活船员将任务替换为 3 个（修复电线→短任务、校准发动机+上传数据→长任务）
    /// </summary>
    private static readonly RemoteProcess RpcAssignPhaseTwoTasks = new("SWPhase2AssignTasks", _ =>
    {
        var local = GamePlayer.LocalPlayer;
        if (local == null || local.IsDead) return;
        if (local.Role?.Role.Category != Virial.Assignable.RoleCategory.CrewmateRole) return;

        local.Tasks.Unbox().ReplaceTasks(1, 2);
        HsgDebug.Log("[Phase2] 已为船员分配 3 个障碍赛任务");
    });

    private void OnHudUpdate(GameHudUpdateEvent ev)
    {
        if (!phaseActive) return;

        // 每帧更新体温
        TemperatureSystem.Update(ev.DeltaTime);

        // 检查任务完成状态
        if (!tasksCompleted)
            CheckTaskCompletion();

        // 登船倒计时
        if (boardingStarted)
        {
            boardingCountdown -= ev.DeltaTime;
            if (boardingCountdown <= 0f)
                ExecuteBoarding();
        }
    }

    private void CheckTaskCompletion()
    {
        // 所有存活船员是否都完成了当前任务
        bool allDone = GamePlayer.AllPlayers
            .Where(p => !p.IsDead && p.Role?.Role.Category == Virial.Assignable.RoleCategory.CrewmateRole)
            .All(p => p.Tasks.IsCompletedCurrentTasks);

        if (!allDone) return;

        tasksCompleted = true;
        OnAllTasksCompleted();
    }

    private void OnAllTasksCompleted()
    {
        HsgDebug.Log("[Phase2] 所有船员已完成障碍赛任务");
        // 配置密码面板：单人模式，位于登录艇区域
        PasswordPanel.OnlyOneNeeded = true;
        PasswordPanel.SpawnRoom = SystemTypes.Storage;
        PasswordPanel.SpawnPosition = new Vector3(0f, 0f, -0.5f);
        // 激活登录艇区域的密码面板
        PasswordPanel.Activate();
        PasswordPanel.OnAllPassed += OnPasswordAllPassed;
    }

    private void OnPasswordAllPassed()
    {
        StartBoardingCountdown();
        PasswordPanel.OnAllPassed -= OnPasswordAllPassed;
    }

    /// <summary>
    /// 密码正确后调用，开始 30 秒登船倒计时
    /// </summary>
    public void StartBoardingCountdown()
    {
        if (boardingStarted) return;
        boardingStarted = true;
        boardingCountdown = 30f;

        PatchManager.SendNormalMessage("密码已确认！30秒后登录艇起飞！");
        HsgDebug.Log("[Phase2] 登船倒计时开始：30秒");
    }

    /// <summary>
    /// 倒计时结束：未在登录艇区域的玩家死亡，存活者进入阶段三
    /// </summary>
    private void ExecuteBoarding()
    {
        HsgDebug.Log("[Phase2] 登船倒计时结束，执行登船判定");

        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;

            float dist = Vector2.Distance(player.TruePosition, ShuttlePosition);
            if (dist > ShuttleRadius)
            {
                // 未及时到达者死亡
                HsgDebug.Log($"[Phase2] 玩家 {player.PlayerId} 未及时到达登录艇，已死亡");
                player.Suicide(PlayerState.Suicide, EventDetail.Kill, KillParameter.NormalKill);
            }
        }

        // 当前阶段结束
        phaseActive = false;
        TemperatureSystem.Cleanup();

        // 启动阶段三
        var phaseThree = new PhaseThreeManager();
        phaseThree.StartPhase();

        instance = null;
    }

    void IGameOperator.OnReleased()
    {
        TemperatureSystem.Cleanup();
        if (instance == this) instance = null;
    }
}

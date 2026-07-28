using Nebula.Modules.ScriptComponents;
using Virial.Assignable;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 阶段一管理器：氧气倒计时、共享任务、内鬼通风管限时、内鬼全黑外观、禁止 sabotage、密码阶段管理。
/// 使用 GameOperator 模式，自动发现事件处理方法。
/// </summary>
internal class PhaseOneManager : FlexibleLifespan, IGameOperator
{
    // ===== 氧气计时器 =====
    private TimerImpl oxygenTimer;

    // ===== 内鬼通风管追踪 =====
    private Dictionary<byte, float> impostorOutdoorTime = new();

    // ===== 密码阶段 =====
    private bool passwordPhaseActive;

    // ===== 阶段状态 =====
    private bool isActive;

    /// <summary>氧气耗尽时触发（全员死亡判定）</summary>
    public event Action? OnOxygenDepleted;

    /// <summary>阶段一完成时触发</summary>
    public event Action? OnPhaseComplete;

    public PhaseOneManager()
    {
        oxygenTimer = new TimerImpl(0f, StarWreckEscapeConfig.OxygenInitialTime);
        oxygenTimer.SetPredicate(() => true);
    }

    /// <summary>
    /// 启动阶段一：分配共享任务、开始氧气倒计时。
    /// </summary>
    public void StartPhase()
    {
        if (isActive) return;
        isActive = true;
        passwordPhaseActive = false;

        // 初始化氧气计时器
        oxygenTimer.Start();

        // 将氧气计时器注册为游戏操作符（自动触发 Update 事件）
        oxygenTimer.Register(this);

        HsgDebug.Log($"PhaseOneManager: 阶段一启动，氧气初始 {StarWreckEscapeConfig.OxygenInitialTime}s");

        // 直接分配共享任务（不显示标题卡片，避免遮盖画面）
        SharedTaskPool.AssignTasks();

        // 订阅共享任务完成事件（增加氧气时间）
        SharedTaskPool.OnAllTasksCompleted += OnSharedTasksDone;
    }

    /// <summary>
    /// 游戏开始事件：启动阶段一逻辑。
    /// </summary>
    void OnGameStart(GameStartEvent ev)
    {
        StartPhase();
    }

    /// <summary>
    /// 每帧更新：内鬼通风管追踪、密码面板更新、氧气耗尽检测。
    /// </summary>
    void Update(UpdateEvent ev)
    {
        if (!isActive) return;

        float dt = ev.DeltaTime;

        // 更新密码面板倒计时
        PasswordPanel.Update(dt);

        // 检查氧气是否耗尽
        if (oxygenTimer.CurrentTime <= 0f && oxygenTimer.IsProgressing)
        {
            oxygenTimer.StopForcely();
            HsgDebug.Log("PhaseOneManager: 氧气耗尽！");
            OnOxygenDepleted?.Invoke();
        }

        // 追踪内鬼是否在通风管外
        TrackImpostorOutdoor(dt);
    }

    /// <summary>
    /// 玩家完成任务事件：每完成一个任务增加氧气时间。
    /// </summary>
    void OnPlayerTaskComplete(PlayerTaskCompleteEvent ev)
    {
        if (!isActive) return;
        oxygenTimer.Expand(StarWreckEscapeConfig.OxygenPerTask);
        HsgDebug.Log($"PhaseOneManager: 玩家 {ev.Player.PlayerId} 完成任务，氧气增加 {StarWreckEscapeConfig.OxygenPerTask}s");
    }

    /// <summary>
    /// 玩家进入/离开通风管事件：记录内鬼在通风管外的时间。
    /// </summary>
    [OnlyMyPlayer]
    void OnPlayerMove(PlayerUpdateVentStateLocalEvent ev)
    {
        // 此处仅作为事件占位；实际追踪在 Update 中基于位置检测
    }

    /// <summary>
    /// 内鬼全黑外观：使内鬼在自身视角和其他玩家视角中呈现全黑色。
    /// 通过将内鬼设为不可见实现"全黑"视觉效果（其他人看内鬼为黑色轮廓）。
    /// </summary>
    void OnPlayerVisibility(PlayerUpdateVisibilityEvent ev)
    {
        if (!isActive) return;
        // 内鬼全部设为不可见（全黑外观）
        if (ev.Player.Role.Role.Category == RoleCategory.ImpostorRole)
            ev.SetInvisible();
    }

    /// <summary>
    /// 禁止内鬼使用 sabotage：在 HUD 更新时禁用 sabotage 按钮。
    /// 内鬼的破坏能力在本阶段被完全封锁。
    /// </summary>
    void OnHudUpdate(GameHudUpdateEvent ev)
    {
        if (!isActive) return;
        // 本阶段禁止所有 sabotage
        if (MapBehaviour.Instance != null && MapBehaviour.Instance.IsOpen)
        {
            MapBehaviour.Instance.infectedOverlay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 当共享任务全部完成时，进入密码阶段。
    /// </summary>
    private void OnSharedTasksDone()
    {
        if (!isActive) return;
        HsgDebug.Log("PhaseOneManager: 共享任务全部完成，进入密码阶段");

        // 启动密码面板
        passwordPhaseActive = true;
        PasswordPanel.OnlyOneNeeded = false;
        PasswordPanel.SpawnRoom = SystemTypes.Cafeteria;
        PasswordPanel.SpawnPosition = new Vector3(3.0f, -2.0f, -0.5f);
        PasswordPanel.Activate();

        PasswordPanel.OnPasswordTimeout += OnPasswordTimeout;
        PasswordPanel.OnAllPassed += OnPasswordAllPassed;
    }

    /// <summary>
    /// 密码输入超时：未通过密码验证的玩家死亡。
    /// </summary>
    private void OnPasswordTimeout()
    {
        if (!isActive) return;
        HsgDebug.Log("PhaseOneManager: 密码时间到，处决未通过玩家");

        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;
            if (PasswordPanel.HasPlayerPassed(player.PlayerId)) continue;
            // 标记为惩罚死亡
            player.Suicide(PlayerStates.Punished, null, KillParameter.NormalKill);
        }
    }

    /// <summary>
    /// 所有玩家通过密码，阶段一完成。
    /// </summary>
    private void OnPasswordAllPassed()
    {
        if (!isActive) return;
        HsgDebug.Log("PhaseOneManager: 所有玩家通过密码，阶段一完成");
        CompletePhase();
    }

    /// <summary>
    /// 结束阶段一，清理资源。
    /// </summary>
    private void CompletePhase()
    {
        if (!isActive) return;
        isActive = false;
        passwordPhaseActive = false;

        // 取消事件订阅
        SharedTaskPool.OnAllTasksCompleted -= OnSharedTasksDone;
        PasswordPanel.OnPasswordTimeout -= OnPasswordTimeout;
        PasswordPanel.OnAllPassed -= OnPasswordAllPassed;

        // 释放氧气计时器
        oxygenTimer.Release();

        HsgDebug.Log("PhaseOneManager: 阶段一结束");
        OnPhaseComplete?.Invoke();
    }

    /// <summary>
    /// 追踪内鬼是否在通风管外，超过 ImpostorOutdoorTimeout 则杀死内鬼。
    /// </summary>
    private void TrackImpostorOutdoor(float deltaTime)
    {
        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;
            if (player.Role.Role.Category != RoleCategory.ImpostorRole) continue;

            // 获取玩家对应的 PlayerControl
            PlayerControl? pc = null;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == player.PlayerId) { pc = p; break; }
            }
            if (pc == null) continue;

            var playerId = player.PlayerId;
            bool inVent = pc.inVent;

            if (inVent)
            {
                // 在通风管内，重置计时
                if (impostorOutdoorTime.ContainsKey(playerId))
                    impostorOutdoorTime.Remove(playerId);
            }
            else
            {
                // 在通风管外，累计时间
                if (!impostorOutdoorTime.ContainsKey(playerId))
                    impostorOutdoorTime[playerId] = 0f;

                impostorOutdoorTime[playerId] += deltaTime;
                float timeout = StarWreckEscapeConfig.ImpostorOutdoorTimeout;

                if (impostorOutdoorTime[playerId] >= timeout)
                {
                    HsgDebug.Log($"PhaseOneManager: 内鬼 {playerId} 在通风管外超过 {timeout}s，执行处决");
                    player.Suicide(PlayerStates.Punished, null, KillParameter.NormalKill);
                    impostorOutdoorTime.Remove(playerId);
                }
            }
        }
    }

    /// <summary>检查是否内鬼在通风管外超时</summary>
    public float GetImpostorOutdoorTime(byte playerId)
    {
        if (impostorOutdoorTime.TryGetValue(playerId, out var time))
            return time;
        return 0f;
    }

    /// <summary>获取当前氧气剩余时间</summary>
    public float OxygenRemaining => oxygenTimer.CurrentTime;

    /// <summary>阶段一是否激活中</summary>
    public bool IsActive => isActive;

    /// <summary>密码阶段是否激活中</summary>
    public bool IsPasswordPhase => passwordPhaseActive;

    /// <summary>
    /// 当 GameOperator 的 lifespan 结束时自动清理。
    /// </summary>
    void IGameOperator.OnReleased()
    {
        isActive = false;
        SharedTaskPool.OnAllTasksCompleted -= OnSharedTasksDone;
        PasswordPanel.OnPasswordTimeout -= OnPasswordTimeout;
        PasswordPanel.OnAllPassed -= OnPasswordAllPassed;
    }
}

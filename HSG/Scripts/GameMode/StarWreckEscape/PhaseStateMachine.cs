namespace hvtXsvc.GameMode.StarWreckEscape;

using Nebula.Game;

/// <summary>
/// StarWreckEscape 的阶段枚举
/// </summary>
internal enum StarWreckPhase
{
    Inactive,
    PhaseOne,   // 重启飞船 - Skeld
    PhaseTwo,   // 障碍赛 - Polus
    PhaseThree  // 孢子黎明 - Fungle
}

/// <summary>
/// 阶段状态机，管理 StarWreckEscape 整个游戏模式的阶段流转
/// </summary>
internal class PhaseStateMachine
{
    public static StarWreckPhase CurrentPhase { get; private set; } = StarWreckPhase.Inactive;

    // 持有各阶段管理器实例（PhaseTwoManager 内部自管理单例，无需外部持有）
    private static PhaseOneManager? phaseOneManager;
    private static PhaseThreeManager? phaseThreeManager;

    /// <summary>
    /// 游戏开始，启动阶段一
    /// </summary>
    public static void StartGame()
    {
        // 防重复调用
        if (CurrentPhase != StarWreckPhase.Inactive) return;

        CurrentPhase = StarWreckPhase.PhaseOne;

        // 创建并启动阶段一
        phaseOneManager = new PhaseOneManager();

        // 注册到 GameOperator 系统，使其事件处理方法被自动发现
        GameOperatorManager.Instance?.Subscribe(phaseOneManager, phaseOneManager);

        // 订阅阶段一事件
        phaseOneManager.OnPhaseComplete += TransitionToPhaseTwo;
        phaseOneManager.OnOxygenDepleted += OnOxygenDepleted;

        // 预加载所有地图（内部只激活 Skeld）
        MapPreloader.PreloadAll();

        // 直接启动阶段一（不显示标题卡片，避免遮盖原版 intro 过渡）
        phaseOneManager.StartPhase();
    }

    /// <summary>
    /// 氧气耗尽处理：所有存活玩家死亡
    /// </summary>
    private static void OnOxygenDepleted()
    {
        foreach (var player in GamePlayer.AllPlayers)
        {
            if (!player.IsDead)
                player.Suicide(PlayerStates.Punished, null, KillParameter.NormalKill);
        }
    }

    /// <summary>
    /// 阶段一 → 阶段二过渡
    /// </summary>
    public static void TransitionToPhaseTwo()
    {
        var (title, subtitle) = TitleCardUI.GetPhaseTitle(1);
        TitleCardUI.ShowTitle(title, subtitle, () =>
        {
            CurrentPhase = StarWreckPhase.PhaseTwo;
            MapPreloader.SwitchTo(2); // Polus
            PhaseTwoManager.Begin();
        });
    }

    /// <summary>
    /// 阶段二 → 阶段三过渡
    /// </summary>
    public static void TransitionToPhaseThree()
    {
        var (title, subtitle) = TitleCardUI.GetPhaseTitle(2);
        TitleCardUI.ShowTitle(title, subtitle, () =>
        {
            CurrentPhase = StarWreckPhase.PhaseThree;
            MapPreloader.SwitchTo(5); // Fungle

            phaseThreeManager = new PhaseThreeManager();
            GameOperatorManager.Instance?.Subscribe(phaseThreeManager, NebulaAPI.CurrentGame!);
            phaseThreeManager.StartPhase();
        });
    }

    /// <summary>
    /// 阶段三结束，触发游戏结束判定
    /// </summary>
    /// <param name="crewWin">true=船员胜利，false=内鬼胜利</param>
    public static void EndGame(bool crewWin)
    {
        CurrentPhase = StarWreckPhase.Inactive;

        var game = NebulaAPI.CurrentGame;
        if (game == null) return;

        if (crewWin)
            game.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Special);
        else
            game.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Special);
    }
}

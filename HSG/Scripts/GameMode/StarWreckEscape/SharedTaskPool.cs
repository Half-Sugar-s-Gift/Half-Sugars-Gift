namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 全局共享任务池：所有玩家共用一组任务计数，单人完成全员受益。
/// 任务类型：修复电线、上传数据、校准发动机、重启反应堆、校准航线、扫描、倒垃圾。
/// </summary>
internal static class SharedTaskPool
{
    /// <summary>剩余共享任务数</summary>
    private static int totalTasksRemaining;

    /// <summary>共享任务池定义</summary>
    private static readonly TaskTypes[] TaskPool = new[]
    {
        TaskTypes.FixWiring,
        TaskTypes.UploadData,
        TaskTypes.CalibrateDistributor,
        TaskTypes.ResetReactor,
        TaskTypes.ClearAsteroids,
        TaskTypes.FixWiring,
        TaskTypes.EmptyGarbage
    };

    /// <summary>所有共享任务完成时触发</summary>
    public static event Action? OnAllTasksCompleted;

    /// <summary>
    /// 为所有存活玩家分配共享任务。
    /// 替换他们原有的任务，每人获得 TaskPool 中的一组任务。
    /// </summary>
    public static void AssignTasks()
    {
        totalTasksRemaining = TaskPool.Length;

        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;
            var tasks = player.Tasks;
            if (tasks == null) continue;
            // 替换玩家当前任务为共享任务：各 1 个短任务（TaskPool 中的任务作为 short 任务分配）
            tasks.ReplaceTasks(0, TaskPool.Length, 0);
        }

        HsgDebug.Log($"SharedTaskPool: 已分配 {TaskPool.Length} 个共享任务，剩余 {totalTasksRemaining}");

        // 注册任务完成监听
        GameOperatorManager.Instance?.Subscribe<PlayerTaskCompleteEvent>(OnTaskCompleted, NebulaAPI.CurrentGame!);
    }

    /// <summary>
    /// 拦截玩家任务完成事件，减少全局计数。
    /// </summary>
    private static void OnTaskCompleted(PlayerTaskCompleteEvent ev)
    {
        if (totalTasksRemaining <= 0) return;
        totalTasksRemaining--;
        HsgDebug.Log($"SharedTaskPool: 玩家 {ev.Player.PlayerId} 完成一个共享任务，剩余 {totalTasksRemaining}");

        // 广播同步剩余任务计数
        RpcSyncTask.Invoke(totalTasksRemaining);

        if (totalTasksRemaining <= 0)
        {
            HsgDebug.Log("SharedTaskPool: 所有共享任务已完成！");
            OnAllTasksCompleted?.Invoke();
        }
    }

    /// <summary>
    /// RPC：同步剩余共享任务计数到所有客户端
    /// </summary>
    private static readonly RemoteProcess<int> RpcSyncTask = new(
        "StarWreckSharedTaskSync",
        (remaining, _) =>
        {
            totalTasksRemaining = remaining;
            HsgDebug.Log($"SharedTaskPool: RPC 同步剩余任务数 = {remaining}");
        }
    );

    /// <summary>获取当前剩余任务数</summary>
    public static int RemainingTasks => totalTasksRemaining;
}

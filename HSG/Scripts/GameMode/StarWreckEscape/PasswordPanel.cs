using Nebula.Extensions;
using System;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 密码面板系统：生成随机 4 位数字密码，在食堂创建交互控制台，
/// 玩家靠近输入即可"通过"，倒计时结束未输入者死亡。
/// </summary>
internal static class PasswordPanel
{
    /// <summary>当前密码（4 位数字字符串）</summary>
    public static string CurrentPassword { get; private set; } = "";

    /// <summary>已通过密码验证的玩家 ID 集合</summary>
    private static HashSet<byte> passedPlayers = new();

    /// <summary>密码输入剩余时间</summary>
    private static float timeRemaining;

    /// <summary>密码面板是否激活</summary>
    public static bool IsActive { get; private set; }

    /// <summary>密码输入超时事件</summary>
    public static event Action? OnPasswordTimeout;

    /// <summary>所有玩家通过（或单人模式下首人通过）时触发</summary>
    public static event Action? OnAllPassed;

    /// <summary>仅需 1 人通过即触发 OnAllPassed（用于阶段二）</summary>
    public static bool OnlyOneNeeded { get; set; } = false;

    /// <summary>密码面板生成的目标房间</summary>
    public static SystemTypes SpawnRoom { get; set; } = SystemTypes.Cafeteria;

    /// <summary>密码面板生成的目标位置</summary>
    public static Vector3 SpawnPosition { get; set; } = new(3.0f, -2.0f, -0.5f);

    /// <summary>
    /// 生成新密码并激活密码面板。
    /// </summary>
    public static void Activate()
    {
        // 生成随机 4 位数字密码
        CurrentPassword = "";
        for (int i = 0; i < 4; i++)
            CurrentPassword += UnityEngine.Random.Range(0, 10).ToString();

        timeRemaining = StarWreckEscapeConfig.PasswordInputTime;
        IsActive = true;
        passedPlayers.Clear();

        HsgDebug.Log($"PasswordPanel: 密码已生成 = {CurrentPassword}");

        // 在食堂创建密码交互控制台
        CreatePasswordConsole();

        // 显示密码给所有玩家
        ShowPasswordToAll();

        // 注册任务完成事件监听，检测玩家是否使用了密码控制台
        GameOperatorManager.Instance?.Subscribe<PlayerTaskCompleteEvent>(OnPlayerInteractConsole, NebulaAPI.CurrentGame!);
    }

    /// <summary>
    /// 向所有存活玩家显示当前密码（通过本地消息）。
    /// </summary>
    private static void ShowPasswordToAll()
    {
        string msg = $"【密码面板】本阶段密码: {CurrentPassword}  剩余时间: {Mathf.CeilToInt(timeRemaining)}s";
        PatchManager.SendLocalMessage(msg);
    }

    /// <summary>
    /// 在食堂（Cafeteria）创建密码输入控制台。
    /// 复用氧气室面板位置作为视觉参考。
    /// </summary>
    private static void CreatePasswordConsole()
    {
        var ship = AmongUsLLImpl.ShipStatusInstance;
        if (ship == null) return;

        // 根据 SpawnRoom 和 SpawnPosition 动态放置密码面板
        GameObject obj = new GameObject("PasswordPanelConsole");
        obj.transform.SetParent(ship.FastRooms.TryGetValue(SpawnRoom, out var room) ? room.transform : ship.transform);
        obj.transform.localPosition = SpawnPosition;

        // 添加 SpriteRenderer：复用氧气面板的 sprite
        var renderer = obj.AddComponent<SpriteRenderer>();
        var lifeSuppConsole = ship.AllConsoles.FirstOrDefault(c => c.Room == SystemTypes.LifeSupp);
        if (lifeSuppConsole != null && lifeSuppConsole.Image != null)
            renderer.sprite = lifeSuppConsole.Image.sprite;

        // 配置为可交互 Console
        Console console = obj.AddComponent<Console>();
        console.checkWalls = true;
        console.usableDistance = 1.0f;
        console.TaskTypes = new[] { TaskTypes.FixWiring }; // 复用接线任务类型作为交互触发
        console.ConsoleId = 100; // 专用 ID，区别于原有控制台
        console.Room = SystemTypes.Cafeteria;
        console.Image = renderer;
        console.ValidTasks = Array.Empty<TaskSet>();

        // 添加到 ShipStatus 的 AllConsoles
        var list = ship.AllConsoles.ToList();
        list.Add(console);
        ship.AllConsoles = list.ToArray();

        // 添加碰撞体和 PassiveButton
        var collider = obj.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;
        collider.isTrigger = true;

        var button = obj.AddComponent<PassiveButton>();
        button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        button._CachedZ_k__BackingField = 0.1f;
        button.CachedZ = 0.1f;

        obj.layer = LayerMask.NameToLayer("ShortObjects");

        HsgDebug.Log("PasswordPanel: 控制台已在食堂创建");
    }

    /// <summary>
    /// 玩家与控制台交互时调用。
    /// </summary>
    private static void OnPlayerInteractConsole(PlayerTaskCompleteEvent ev)
    {
        if (!IsActive) return;
        var playerId = ev.Player.PlayerId;
        if (passedPlayers.Contains(playerId)) return;

        // 标记该玩家已通过密码验证
        passedPlayers.Add(playerId);
        HsgDebug.Log($"PasswordPanel: 玩家 {playerId} 已通过密码验证");

        // 单人模式：首人通过即触发
        if (OnlyOneNeeded)
        {
            IsActive = false;
            OnAllPassed?.Invoke();
            return;
        }

        // 多人模式：检查是否所有存活玩家都已通过
        CheckAllPassed();
    }

    /// <summary>
    /// 每帧更新，处理密码输入倒计时。
    /// </summary>
    public static void Update(float deltaTime)
    {
        if (!IsActive) return;
        if (timeRemaining <= 0f) return;

        timeRemaining -= deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            IsActive = false;
            HsgDebug.Log("PasswordPanel: 密码输入时间到！");
            OnPasswordTimeout?.Invoke();
        }
    }

    /// <summary>
    /// 检查是否所有存活玩家都已通过密码验证。
    /// </summary>
    private static void CheckAllPassed()
    {
        bool allPassed = true;
        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;
            if (!passedPlayers.Contains(player.PlayerId))
            {
                allPassed = false;
                break;
            }
        }

        if (allPassed)
        {
            IsActive = false;
            HsgDebug.Log("PasswordPanel: 所有玩家已通过密码！");
            OnAllPassed?.Invoke();
        }
    }

    /// <summary>获取剩余输入时间</summary>
    public static float TimeRemaining => timeRemaining;

    /// <summary>指定玩家是否已通过密码</summary>
    public static bool HasPlayerPassed(byte playerId) => passedPlayers.Contains(playerId);

    /// <summary>获取当前通过的玩家数</summary>
    public static int PassedCount => passedPlayers.Count;

    /// <summary>
    /// 直接标记某个玩家已通过密码（用于 RPC 同步等场景）。
    /// </summary>
    public static void MarkPlayerPassed(byte playerId)
    {
        passedPlayers.Add(playerId);
    }
}

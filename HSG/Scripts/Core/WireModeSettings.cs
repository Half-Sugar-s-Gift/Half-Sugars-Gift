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

/// <summary>
/// 钢丝模式：所有玩家各自出生在一个房间，房间以玩家颜色染色
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
[NebulaRPCHolder]
public class WireModeSettings : AbstractModule<Game>, IGameOperator
{
    /// <summary>
    /// 是否启用钢丝模式
    /// </summary>
    public static BoolConfiguration EnableWireMode = NebulaAPI.Configurations.Configuration(
        "options.hsg.wireMode.enable", false);

    /// <summary>
    /// 房间染色透明度（0-1）
    /// </summary>
    public static FloatConfiguration RoomColorAlpha = NebulaAPI.Configurations.Configuration(
        "options.hsg.wireMode.roomAlpha", (0.05f, 0.5f, 0.05f), 0.2f,
        FloatConfigurationDecorator.None, () => EnableWireMode);

    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new WireModeSettings());
    }

    protected override void OnInjected(Game container) => this.Register(container);

    /// <summary>
    /// Skeld 14 个可用房间的出生坐标（来源：NebulaPreSpawnLocation）
    /// </summary>
    private static readonly (SystemTypes room, Vector2 pos)[] SkeldRooms =
    [
        (SystemTypes.Admin,       new(2.9753f,   -7.4595f)),
        (SystemTypes.Cafeteria,   new(-0.8721f,   3.6115f)),
        (SystemTypes.Comms,       new(4.5986f,  -15.618f)),
        (SystemTypes.Electrical,  new(-7.6091f,  -8.7664f)),
        (SystemTypes.LifeSupp,    new(6.5236f,   -3.5375f)),
        (SystemTypes.LowerEngine, new(-17.1282f, -13.2787f)),
        (SystemTypes.MedBay,      new(-8.6636f,  -4.4547f)),
        (SystemTypes.Nav,         new(16.6989f,  -4.7736f)),
        (SystemTypes.Reactor,     new(-20.9127f, -5.5454f)),
        (SystemTypes.Security,    new(-13.2544f, -4.1371f)),
        (SystemTypes.Shields,     new(9.1997f,  -12.3562f)),
        (SystemTypes.Storage,     new(-2.3901f, -15.1296f)),
        (SystemTypes.UpperEngine, new(-17.6972f, -0.9157f)),
        (SystemTypes.Weapons,     new(9.5354f,    1.3911f)),
    ];

    private static Sprite? _roomOverlaySprite;

    /// <summary>
    /// 创建 1x1 白色方块精灵，运行时染色用
    /// </summary>
    private static Sprite GetRoomOverlaySprite()
    {
        if (_roomOverlaySprite != null) return _roomOverlaySprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _roomOverlaySprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        _roomOverlaySprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return _roomOverlaySprite;
    }

    /// <summary>
    /// 游戏开始时：分配每人独立房间 + 染色（所有客户端可见）
    /// </summary>
    void OnGameStart(GameStartEvent ev)
    {
        if (!EnableWireMode) return;

        var ship = ShipStatus.Instance;
        if (ship == null || !ship.IsFast<SkeldShipStatus>()) return;

        var players = GamePlayer.AllPlayers.Where(p => !p.IsDead).ToList();
        var rooms = SkeldRooms.Take(players.Count).ToList();

        NebulaManager.Instance.StartCoroutine(
            CoSpawnAndColor(players, rooms, AmongUsClient.Instance.AmHost).WrapToIl2Cpp());
    }

    private static IEnumerator CoSpawnAndColor(
        List<GamePlayer> players,
        List<(SystemTypes room, Vector2 pos)> rooms,
        bool isHost)
    {
        yield return new WaitForSeconds(0.2f);

        var ship = ShipStatus.Instance;
        if (ship == null) yield break;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var (roomType, spawnPos) = rooms[i];

            // Host 负责传送（RpcSnapTo 会同步到所有客户端）
            if (isHost && player.VanillaPlayer != null)
                player.VanillaPlayer.NetTransform.RpcSnapTo(spawnPos);

            // 获取玩家颜色
            int colorId = player.VanillaPlayer?.Data?.DefaultOutfit?.ColorId ?? 0;
            var playerColor = Palette.PlayerColors[Math.Min(colorId, Palette.PlayerColors.Length - 1)];
            var overlayColor = new Color(playerColor.r, playerColor.g, playerColor.b, RoomColorAlpha);

            // 所有客户端都创建房间染色
            PlainShipRoom? plainRoom = null;
            try { plainRoom = ship.FastRooms[roomType]; } catch { }

            if (plainRoom != null && plainRoom.roomArea != null)
            {
                var bounds = plainRoom.roomArea.bounds;
                var overlay = new GameObject($"WireRoom_{player.PlayerId}_{roomType}");
                overlay.transform.position = new Vector3(bounds.center.x, bounds.center.y, -5f);
                overlay.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);

                var renderer = overlay.AddComponent<SpriteRenderer>();
                renderer.sprite = GetRoomOverlaySprite();
                renderer.color = overlayColor;
                renderer.sortingOrder = -50;
            }
        }
    }
}

/// <summary>
/// Harmony 补丁：强制地图为 Skeld
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
internal static class WireModePatches
{
    static Harmony? harmony;

    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        harmony = new Harmony("hsg.wiremode");

        var awake = typeof(ShipStatus).GetMethod("Awake",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (awake != null)
        {
            harmony.Patch(awake, prefix: new HarmonyMethod(
                typeof(WireModePatches).GetMethod(nameof(OnShipAwake),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)));
        }
    }

    private static bool OnShipAwake(ShipStatus __instance)
    {
        if (!WireModeSettings.EnableWireMode) return true;
        if (__instance.IsFast<SkeldShipStatus>()) return true;

        HsgDebug.Log("[钢丝模式] 检测到非 Skeld 地图，正在修正");
        try
        {
            AmongUsUtil.GetCurrentNormalOption().MapId = 0;
            GameOptionsManager.Instance.currentNormalGameOptions.MapId = 0;
        }
        catch (System.Exception e)
        {
            HsgDebug.Log($"[钢丝模式] 修正失败: {e.Message}");
        }
        return true;
    }
}

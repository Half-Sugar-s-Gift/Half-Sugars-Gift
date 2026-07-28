using Nebula;
using Nebula.Game;
using Nebula.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 地图预加载器，用于 StarWreckEscape 模式中预加载和切换地图
/// </summary>
public static class MapPreloader
{
    /// <summary>已实例化的地图实例，key 为 mapId</summary>
    private static Dictionary<int, ShipStatus> _loadedMaps = new();

    /// <summary>当前激活的地图 Id，-1 表示尚未加载</summary>
    private static int _currentMapId = -1;

    /// <summary>当前激活的地图 Id</summary>
    public static int CurrentMapId => _currentMapId;

    /// <summary>
    /// 预加载所有需要的地图（Skeld/0, Polus/2, Fungle/5）
    /// 仅激活第一个地图（Skeld），其余设为未激活
    /// </summary>
    public static void PreloadAll()
    {
        int[] mapIds = { 0, 2, 5 };

        foreach (int mapId in mapIds)
        {
            var go = UnityEngine.Object.Instantiate(VanillaAsset.MapAsset[mapId].gameObject, Vector3.zero, Quaternion.identity);
            var ship = go.GetComponent<ShipStatus>();
            go.SetActive(false);
            _loadedMaps[mapId] = ship;
        }

        // 激活第一个地图（Skeld）并设为当前 ShipStatus
        _currentMapId = 0;
        _loadedMaps[0].gameObject.SetActive(true);
        ShipStatus.Instance = _loadedMaps[0];
    }

    /// <summary>
    /// 对外切换接口：启动协程切换到指定地图
    /// </summary>
    public static void SwitchTo(int mapId)
    {
        HudManager.Instance.StartCoroutine(CoSwitchMap(mapId).WrapToIl2Cpp());
    }

    /// <summary>
    /// 切换地图的协程流程：
    /// 1. 强制所有存活玩家退出通风口
    /// 2. 禁用当前地图 → 激活目标地图 → 替换 ShipStatus.Instance
    /// 3. 刷新缓存并更新 RuntimeAsset
    /// 4. 传送所有存活玩家到新地图出生点
    /// </summary>
    private static IEnumerator CoSwitchMap(int targetMapId)
    {
        // 1. 强制所有玩家退出通风口
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.inVent)
            {
                player.MyPhysics.RpcExitVent(0);
            }
        }

        yield return null;

        // 2. 禁用当前地图 → 激活目标地图
        if (_currentMapId >= 0 && _loadedMaps.TryGetValue(_currentMapId, out var currentShip))
        {
            currentShip.gameObject.SetActive(false);
        }

        if (_loadedMaps.TryGetValue(targetMapId, out var targetShip))
        {
            targetShip.gameObject.SetActive(true);
            ShipStatus.Instance = targetShip;
            _currentMapId = targetMapId;
        }

        // 3. 刷新 Nebula 缓存（使所有 LLCache 重新读取新的 ShipStatus.Instance）
        AmongUsLLImpl.Instance.OnSceneChanged();

        // 更新 RuntimeAsset（仅首次切换时释放旧 Addressables handle）
        var gameManager = NebulaGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.RuntimeAsset.MinimapPrefab = ShipStatus.Instance.MapPrefab;
            gameManager.RuntimeAsset.MinimapPrefab.gameObject.MarkDontUnload();
            gameManager.RuntimeAsset.MapScale = ShipStatus.Instance.MapScale;
        }

        yield return null;

        // 4. 传送所有存活玩家到新地图出生点
        var spawnCenter = ShipStatus.Instance.InitialSpawnCenter;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!player.Data.IsDead)
            {
                player.NetTransform.SnapTo(spawnCenter);
            }
        }
    }
}

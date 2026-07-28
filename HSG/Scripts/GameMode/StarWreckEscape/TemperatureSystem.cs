using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 体温系统 - 管理所有玩家的体温变化与室外判定
/// </summary>
public static class TemperatureSystem
{
    // ==== 体温数据 ====
    private static Dictionary<byte, float> playerTemperatures = new();

    // ==== UI 显示 ====
    private static TextMeshPro? temperatureDisplay;
    private static bool initialized = false;

    // ==== 体温阈值 ====
    public const float CrewmateDefaultTemp = 36f;
    public const float ImpostorDefaultTemp = 35f;

    /// <summary>
    /// 初始化体温系统：创建屏幕上方温度显示
    /// </summary>
    public static void Initialize()
    {
        if (initialized) return;
        if (HudManager.Instance == null || HudManager.Instance.IntroPrefab == null) return;

        // 克隆 ImpostorTitle 作为温度显示（参考 TitleShower 的创建方式）
        var textObj = GameObject.Instantiate(
            HudManager.Instance.IntroPrefab.ImpostorTitle,
            HudManager.Instance.transform
        );
        textObj.GetComponent<TextTranslatorTMP>().enabled = false;
        textObj.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        textObj.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        textObj.rectTransform.localScale = new Vector3(1.8f, 1.8f, 1f);
        textObj.rectTransform.sizeDelta = new Vector2(2.4f, 3f);
        textObj.outlineColor = new Color32(0, 0, 0, 0);
        textObj.color = new Color(1f, 1f, 1f, 0f);
        textObj.text = "";

        temperatureDisplay = textObj;
        initialized = true;
    }

    /// <summary>
    /// 重置所有存活玩家的体温
    /// </summary>
    public static void Reset()
    {
        playerTemperatures.Clear();
        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;
            bool isImpostor = player.Role?.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole;
            playerTemperatures[player.PlayerId] = isImpostor ? ImpostorDefaultTemp : CrewmateDefaultTemp;
        }
    }

    /// <summary>
    /// 每帧更新所有玩家的体温
    /// </summary>
    public static void Update(float deltaTime)
    {
        if (!initialized) Initialize();

        // 清理已死亡玩家
        var deadIds = new List<byte>();
        foreach (var kvp in playerTemperatures)
        {
            var player = GamePlayer.GetPlayer(kvp.Key);
            if (player == null || player.IsDead)
                deadIds.Add(kvp.Key);
        }
        foreach (var id in deadIds) playerTemperatures.Remove(id);

        float rate = StarWreckEscapeConfig.TemperatureChangeRate * deltaTime;

        // 更新存活玩家
        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead) continue;
            if (!playerTemperatures.TryGetValue(player.PlayerId, out float temp)) continue;

            bool isImpostor = player.Role?.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole;
            bool outdoors = IsOutdoors(player.TruePosition);

            if (isImpostor)
            {
                // 内鬼：室内升温，室外静止
                if (!outdoors) temp += rate;
                else if (temp > ImpostorDefaultTemp) temp -= rate * 0.3f; // 室外缓慢冷却
            }
            else
            {
                // 船员：室外降温，室内恢复
                if (outdoors) temp -= rate;
                else if (temp < CrewmateDefaultTemp) temp += rate * 0.3f;
            }

            temp = Mathf.Clamp(temp, 30f, 40f);
            playerTemperatures[player.PlayerId] = temp;

            // 死亡判定
            if (ShouldDie(player, temp))
            {
                // 使用自杀来模拟极端温度死亡
                player.Suicide(PlayerState.Suicide, EventDetail.Kill, KillParameter.NormalKill);
            }
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 获取指定玩家的当前体温
    /// </summary>
    public static float GetTemperature(byte playerId)
    {
        return playerTemperatures.TryGetValue(playerId, out float temp) ? temp : CrewmateDefaultTemp;
    }

    /// <summary>
    /// 设置指定玩家的体温（用于外部干预，如医生技能）
    /// </summary>
    public static void SetTemperature(byte playerId, float value)
    {
        playerTemperatures[playerId] = Mathf.Clamp(value, 30f, 40f);
    }

    /// <summary>
    /// 判定玩家是否应因极端温度死亡
    /// </summary>
    private static bool ShouldDie(GamePlayer player, float temperature)
    {
        bool isImpostor = player.Role?.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole;
        float threshold = StarWreckEscapeConfig.TemperatureDeathThreshold;

        if (isImpostor)
            return temperature >= ImpostorDefaultTemp + threshold; // ≥ 35.5°C 过热死亡
        else
            return temperature <= CrewmateDefaultTemp - threshold; // ≤ 35.5°C 失温死亡
    }

    /// <summary>
    /// 判定坐标是否在室外（参考 EclipseShared.IsOutdoors 实现）
    /// </summary>
    public static bool IsOutdoors(Vector2 position)
    {
        var ship = ShipStatus.Instance;
        if (ship == null) return false;

        var rooms = ship.AllRooms;
        if (rooms == null) return false;

        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room == null || room.roomArea == null) continue;
            if (room.roomArea.OverlapPoint(position)) return false; // 在某房间内 → 室内
        }

        return true; // 不在任何房间 → 室外
    }

    /// <summary>
    /// 更新本地玩家的温度 UI 显示
    /// </summary>
    private static void UpdateDisplay()
    {
        if (temperatureDisplay == null) return;

        var local = GamePlayer.LocalPlayer;
        if (local == null || local.IsDead)
        {
            temperatureDisplay.gameObject.SetActive(false);
            return;
        }

        if (playerTemperatures.TryGetValue(local.PlayerId, out float temp))
        {
            temperatureDisplay.gameObject.SetActive(true);

            bool isImpostor = local.Role?.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole;
            string label = isImpostor ? "核心温度" : "体表温度";
            float threshold = StarWreckEscapeConfig.TemperatureDeathThreshold;

            // 根据温度范围设置颜色
            string colorHex;
            if (isImpostor)
            {
                float danger = ImpostorDefaultTemp + threshold;
                if (temp >= danger)      colorHex = "#FF4444"; // 危险-红
                else if (temp >= ImpostorDefaultTemp + threshold * 0.5f) colorHex = "#FFAA44"; // 警告-橙
                else                     colorHex = "#44AAFF"; // 正常-蓝
            }
            else
            {
                float danger = CrewmateDefaultTemp - threshold;
                if (temp <= danger)      colorHex = "#FF4444"; // 危险-红
                else if (temp <= CrewmateDefaultTemp - threshold * 0.5f) colorHex = "#FFAA44"; // 警告-橙
                else                     colorHex = "#44FF44"; // 正常-绿
            }

            temperatureDisplay.text = $"{label}: <color={colorHex}>{temp:F1}°C</color>";
        }
        else
        {
            temperatureDisplay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 清理体温系统
    /// </summary>
    public static void Cleanup()
    {
        playerTemperatures.Clear();
        if (temperatureDisplay != null)
        {
            GameObject.Destroy(temperatureDisplay.gameObject);
            temperatureDisplay = null;
        }
        initialized = false;
    }
}

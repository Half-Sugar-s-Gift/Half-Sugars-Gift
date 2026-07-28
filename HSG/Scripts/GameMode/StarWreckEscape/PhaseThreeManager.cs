using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Nebula;
using Nebula.Extensions;
using Virial.Events.Game;
using Virial.Events.Role;
using Virial.Game;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using NebulaN.Roles.Impostor;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 孢子黎明阶段（第三阶段）管理器
/// 复活所有死者为内鬼，存活船员变医生，切换 Fungle 地图
/// </summary>
public class PhaseThreeManager : IGameOperator
{
    /// <summary>阶段是否已启动</summary>
    private bool _phaseStarted = false;

    /// <summary>密码是否已正确输入</summary>
    private bool _passwordCorrect = false;

    /// <summary>游戏结束倒计时（秒）</summary>
    private float _endCountdown = 0f;

    /// <summary>密码输入界面是否已开启</summary>
    private bool _passwordPanelOpen = false;

    /// <summary>感染事件是否已订阅</summary>
    private bool _infectionSubscribed = false;

    void IGameOperator.OnReleased() { }

    /// <summary>
    /// 启动阶段三
    /// </summary>
    public void StartPhase()
    {
        if (_phaseStarted) return;
        _phaseStarted = true;
        _passwordCorrect = false;
        _passwordPanelOpen = false;

        NebulaManager.Instance.StartCoroutine(CoStartPhase().WrapToIl2Cpp());
    }

    /// <summary>
    /// 阶段三主流程协程
    /// </summary>
    private IEnumerator CoStartPhase()
    {
        // 显示阶段标题
        var (title, subtitle) = TitleCardUI.GetPhaseTitle(2);
        TitleCardUI.ShowTitle(title, subtitle);

        yield return new WaitForSeconds(0.5f);

        // 1. 复活所有已死亡玩家为内鬼
        ReviveDeadAsImpostors();

        yield return new WaitForSeconds(1f);

        // 2. 将存活船员转换为医生
        ConvertCrewmatesToDoctors();

        yield return new WaitForSeconds(0.5f);

        // 3. 切换到 Fungle 地图（mapId = 5）
        MapPreloader.SwitchTo(5);

        yield return new WaitForSeconds(0.5f);

        // 4. 注册内鬼感染事件（击杀不致死，转为内鬼）
        SubscribeInfectionEvent();

        // 5. 启动 1 分钟计时器
        NebulaManager.Instance.StartCoroutine(CoPhaseTimer().WrapToIl2Cpp());

        // 提示所有玩家进入第三阶段
        PatchManager.SendLocalMessage(Language.Translate("phase.starwreck.three.start"));
    }

    /// <summary>
    /// 复活所有已死亡玩家为感染者（特殊内鬼身份）
    /// </summary>
    [OnlyHost]
    private void ReviveDeadAsImpostors()
    {
        var deadPlayers = GamePlayer.AllPlayers.Where(p => p.IsDead).ToList();
        if (deadPlayers.Count == 0) return;

        foreach (var player in deadPlayers)
        {
            // 复活玩家
            player.Revive(player, player.Position, true, false);
        }

        // 使用 RoleTable 统一分配感染者角色
        var roleTable = new RoleTable();
        foreach (var player in deadPlayers)
        {
            roleTable.SetRole(player.PlayerId, Infector.MyRole);
        }
        GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(roleTable));
        roleTable.Determine();
    }

    /// <summary>
    /// 将存活船员角色替换为医生
    /// </summary>
    [OnlyHost]
    private void ConvertCrewmatesToDoctors()
    {
        // 收集所有需要转换的船员，用一个 RoleTable 统一处理
        var crewmates = GamePlayer.AllPlayers
            .Where(p => !p.IsDead && !p.IsDisconnected && p.Role.Role.Category == RoleCategory.CrewmateRole)
            .ToList();

        if (crewmates.Count == 0) return;

        var roleTable = new RoleTable();
        foreach (var player in crewmates)
        {
            roleTable.SetRole(player.PlayerId, Doctor.MyRole);
        }
        GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(roleTable));
        roleTable.Determine();
    }

    /// <summary>
    /// 注册内鬼感染事件：击杀键不致死，目标转为内鬼
    /// </summary>
    [OnlyHost]
    private void SubscribeInfectionEvent()
    {
        if (_infectionSubscribed) return;
        _infectionSubscribed = true;

        GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(OnKillAttempt, NebulaAPI.CurrentGame!);
    }

    /// <summary>
    /// 处理击杀事件：感染者击杀船员时，复活船员并转为感染者
    /// </summary>
    [OnlyHost]
    private void OnKillAttempt(PlayerKillPlayerEvent ev)
    {
        if (ev.Murderer == null || ev.Dead == null) return;
        if (ev.Dead.IsDead) return;

        // 仅当杀手为内鬼阵营时才触发感染
        if (ev.Murderer.Role.Role.Category != RoleCategory.ImpostorRole) return;

        var target = ev.Dead;

        // 目标转为感染者（使用 RoleTable 分配）
        var roleTable = new RoleTable();
        roleTable.SetRole(target.PlayerId, Infector.MyRole);
        GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(roleTable));
        roleTable.Determine();

        // 复活目标
        target.Revive(ev.Murderer, target.Position, true, true);

        // 播放感染反馈
        AmongUsUtil.PlayQuickFlash(Cor.impRed);
    }

    /// <summary>
    /// 阶段计时器协程：等待 1 分钟后开启密码
    /// </summary>
    private IEnumerator CoPhaseTimer()
    {
        // 等待 1 分钟（60 秒）
        float timer = 60f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        // 时间到，开启密码面板
        _passwordPanelOpen = true;
        OpenPasswordPanel();

        // 等待密码正确输入
        while (!_passwordCorrect) yield return null;

        // 密码正确，启动 10 秒倒计时
        _endCountdown = 10f;
        while (_endCountdown > 0f)
        {
            _endCountdown -= Time.deltaTime;

            // 每秒更新倒计时显示
            if (Mathf.FloorToInt(_endCountdown + 1f) > Mathf.FloorToInt(_endCountdown))
            {
                PatchManager.SendLocalMessage(
                    Language.Translate("phase.starwreck.three.countdown")
                        .Replace("%TIME%", Mathf.CeilToInt(_endCountdown).ToString())
                );
            }
            yield return null;
        }

        // 倒计时结束，检查游戏胜利条件
        CheckGameEnd();
    }

    /// <summary>
    /// 打开密码输入界面
    /// </summary>
    private void OpenPasswordPanel()
    {
        PatchManager.SendLocalMessage(Language.Translate("phase.starwreck.three.password"));
        // 密码输入逻辑（简化版：由外部输入 /password 命令触发）
    }

    /// <summary>
    /// 由外部调用，提交密码验证
    /// </summary>
    public bool SubmitPassword(string input)
    {
        if (!_passwordPanelOpen) return false;

        // 验证密码（固定密码 "spore"）
        if (input.ToLower() == "spore")
        {
            _passwordCorrect = true;
            _passwordPanelOpen = false;
            PatchManager.SendLocalMessage(Language.Translate("phase.starwreck.three.password.correct"));
            return true;
        }
        else
        {
            PatchManager.SendLocalMessage(Language.Translate("phase.starwreck.three.password.wrong"));
            return false;
        }
    }

    /// <summary>
    /// 倒计时结束后判定游戏结果
    /// </summary>
    [OnlyHost]
    private void CheckGameEnd()
    {
        // 检查存活玩家中内鬼和船员的数量
        int aliveCrew = 0;
        int aliveImp = 0;

        foreach (var player in GamePlayer.AllPlayers)
        {
            if (player.IsDead || player.IsDisconnected) continue;

            if (player.Role.Role.Category == RoleCategory.CrewmateRole)
                aliveCrew++;
            else if (player.Role.Role.Category == RoleCategory.ImpostorRole)
                aliveImp++;
        }

        // 船员全部被感染 → 内鬼胜利
        if (aliveCrew <= 0)
        {
            NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Situation);
        }
        // 还有船员存活 → 船员胜利（成功在孢子黎明中存活）
        else
        {
            NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Situation);
        }
    }
}

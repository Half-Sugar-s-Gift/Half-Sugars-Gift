using hvtXsvc.Core;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using GamePlayer = Virial.Game.Player;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace NebulaN.Roles.Crewmate;

public class Hunter : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>
{
    private static FloatConfiguration selectionDuration = NebulaAPI.Configurations.Configuration(
        "options.role.hunter.selectionDuration",
        (5f, 60f, 2.5f),
        15f,
        FloatConfigurationDecorator.Second,
        null, null
    );

    private Hunter() : base(
        "hunter",
        new(0.2f, 0.8f, 0.2f),
        RoleCategory.CrewmateRole,
        NebulaTeams.CrewmateTeam,
        new Virial.Configuration.IConfiguration[] { selectionDuration }
    )
    { }

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;
    public static readonly Hunter MyRole = new Hunter();

    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        void IGameOperator.OnReleased() { }
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => Array.Empty<IPlayerAbility>();
        public DefinedRole Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;

            GameOperatorManager.Instance?.Subscribe<PlayerVoteDisclosedLocalEvent>(ev =>
            {
                if (ev.VoteFor?.AmOwner == true && ev.VoteToWillBeExiled)
                {
                    StartHunterSelection();
                }
            }, this);
        }

        private void StartHunterSelection()
        {
            if (!AmOwner || MyPlayer.IsDead) return;

            MeetingHud.Instance.TitleText.text = Language.Translate("role.hunter.waiting");

            var targets = GamePlayer.AllPlayers.Where(p => !p.IsDead && p.PlayerId != MyPlayer.PlayerId).ToList();
            if (targets.Count == 0) return;

            var window = MetaScreen.GenerateWindow(
                new Vector2(5f, 4f),
                HudManager.Instance.transform,
                new Vector3(0, 0, -50f),
                true, false);

            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();

            foreach (var p in targets)
            {
                var target = p;
                inner.Append(new MetaWidgetOld.Button(() =>
                {
                    MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, KillParameter.MeetingKill, KillCondition.BothAlive);
                    window.CloseScreen();
                    MeetingHud.Instance.TitleText.text = Language.Translate("game.meeting.vote");
                }, TextAttributeOld.BoldAttr)
                {
                    RawText = target.Name,
                });
            }

            MetaWidgetOld.ScrollView scroller = new(new Vector2(4.5f, 3.5f), inner, true)
            { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            window.SetWidget(widget);

            float timeLeft = selectionDuration;
            GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(ev2 =>
            {
                if (!AmOwner || MyPlayer.IsDead) return;
                timeLeft -= ev2.DeltaTime;
                if (timeLeft <= 0f)
                {
                    var randomTarget = targets[UnityEngine.Random.Range(0, targets.Count)];
                    MyPlayer.MurderPlayer(randomTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.MeetingKill, KillCondition.BothAlive);
                    window.CloseScreen();
                    MeetingHud.Instance.TitleText.text = Language.Translate("game.meeting.vote");
                }
            }, null);
        }
    }
}
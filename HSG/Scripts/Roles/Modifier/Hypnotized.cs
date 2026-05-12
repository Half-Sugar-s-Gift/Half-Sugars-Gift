using Nebula;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Utilities;
using NebulaN.Roles.Impostor;
using System;
using Virial;
using Virial.Assignable;
using Virial.Game;
using GamePlayer = Virial.Game.Player;

namespace NebulaN.Roles.Modifier;

public class Hypnotized : DefinedModifierTemplate, DefinedModifier
{
    private Hypnotized() : base(
        "hypnotized",
        new Virial.Color(0.2f, 0.6f, 1f),
        null,
        false,
        () => false)
    {
    }

    bool DefinedModifier.IsMadmate => true;

    public static Hypnotized MyRole = new();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        private bool active;
        private FlexibleLifespan? killLifespan;

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
            active = true;
            killLifespan = new FlexibleLifespan();
            var killTracker = ObjectTrackers.ForPlayer(this, null, MyPlayer, ObjectTrackers.KillablePredicate(MyPlayer));
            var killButton = NebulaAPI.Modules.AbilityButton(
                killLifespan, MyPlayer,
                VirtualKeyInput.Kill,
                0f,
                "kill",
                null,
                (button) => killTracker.CurrentTarget != null,
                (button) => !MyPlayer.IsDead,
                false
            );

            killButton.OnClick = (button) =>
            {
                var target = killTracker.CurrentTarget;
                if (target != null && active)
                {
                    MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    MyPlayer.RemoveModifier(MyRole);
                }
            };
            float duration = Owl.hypnotizeDuration;
            NebulaManager.Instance.StartDelayAction(duration, () =>
            {
                if (AmOwner && !MyPlayer.IsDead && active)
                {
                    MyPlayer.Suicide(PlayerState.Dead, null, KillParameter.NormalKill, null);
                    MyPlayer.RemoveModifier(MyRole);
                }
            });
        }

        void RuntimeAssignable.OnInactivated()
        {
            active = false;
            if (killLifespan != null)
            {
                killLifespan.Release();
                killLifespan = null;
            }
        }
    }
}
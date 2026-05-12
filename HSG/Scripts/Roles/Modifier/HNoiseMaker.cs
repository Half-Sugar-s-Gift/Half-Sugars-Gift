using System;
using System.Collections.Generic;
using System.Linq;
using hvtXsvc.Core;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;
using NPlayer = Virial.Game.Player;

namespace NebulaN.Roles.Modifier;


public class HNoiseMaker : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private static FloatConfiguration noiseDuration = NebulaAPI.Configurations.Configuration(
        "options.role.HNoiseMaker.noiseDuration",
        (1f, 10f, 1f),
        3f,
        FloatConfigurationDecorator.Second,
        null, null
    );

    private HNoiseMaker() : base(
        "HNoiseMaker",                            
        "modinoise",                                   
        new Virial.Color(160, 131, 187, byte.MaxValue), 
        [noiseDuration]                             
    )
    { }

    Citation? HasCitation.Citation => Citations.AmongUs;

    //Virial.Media.Image? DefinedAssignable.IconImage =>
        //NebulaAPI.AddonAsset.GetResource("HNoiseMakerIcon.png")?.AsImage();

    public static readonly HNoiseMaker MyRole = new HNoiseMaker();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(NPlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(NPlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }

        private void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Dead.PlayerId != MyPlayer.PlayerId) return;
            var pos = ev.Dead.TruePosition;
            var unityPos = new UnityEngine.Vector3(pos.x, pos.y, 0f);
            var arrow = AmongUsUtil.InstantiateNoisemakerArrow(unityPos, true, null);
            arrow.Item2.SetDuration(noiseDuration);
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner) name += " !!".Color(new UnityEngine.Color(0.627f, 0.514f, 0.733f));//这个数是拿deepseek算的。
        }
    }
}
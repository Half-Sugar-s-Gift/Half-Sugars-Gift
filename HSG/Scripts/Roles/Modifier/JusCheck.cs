using Virial;
using Virial.Assignable;
using Virial.Game;
using GamePlayer = Virial.Game.Player;

namespace NebulaN.Roles.Modifier;

public class RefereeRecruited : DefinedModifierTemplate, DefinedModifier
{
    private RefereeRecruited() : base("rrefRecruited", new Virial.Color(128, 128, 128), null, false, () => false) { }
    public static RefereeRecruited MyRole = new();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }
    }
}
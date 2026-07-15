namespace NebulaN.Roles.Modifier;
public class Sharp_Eye : DefinedAllocatableModifierTemplate,
    DefinedAllocatableModifier, 
    RuntimeAssignableGenerator<RuntimeModifier>, 
    HasCitation
{


    private Sharp_Eye() : base(
        "sharpeye",
        "SE",
        new Virial.Color(0.2f, 0.8f, 0.2f),
        new IConfiguration[] { }
    )
    { }
    static bool _GameStarted = false;
    public static Sharp_Eye MyRole = new();

    public Citation Citation => Citations.hvtXsvc_hsg;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier,HasCitation
    {
        public Instance(GamePlayer player) : base(player) { }

        public Citation Citation => Citations.hvtXsvc_hsg;

        DefinedModifier RuntimeModifier.Modifier => (DefinedModifier)MyRole;

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
        }
        [OnlyHost]
        void OnGameStarted(GameStartEvent ev)
        {
            _GameStarted = true;
        }
        [Local]
        void RoleChange(PlayerRoleSetEvent ev)
        {
            if (!_GameStarted) return;
            if (!AmOwner) return;
            AmongUsUtil.PlayQuickFlash(Cor.blue);
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (!AmOwner) return;
            name += " O".Color(new UnityEngine.Color(0.2f, 0.8f, 0.2f));
        }
    }

}
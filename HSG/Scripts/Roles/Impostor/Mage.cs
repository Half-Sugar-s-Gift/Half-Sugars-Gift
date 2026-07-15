//using NebulaN.Roles.Modifier;

//namespace NebulaN.Roles.Impostor;

//public class Mage : DefinedRoleTemplate, DefinedRole, RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument, HasCitation
//{
//    static IntegerConfiguration WeakMaxUses = NebulaAPI.Configurations.Configuration(
//        "options.role.mage.weakmaxUses", (1, 10, 1), 2);
//    static FloatConfiguration WCooldown = NebulaAPI.Configurations.Configuration(
//        "options.role.mage.cooldown", (2f, 60f, 2f), 15f, FloatConfigurationDecorator.Second);
//    Mage() : base(
//        "mage",
//        Cor.impRed,
//        RoleCategory.ImpostorRole,
//        NebulaTeams.ImpostorTeam,
//        new IConfiguration[] { WeakMaxUses, WCooldown }
//        )
//    {

//    }
//    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
//    public static readonly Mage MyRole = new Mage();
//    public Citation Citation => Citations.hvtXsvc_hsg;
//    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
//    {
//        yield return new AssignableDocumentImage(
//            NebulaAPI.AddonAsset.GetResource("Weak.png")?.AsImage(100f),
//            "role.mage.doc"
//        );
//    }
//    bool IAssignableDocument.HasTips => false;
//    bool IAssignableDocument.HasAbility => true;
//    public class Instance : RuntimeAssignableTemplate, RuntimeRole
//    {
//        public DefinedRole Role => MyRole;
//        public Instance(GamePlayer player) : base(player) { }
//        private static readonly Image? WeakImage = NebulaAPI.AddonAsset.GetResource("Weak.png")?.AsImage(100f);

//        private ModAbilityButton? Weakbutton;
//        private int WeakusesLeft;

//        void RuntimeAssignable.OnActivated()
//        {
//            if (!AmOwner) return;

//            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
//            playerTracker.SetColor(MyRole.RoleColor);

//            WeakusesLeft = WeakMaxUses;
//            Weakbutton = NebulaAPI.Modules.AbilityButton(
//                this,
//                MyPlayer,
//                VirtualKeyInput.Ability,
//                WCooldown,
//                "mage.weak",
//                WeakImage,
//                _ => WeakusesLeft > 0 && MyPlayer.CanMove && playerTracker.CurrentTarget != null && playerTracker.CurrentTarget != MyPlayer,
//                _ => !MyPlayer.IsDead
//            );

//            Weakbutton.ShowUsesIcon(4, WeakusesLeft.ToString());
//            Weakbutton.OnClick = (button) =>
//            {
//                var target = playerTracker.CurrentTarget;
//                if (target == null || target == MyPlayer || WeakusesLeft <= 0) return;

//                target.AddModifier(Weakness.MyRole);
//                WeakusesLeft--;
//                Weakbutton.UpdateUsesIcon(WeakusesLeft.ToString());
//                Weakbutton.StartCoolDown();
//            };
//        }
//    } 
//}
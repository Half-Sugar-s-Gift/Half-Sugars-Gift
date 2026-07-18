using Nebula.Roles.Ghost;

namespace NebulaN.Roles.Ghost;

public class Glimmer : DefinedGhostRoleTemplate, DefinedGhostRole,HasCitation
{
    private Glimmer() : base(
        "glimmer",
        new Virial.Color(1f, 0f, 0f),
        RoleCategory.CrewmateRole,
        [Uses, CoolDown, FlashDuration]
    )
    {
    }


    private static IntegerConfiguration Uses =
        NebulaAPI.Configurations.Configuration(
            "options.role.glimmer.uses",
            (1, 10),
            1
        );

    private static FloatConfiguration CoolDown =
        NebulaAPI.Configurations.Configuration(
            "options.role.glimmer.cooldown",
            (0f, 60f, 2.5f),
            15f,
            FloatConfigurationDecorator.Second
        );

    private static FloatConfiguration FlashDuration =
        NebulaAPI.Configurations.Configuration(
            "options.role.glimmer.flashDuration",
            (0.5f, 10f, 0.5f),
            3f,
            FloatConfigurationDecorator.Second
        );


    public static readonly Glimmer MyRole = new();


    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;

    public string CodeName => "GR";

    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);


    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;
        ModAbilityButton? flashButton;
        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
            int left = Uses;
            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
            playerTracker.SetColor(MyRole.RoleColor);
            flashButton = NebulaAPI.Modules.AbilityButton(
                this,
                MyPlayer,
                VirtualKeyInput.Ability,
                CoolDown,
                "glimmer.flash",
                NebulaAPI.AddonAsset.GetResource("glimmer.png")?.AsImage(),
                _ => playerTracker.CurrentTarget != null
                    && playerTracker.CurrentTarget != MyPlayer,
                _ => left > 0,
                true
            );
            flashButton.ShowUsesIcon(0, left.ToString());
            flashButton.OnClick = button =>
            {
                var target = playerTracker.CurrentTarget;
                if (target == null)
                    return;
                PatchManager.RpcFlashCustom.Invoke(
                    (
                        target.PlayerId,
                        "#FF0000",
                        0.2f,
                        FlashDuration
                    )
                );
                left--;
                if (left > 0)
                    button.UpdateUsesIcon(left.ToString());
                button.StartCoolDown();
            };
        }
    }
}
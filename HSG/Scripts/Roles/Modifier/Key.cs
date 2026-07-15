
namespace NebulaN.Roles.Modifier;

public class KeyMaster : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    static private FloatConfiguration cooldown = NebulaAPI.Configurations.Configuration(
        "options.modifier.keymaster.cooldown",
        (0f, 60f, 2.5f),
        3f,
        FloatConfigurationDecorator.Second,
        null, null
    );

    static private FloatConfiguration Specooldown = NebulaAPI.Configurations.Configuration(
        "options.modifier.keymaster.Specooldown",
        (0f, 60f, 2.5f),
        15,
        FloatConfigurationDecorator.Second,
        null, null
    );
    /*static private IntegerConfiguration maxUses = NebulaAPI.Configurations.Configuration(
        "options.modifier.keymaster.maxuses",
        (1, 15),
        3,
        null, null
    );*/

    private const float OpenRadius = 2f;

    private KeyMaster() : base(
        "keymaster",
        "key",
        new Virial.Color(0f, 1f, 0f),
        new Virial.Configuration.IConfiguration[] { cooldown,Specooldown}

    )
    { }

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;
    static public KeyMaster MyRole = new KeyMaster();
    Virial.Media.Image? DefinedAssignable.IconImage =>
        NebulaAPI.AddonAsset.GetResource("Smallicon/KeyIcon.png")?.AsImage();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments)
    {
        return ((RuntimeAssignableGenerator<RuntimeModifier>)MyRole).CreateInstance(player, arguments);
    }

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        Virial.Media.Image? doorHackImage = NebulaAPI.AddonAsset.GetResource("KeyMasterButton.png")?.AsImage(115f);
        //int usesLeft;

        public Instance(GamePlayer player) : base(player) { }

        void IGameOperator.OnReleased() { }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                //usesLeft = maxUses;
                ModAbilityButton hackButton = NebulaAPI.Modules.AbilityButton(this)
                    .BindKey(VirtualKeyInput.Ability, "keymaster.open", false)
                    .SetImage(doorHackImage)
                    .SetLabel("keymaster.open");
                hackButton.Availability = (ModAbilityButton button) =>
                    MyPlayer.CanMove && !MyPlayer.IsDead && AnyClosedDoorInRange() && !button.IsInCooldown;

                hackButton.Visibility = (ModAbilityButton button) => !MyPlayer.IsDead;

                hackButton.OnClick = (ModAbilityButton button) =>
                {
                    var pos = new UnityEngine.Vector2(MyPlayer.TruePosition.x, MyPlayer.TruePosition.y);
                    bool openedDecontamination = false;

                    foreach (OpenableDoor door in ShipStatus.Instance.AllDoors)
                    {
                        if (!door.IsOpen)
                        {
                            float dist = Vector2.Distance(pos, door.transform.position);
                            if (dist <= OpenRadius)
                            {
                                if (door.Room == SystemTypes.Decontamination)
                                    openedDecontamination = true;
                                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64));
                            }
                        }
                    }

                    float cd = openedDecontamination ? Specooldown : cooldown;
                    var timer = NebulaAPI.Modules.Timer(this, cd).SetAsAbilityTimer();
                    button.CoolDownTimer = timer.Start();
                };
            }
        }

        private bool AnyClosedDoorInRange()
        {
            var pos = new Vector2(MyPlayer.TruePosition.x, MyPlayer.TruePosition.y);
            foreach (OpenableDoor door in ShipStatus.Instance.AllDoors)
            {
                if (!door.IsOpen)
                {
                    if (Vector2.Distance(pos, door.transform.position) <= OpenRadius)
                        return true;
                }
            }
            return false;
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner) name += " >".Color(new UnityEngine.Color(0f, 1f, 0f));
        }
    }
}
namespace NebulaN.Roles.Crewmate;

public class DoctorAssistant : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{
    private DoctorAssistant() : base(
        "doctorAssistant",
        Cor.green,
        RoleCategory.CrewmateRole,
        NebulaTeams.CrewmateTeam,
        []  // 无配置项
    )
    { }

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;
    public static readonly DoctorAssistant MyRole = new();

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        private int assistUsesLeft = 1; // 只有 1 次治愈机会
        private ModAbilityButton? assistButton;

        void IGameOperator.OnReleased() { }
        public DefinedRole Role => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;

            // 创建玩家追踪器，用于选择目标
            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
            playerTracker.SetColor(Cor.green);

            // 技能图标
            var image = NebulaAPI.AddonAsset.GetResource("SkillIcon/doctor_assist.png")?.AsImage(100f);

            assistButton = NebulaAPI.Modules.AbilityButton(
                this,
                MyPlayer,
                VirtualKeyInput.Ability,
                3f,
                "doctorAssistant.assist",
                image,
                _ => playerTracker.CurrentTarget != null,
                _ => !MyPlayer.IsDead && assistUsesLeft > 0,
                false
            );
            assistButton.ShowUsesIcon(4, assistUsesLeft.ToString());

            assistButton.OnClick = (button) =>
            {
                var target = playerTracker.CurrentTarget;
                if (target == null || assistUsesLeft <= 0) return;

                assistUsesLeft--;
                button.UpdateUsesIcon("0");

                // 治愈目标（复活已死目标）
                if (target.IsDead)
                {
                    target.Revive(MyPlayer, target.Position, true, true);
                }

                button.StartCoolDown();
            };
        }
    }
}

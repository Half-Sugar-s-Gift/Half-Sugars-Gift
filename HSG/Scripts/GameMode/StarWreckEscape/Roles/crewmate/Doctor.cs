using hvtXsvc.GameMode.StarWreckEscape;
using Nebula.Roles.Assignment;
using Virial.Events.Role;

namespace NebulaN.Roles.Crewmate;

public class Doctor : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{
    private Doctor() : base(
        "doctor",
        Cor.green,
        RoleCategory.CrewmateRole,
        NebulaTeams.CrewmateTeam,
        []  // 无配置项，次数由 StarWreckEscapeConfig 管理
    )
    { }

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;
    public static readonly Doctor MyRole = new();

    // RPC：请求房主将目标角色替换为医生助手
    private static readonly RemoteProcess<byte> RpcConvertToAssistant = new("SWDocConvertAssist", (targetId, _) =>
    {
        var target = GamePlayer.GetPlayer(targetId);
        if (target == null || target.IsDead) return;

        var roleTable = new RoleTable();
        roleTable.SetRole(targetId, DoctorAssistant.MyRole);
        GameOperatorManager.Instance?.Run(new PreFixAssignmentEvent(roleTable));
        roleTable.Determine();
    });

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        private int cureUsesLeft;
        private ModAbilityButton? cureButton;

        void IGameOperator.OnReleased() { }
        public DefinedRole Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
            cureUsesLeft = StarWreckEscapeConfig.DoctorCureUses.GetValue();
        }

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;

            // 创建玩家追踪器，用于选择目标
            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
            playerTracker.SetColor(Cor.green);

            // 技能图标
            var image = NebulaAPI.AddonAsset.GetResource("SkillIcon/doctor.png")?.AsImage(100f);

            cureButton = NebulaAPI.Modules.AbilityButton(
                this,
                MyPlayer,
                VirtualKeyInput.Ability,
                3f,
                "doctor.cure",
                image,
                _ => playerTracker.CurrentTarget != null && playerTracker.CurrentTarget != MyPlayer,
                _ => !MyPlayer.IsDead && cureUsesLeft > 0,
                false
            );
            cureButton.ShowUsesIcon(4, cureUsesLeft.ToString());

            cureButton.OnClick = (button) =>
            {
                var target = playerTracker.CurrentTarget;
                if (target == null || target == MyPlayer || cureUsesLeft <= 0) return;

                cureUsesLeft--;
                button.UpdateUsesIcon(cureUsesLeft.ToString());

                // 若目标为内鬼阵营，发送RPC请求房主将其替换为医生助手
                if (target.Role.Role.Category == RoleCategory.ImpostorRole)
                {
                    RpcConvertToAssistant.Invoke(target.PlayerId);
                }
                // 目标为已死船员则复活
                else if (target.IsDead)
                {
                    target.Revive(MyPlayer, target.Position, true, true);
                }

                button.StartCoolDown();
            };
        }
    }
}

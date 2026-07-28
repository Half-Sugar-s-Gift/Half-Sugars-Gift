using Virial.Assignable;
using Virial.Game;

namespace NebulaN.Roles.Impostor;

/// <summary>
/// 感染者 - 星骸逃生第三阶段的特殊内鬼身份
/// 通过击杀键将船员转化为感染者，而非直接杀死
/// </summary>
public class Infector : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{
    private Infector() : base(
        "infector",
        Cor.impRed,
        RoleCategory.ImpostorRole,
        NebulaTeams.ImpostorTeam,
        []  // 无额外配置项
    )
    { }

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;
    public static readonly Infector MyRole = new();

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        void IGameOperator.OnReleased() { }
        public DefinedRole Role => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
            // 感染者使用击杀键作为感染技能
            // 技能图标复用击杀按钮图标（默认行为）
            // 感染逻辑由 PhaseThreeManager.OnKillAttempt 统一处理
        }
    }
}

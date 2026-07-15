/*using NebulaN.Roles.Modifier;

namespace Nebula.Roles.Impostor;

public class PanicMaker : DefinedRoleTemplate, DefinedRole, RuntimeAssignableGenerator<RuntimeRole>,HasCitation
{
    static IntegerConfiguration maxUses = 
        NebulaAPI.Configurations.Configuration(
            "options.role.panicmaker.MaxUses", 
            (1, 3), 
            1
            );
    static readonly Image buttonSprite = 
        NebulaAPI.AddonAsset.GetResource(
            "PanicMake.png"
            )?.AsImage(115f);

    private PanicMaker() : base("panicmaker",Cor.impRed, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam,
        new IConfiguration[] { maxUses }, true)
    { }

    public static PanicMaker MyRole = new();

    public Citation Citation => hvtXsvc.Core.Citations.hvtXsvc_hsg;

    public RuntimeRole CreateInstance(GamePlayer player, int[] args) => new Instance(player, args.Length > 0 ? args[0] : maxUses);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        int LeftUses;
        bool UsedThisMeeting = false;

        public Instance(GamePlayer player, int initUses) : base(player) => LeftUses = initUses;
        public DefinedRole Role => MyRole;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                GameOperatorManager.Instance.Subscribe<MeetingStartEvent>(OnMeetingStart, this);
                GameOperatorManager.Instance.Subscribe<MeetingEndEvent>(_ => { UsedThisMeeting = false; }, this);
            }
        }

        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (LeftUses <= 0 || UsedThisMeeting) return;
            bool HaveLivePanicer = GamePlayer.AllPlayers.Any(p => !p.IsDead && p.TryGetModifier<Panicer.Instance>(out _));
            var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            buttonManager?.RegisterMeetingAction(new(
                buttonSprite,
                state =>
                {
                    var target = state.MyPlayer;
                    if (target == null || target == MyPlayer) return;
                    RpcAddPanicer.Invoke(target.PlayerId);
                    RpcPanicKill.Invoke();

                    LeftUses--;
                    UsedThisMeeting = true;
                },
                p => !UsedThisMeeting && !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && LeftUses > 0 && !HaveLivePanicer
            ));
        }
        void ChangeName(PlayerDecorateNameEvent ev)
        {
            if (!AmOwner) return;
            if(ev.Player.TryGetModifier<Panicer.Instance>(out _))
            ev.Name = $"<b><color=#FF0000>!</color></b>{ev.Name}<b><color=#FF0000>!</color></b>";
        }
        static RemoteProcess<byte> RpcAddPanicer = new("AddPanicer", (targetId, _) =>
        {
            var target = GamePlayer.GetPlayer(targetId);
            target?.AddModifier(Panicer.MyRole, null);
        });
        static RemoteProcess RpcPanicKill = new("PanicKill", _ =>
        {
            if (!AmongUsClient.Instance.AmHost) return;

            var candidates = 
            GamePlayer.AllPlayers.Where(p => !p.IsDead && 
            !p.IsImpostor 
            && p.Role.Role != PanicMaker.MyRole 
            && !p.TryGetModifier<Panicer.Instance>
            (out var _)
            ).ToList();

            if (candidates.Count == 0) return;
            var victim = candidates.Random();
           hvtXsvc.Core.Patch.Play(victim);
        });
    }
}*/
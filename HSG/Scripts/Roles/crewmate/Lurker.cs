namespace NebulaN.Roles.Crewmate;
public class Lurker : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>,IAssignableDocument
{
    static Lurker()
    {
        GameOperatorManager.Instance?.Subscribe<GameStartEvent>(GameStart, null);
    }

    [OnlyHost]
    private static void GameStart(GameStartEvent ev)
    {
        bool _Alive = GamePlayer.AllPlayers.Any(p => !p.IsDead && p.Role is Lurker.Instance);
        if (!_Alive) return;

        GameOperatorManager.Instance.Subscribe<PlayerBlockWinEvent>(BlockWin, ev.Game);
    }

    [OnlyHost]
    private static void BlockWin(PlayerBlockWinEvent ev)
    {
        bool _Alive = GamePlayer.AllPlayers.Any(p => !p.IsDead && p.Role is Lurker.Instance);
        if (!_Alive) return;

        bool _isCrewmateWin = GamePlayer.AllPlayers.Any(p => ev.LastWinners.Test(p) && p.Role.Role.Team != NebulaTeams.CrewmateTeam);
        if (!_isCrewmateWin) return;

        ev.SetBlockedIf(true);
        Instance.RpcSetBool.Invoke();
    }


    static private BoolConfiguration CanKillConfig = NebulaAPI.Configurations.Configuration(
    "options.role.lurker.ckc",
    true
    );
    static private FloatConfiguration Cooldown = NebulaAPI.Configurations.Configuration(
        "options.role.lurker.cooldown",
        (5f, 60f, 3f),
        25,
        FloatConfigurationDecorator.Second,
        () => CanKillConfig
    );
    static private BoolConfiguration NoTask = NebulaAPI.Configurations.Configuration(
        "options.role.lurker.nt",
        true
        );


    private Lurker() : base(
        "lurker",
        Cor.LurkerCor,
        RoleCategory.CrewmateRole,
        NebulaTeams.CrewmateTeam,
        new Virial.Configuration.IConfiguration[] { Cooldown,NoTask,CanKillConfig }
    )
    {
        // ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("BigPic/Lurker.png")?.AsImage(115f);
    }
    // Virial.Media.Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("Smallicon/LurkerIcon.png")?.AsImage();

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;

    public static readonly Lurker MyRole = new Lurker();

    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%CD%", Cooldown.ToString());
        yield return new AssignableDocumentReplacement("%CANKILL%", CanKillConfig?Language.Translate("role.lurker.ck.true") :Language.Translate("role.lurker.ck.false"));
        yield return new AssignableDocumentReplacement("%NT%",NoTask?Language.Translate("role.lurker.nt.true"):Language.Translate("role.lurker.nt.false"));
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        void IGameOperator.OnReleased() { }
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => Array.Empty<IPlayerAbility>();
        bool RuntimeAssignable.InvalidateCrewmateTask => NoTask;
        public DefinedRole Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        ModAbilityButton? Btn;
        
        static public bool CanKill = false;
        void RuntimeAssignable.OnActivated()
        {
            ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;
            if (!AmOwner) return;
            var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
            playerTracker.SetColor(Cor.impRed);
            Btn = NebulaAPI.Modules.AbilityButton(
                this,
                MyPlayer,
                VirtualKeyInput.Kill,
                Cooldown,
                "lurker.kill",
                null,
                _ => !MyPlayer.IsDead && CanKill,
                _ => !MyPlayer.IsDead&& CanKillConfig,
                false
            );
            Btn.OnClick = (button) =>
            {
                var target = playerTracker.CurrentTarget;
                if (target != null) 
                {
                    MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, KillCondition.NormalKill);
                }
                button.StartCoolDown();
            };
        }
        static public RemoteProcess RpcSetBool = new("SetBool_H", _ =>
        {
            CanKill = true;
        });


        [Local]
        [OnlyHost]
        void NeedWin(PlayerDieEvent ev)
        {
            if (ev.Player == MyPlayer)
            {
                // 只是不想不使用参数。
                // 不用的话IDE骚扰我让我用。
                // 我好像可以PlayerDieEvent _
                // 这样就不会骚扰了。
                // 懒得整了。
                // 不改了。
            }
            var AlivePlayers = GamePlayer.AllPlayers.Where(p => !p.IsDead && !p.IsDisconnected).ToList();
            int CrewCount = AlivePlayers.Count(p => p.Role.Role.Category == RoleCategory.CrewmateRole);
            if (AlivePlayers.Count==CrewCount) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Situation);
        }
        void EraseCanKill(GameStartEvent ev) => CanKill = false;

    }
}
/*using HarmonyLib;
using Nebula;
using Nebula.Behavior;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Virial.Events.Player;
using Citations = hvtXsvc.Core.Citations;
using GamePlayer = Virial.Game.Player;
using Vector2 = UnityEngine.Vector2;

namespace NebulaN.Roles.Neutral;

public class Avenger : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{
    private static Team MyTeam = new Team("teams.Havenger",
        new Virial.Color(0.8f, 0.2f, 0.2f), TeamRevealType.OnlyMe);

    static private FloatConfiguration revengeDuration = NebulaAPI.Configurations.Configuration(
        "options.role.Havenger.revengeDuration",
        (10f, 120f, 5f),
        30f,
        FloatConfigurationDecorator.Second,
        null, null);

    private Avenger() : base(
        "Havenger",
        new Virial.Color(0.8f, 0.2f, 0.2f),
        RoleCategory.NeutralRole,
        MyTeam,
        new Virial.Configuration.IConfiguration[] { revengeDuration }
    )
    {
        HAvengerRevengeWin = NebulaAPI.Preprocessor!.CreateEnd("HAvengerRevenge",
            new Virial.Color(0.8f, 0.2f, 0.2f), 0);
    }

    public static GameEnd HAvengerRevengeWin { get; private set; } = null!;

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => false;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    { yield break; }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    { yield break; }

    public static readonly Avenger MyRole = new Avenger();

    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        void IGameOperator.OnReleased() { }
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => Array.Empty<IPlayerAbility>();
        public DefinedRole Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        private bool wasKilled = false;
        private GamePlayer? myKiller = null;

        void RuntimeAssignable.OnActivated()
        {
            
            if (!AmOwner) return;
            new StaticAchievementToken("BeNothing.common1");
            wasKilled = false;
            myKiller = null;

            GameOperatorManager.Instance?.Subscribe<PlayerMurderedEvent>(ev =>
            {
                if (ev.Dead.AmOwner && !MeetingHud.Instance && !ExileController.Instance)
                {
                    wasKilled = true;
                    myKiller = ev.Murderer;
                    RpcRemoveMyBody.Invoke(MyPlayer);
                }
            }, this);

            GameOperatorManager.Instance?.Subscribe<MeetingEndEvent>(ev =>
            {
                if (!wasKilled) return;

                if (MyPlayer.IsDead || myKiller == null || myKiller.IsDead)
                {
                    if (!MyPlayer.IsDead)
                        MyPlayer.Suicide(PlayerState.Dead, null, KillParameter.NormalKill, null);
                    return;
                }

                new AvengerRevengeMode(this, myKiller, revengeDuration).Register(this);
            }, this);
        }

        static RemoteProcess<GamePlayer> RpcRemoveMyBody = new("AvengerRemoveBody",
            (player, _) =>
            {
                foreach (var body in Helpers.AllDeadBodies()
                             .Where(b => b.ParentId == player.PlayerId))
                {
                    GameObject.Destroy(body.gameObject);
                }
            });

    }

    public class AvengerRevengeMode : FlexibleLifespan, IGameOperator
    {
        private Instance avenger;
        private GamePlayer targetKiller;
        private ModAbilityButton? killButton;
        private object? tracker;
        internal static bool IsRevengeActive = false;

        public AvengerRevengeMode(Instance avenger, GamePlayer killer, float duration)
        {
            this.avenger = avenger;
            this.targetKiller = killer;
            var myPlayer = avenger.MyPlayer;

            IsRevengeActive = true;

            if (myPlayer.AmOwner)
            {
                var reportButton = HudManager.Instance.ReportButton;
                reportButton.enabled = false;
                reportButton.graphic.enabled = false;
                HudManager.Instance.UseButton.enabled = false;
                HudManager.Instance.UseButton.graphic.enabled = false;
            }

            RpcScatterAllPlayers.Invoke();

            if (myPlayer.AmOwner)
            {
                var killIcon = NebulaAPI.AddonAsset.GetResource("HAvengerKill.png")?.AsImage(100f);
                var trackerObj = NebulaAPI.Modules.PlayerTracker(this, myPlayer);
                trackerObj.SetColor(new Virial.Color(0.8f, 0.2f, 0.2f));
                tracker = trackerObj;

                killButton = NebulaAPI.Modules.AbilityButton(
                    this, myPlayer,
                    VirtualKeyInput.Kill,
                    2f,
                    "Havenger.kill",
                    killIcon,
                    (ModAbilityButton _) => trackerObj.CurrentTarget != null,
                    (button) => !myPlayer.IsDead,
                    false
                );

                killButton.OnClick = (button) =>
                {
                    var target = trackerObj.CurrentTarget;
                    if (target == null) return;

                    if (target == targetKiller)
                    {
                        new StaticAchievementToken("Havenger.common.revenge");
                        NebulaAPI.CurrentGame?.TriggerGameEnd(HAvengerRevengeWin, GameEndReason.Special);
                    }
                    else
                    {
                        new StaticAchievementToken("Havenger.common.wrongKill");
                        myPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                        myPlayer.Suicide(PlayerState.Dead, null, KillParameter.NormalKill, null);
                        Release();
                    }
                };
            }
            NebulaManager.Instance.StartDelayAction(duration, () =>
            {
                if (!myPlayer.IsDead)
                {
                    new StaticAchievementToken("Havenger.common.timeout");
                    myPlayer.Suicide(PlayerState.Dead, null, KillParameter.NormalKill, null);
                }
                Release();
            });
        }

        void IGameOperator.OnReleased()
        {
            IsRevengeActive = false;

            if (avenger.MyPlayer.AmOwner)
            {
                var reportButton = HudManager.Instance.ReportButton;
                reportButton.enabled = true;
                reportButton.graphic.enabled = true;
                HudManager.Instance.UseButton.enabled = true;
                HudManager.Instance.UseButton.graphic.enabled = true;
            }
        }

        static RemoteProcess RpcScatterAllPlayers = new("AvengerScatter", _ =>
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.IsDead) continue;

                Vector2 destination;
                if (UnityEngine.Random.value > 0.5f)
                {
                    var vents = GameObject.FindObjectsOfType<Vent>();
                    if (vents.Length > 0)
                    {
                        var vent = vents[UnityEngine.Random.Range(0, vents.Length)];
                        destination = vent.transform.position;
                        destination.y += 0.3636f;
                        pc.NetTransform.RpcSnapTo(destination);
                        continue;
                    }
                }

                byte mapId = AmongUsUtil.CurrentMapId;
                var candidates = NebulaPreSpawnLocation.Locations[mapId];
                if (candidates.Length == 0)
                    candidates = NebulaPreSpawnLocation.Locations[mapId]
                        .Where(l => l.VanillaIndex != null).ToArray();
                destination = candidates[UnityEngine.Random.Range(0, candidates.Length)].Position!.Value;
                pc.NetTransform.RpcSnapTo(destination);
            }
        });
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
public static class AvengerVentBlockPatch
{
    static bool Prefix(ref bool __result)
    {
        if (Avenger.AvengerRevengeMode.IsRevengeActive)
        {
            __result = false;
            return false;
        }
        return true;
    }
}
*/
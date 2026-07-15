namespace NebulaN.Roles.Modifier;
public class RainbowCandy : 
    DefinedAllocatableModifierTemplate,
    DefinedAllocatableModifier, 
    RuntimeAssignableGenerator<RuntimeModifier>, 
    HasCitation
{
    private static FloatConfiguration changeInterval = NebulaAPI.Configurations.Configuration(
        "options.modifier.rainbowcandy.interval",
        (0.1f, 2f, 0.05f),
        0.3f,
        FloatConfigurationDecorator.Second
    );

    private RainbowCandy() : base(
        "rainbowcandy",
        "rb",
        new Virial.Color(1f, 0.5f, 0f),
        new IConfiguration[] { changeInterval }
    )
    { }

    public static RainbowCandy MyRole = new();

    public Citation Citation => Citations.hvtXsvc_hsg;

    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments)
        => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        private Coroutine? _colorRoutine;
        private static readonly List<int> _colorIds = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };

        public Instance(GamePlayer player) : base(player) { }

        DefinedModifier RuntimeModifier.Modifier => (DefinedModifier)MyRole;

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
            _colorRoutine = NebulaManager.Instance.StartCoroutine(ColorLoop().WrapToIl2Cpp());
        }

        private IEnumerator ColorLoop()
        {
            while (!MyPlayer.IsDead)
            {
                int randomColorId = _colorIds[UnityEngine.Random.Range(0, _colorIds.Count)];
                PlayerControl pc = null;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == MyPlayer.PlayerId)
                    {
                        pc = p;
                        break;
                    }
                }
                if (pc != null)  HostSendRpc.SetColor(MyPlayer, (byte)randomColorId);
                yield return new WaitForSeconds(changeInterval);
            }
        }

        void IGameOperator.OnReleased()
        {
            if (_colorRoutine != null)
            {
                NebulaManager.Instance.StopCoroutine(_colorRoutine);
                _colorRoutine = null;
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (!AmOwner) return;
            name += ColorHelper.Create(" |RainBow|", 0.5f, 0.05f, 0.9f, 1f);
        }
    }
}
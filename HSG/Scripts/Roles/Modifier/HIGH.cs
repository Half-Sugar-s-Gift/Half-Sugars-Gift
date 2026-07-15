namespace NebulaN.Roles.Modifier;

public class HIGH : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    static FloatConfiguration zuiXiao = NebulaAPI.Configurations.Configuration("options.modifier.high.min", (0.01f, 10.01f, 1f), 0.1f, FloatConfigurationDecorator.Ratio);
    static FloatConfiguration zuiDa = NebulaAPI.Configurations.Configuration("options.modifier.high.max", (0.5f, 20f, 1f), 15f, FloatConfigurationDecorator.Ratio);
    static FloatConfiguration jianGe = NebulaAPI.Configurations.Configuration("options.modifier.high.jiange", (0.01f, 1f, 0.01f), 0.05f, FloatConfigurationDecorator.Second);

    private HIGH() : base("HIGH", "H", new Virial.Color(0.5f, 0.8f, 1f), new IConfiguration[] { zuiXiao, zuiDa, jianGe })
    {
    }
    public Citation Citation => Citations.hvtXsvc_hsg;

    static public HIGH MyRole = new HIGH();
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("Smallicon/HIGHIcon.png")?.AsImage();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole as DefinedModifier ?? throw new System.InvalidOperationException();

        public Instance(GamePlayer player) : base(player)
        {
        }

        System.Collections.IEnumerator? xieCheng;
        bool huoYue;

        void RuntimeAssignable.OnActivated()
        {
            huoYue = false;
            if (AmOwner)
            {
                huoYue = true;
                xieCheng = GengXinXieCheng();
                NebulaManager.Instance.StartCoroutine(xieCheng.WrapToIl2Cpp());
                SuiJiChange();
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo || inEndScene)
            {
                name += " ⇅".Color(new UnityEngine.Color(0.5f, 0.8f, 1f));
            }
        }

        void IGameOperator.OnReleased()
        {
            huoYue = false;
            xieCheng = null;
            MyPlayer.RemoveAttributeByTag("HIGH_SizeTag");

        }

        System.Collections.IEnumerator GengXinXieCheng()
        {
            while (huoYue && !MyPlayer.IsDead)
            {
                yield return new WaitForSeconds(jianGe);
                if (huoYue && !MyPlayer.IsDead) SuiJiChange();
            }
        }

        void SuiJiChange()
        {

            float min = zuiXiao;
            float max = zuiDa;
            if (min > max) (min, max) = (max, min);
            float suiJiY = UnityEngine.Random.Range(min, max);
            MyPlayer.RemoveAttributeByTag("HIGH_SizeTag");
            HostSendRpc.SetSizeY(MyPlayer, suiJiY);
        }
    }
}
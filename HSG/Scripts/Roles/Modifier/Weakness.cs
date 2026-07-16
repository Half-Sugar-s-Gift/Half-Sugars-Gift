//using NebulaN.Roles.Impostor;

//namespace NebulaN.Roles.Modifier;

//public class Weakness : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
//{
//    static IntegerConfiguration ClearWeakTime = NebulaAPI.Configurations.Configuration(
//        "options.modifier.weakness.clearTime", (1, 10, 1), 2
//    );

//    public Weakness() : base(
//        "weakness", "WK", new Virial.Color(0.5f, 0.8f, 1f), new IConfiguration[] { ClearWeakTime }
//    )
//    {
//    }

//    public Citation Citation => Citations.hvtXsvc_hsg;
//    public static Weakness MyRole = new();
//    bool ISpawnable.IsSpawnable => false;

//    public RuntimeModifier CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

//    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
//    {
//        public Instance(GamePlayer player) : base(player) { }

//        // 不允许玩家知道自己被弱化
//        bool RuntimeAssignable.CanBeAwareAssignment => false;
//        DefinedModifier RuntimeModifier.Modifier => MyRole;

//        int WeakTimes;
//        void RuntimeAssignable.OnActivated()
//        {
//            if (!AmOwner) return;
//            WeakTimes = ClearWeakTime;
//        }
//        [Local]
//        void OnTaskComplete(PlayerTaskCompleteLocalEvent ev)
//        {
//            if (ev.Player != MyPlayer) return;
//            if (!AmOwner || MyPlayer.IsDead) return;
//            MyPlayer.Suicide(PlayerState.Dead, null, KillParameter.NormalKill);
//        }
//        [Local]
//        void OnMeetingEnd(MeetingEndEvent ev)
//        {
//            if (!AmOwner || MyPlayer.IsDead) return;
//            WeakTimes--;
//            if (WeakTimes <= 0)
//            {
//                MyPlayer.RemoveModifier(MyRole);
//            }
//        }
//        [Local]
//        void PlayerDecorateName(PlayerDecorateNameEvent ev)
//        {
//            if (ev.Player.Role.Role.InternalName == "mage")
//            {
//                ev.Name += ColorHelper.Create("X");
//            }
//        }
//    }
//}
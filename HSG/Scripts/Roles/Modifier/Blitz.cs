/*
 * 赛博佛祖 镇楼
 * 永无BUG
 * 
 *                  _ooOoo_
 *                 o8888888o
 *                 88" . "88
 *                 (| -_- |)
 *                 O\  =  /O
 *              ____/`---'\____
 *            .'  \\|     |//  `.
 *           /  \\|||  :  |||//  \
 *          /  _||||| -:- |||||_  \
 *          |   | \\\  -  /// |   |
 *          | \_|  ''\---/''  |_/ |
 *          \  .-\__  `-`  ___/-. /
 *        ___`. .'  /--.--\  `. .'___
 *      ."" '<  `.___\_<|>_/___.' >' "".
 *     | | :  `- \`.;`\ _ /`;.`/ - ` : | |
 *     \  \ `-.   \_ __\ /__ _/   .-` /  /
 *======`-.____`-.___\_____/___.-`____.-'======
 *                   `=---='
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 *          菩提本无树    明镜亦非台
 *          本来无BUG    何必常修改
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
namespace NebulaN.Roles.Modifier;

public class Blitz : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    Blitz() : base(
        "blitz", 
        "%", 
        new Virial.Color(1f, 0.5f, 0f), 
        new IConfiguration[] { },
        false,true,false
        ) 
    {

    }
    public Citation Citation => Citations.hvtXsvc_hsg;
    static public Blitz MyRole = new();
    
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole as DefinedModifier ?? throw new System.InvalidOperationException();

        public Instance(GamePlayer player) : base(player) { }
        bool _NeedChangeKillTime = false;
        [Local]
        void OnGameStart(GameStartEvent ev)
        {
            if(AmOwner)
                _NeedChangeKillTime = true;
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if(AmOwner)
                _NeedChangeKillTime = true;
        }

        [Local]
        void ResetKillCD(ResetKillCooldownLocalEvent ev)
        {
            if (!AmOwner) return;
            if (_NeedChangeKillTime)
            {
                ev.SetFixedCooldown(0.1f);
                _NeedChangeKillTime = false;
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner) name += " %".Color(new Color(1, 0.5f, 0));
        }
        void RuntimeAssignable.OnActivated()
        {
            
        }
    }
}
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
namespace HalfSugarGift.Core.Settings;

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
[NebulaRPCHolder]
public class RandomEventSettings : AbstractModule<Game>, IGameOperator
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new RandomEventSettings());
    }

    protected override void OnInjected(Game container) => this.Register(container);
    public static BoolConfiguration EnableRandomEventsSettings = NebulaAPI.Configurations.Configuration(
        "options.hsg.res.EnableRES",false
        );
    public static BoolConfiguration EnableChangeRandomTime = NebulaAPI.Configurations.Configuration(
        "options.hsg.res.EnableCRT", true,()=>EnableRandomEventsSettings
        ); // 是否启用更改事件发生随机时间
    public static ValueConfiguration<int> HowToReport = NebulaAPI.Configurations.Configuration(
        "options.hsg.res.HTR", 
        new[] {"options.hsg.res.htrTV", "options.hsg.res.htrText", "options.hsg.res.htrNone" },
        0,
        ()=>EnableRandomEventsSettings
        ); // 播报形式，分别为 左上角电视 大字播报 无播报 
    public static BoolConfiguration EnableVoiceReport = NebulaAPI.Configurations.Configuration(
        "options.hsg.res.EnableEVR", 
        true,
        ()=>EnableRandomEventsSettings
        ); // 启用语音播报
    public static IntegerConfiguration RandomEventsTimesEveryGame = NebulaAPI.Configurations.Configuration(
        "options.hsg.res.RETEG",
        (0,15),
        0, 
        ()=>EnableRandomEventsSettings
        ); // 最多次数，0表不限制

}
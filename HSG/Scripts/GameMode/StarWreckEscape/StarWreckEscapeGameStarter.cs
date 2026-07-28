using Nebula.Game;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 星骸逃生模式入口 — 在游戏开始时触发 PhaseStateMachine.StartGame()，
/// 启动整个游戏模式流程（状态机 + 阶段管理器）。
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
[NebulaRPCHolder]
internal class StarWreckEscapeGameStarter : AbstractModule<Game>, IGameOperator
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new StarWreckEscapeGameStarter());
    }

    protected override void OnInjected(Game container) => this.Register(container);

    void OnGameStart(GameStartEvent ev)
    {
        if (Nebula.Configuration.GeneralConfigurations.CurrentGameMode != StarWreckEscapeRegistration.Definition)
            return;

        HsgDebug.Log("StarWreckEscapeGameStarter: 检测到星骸逃生模式，启动状态机");
        PhaseStateMachine.StartGame();
    }

    void IGameOperator.OnReleased() { }
}

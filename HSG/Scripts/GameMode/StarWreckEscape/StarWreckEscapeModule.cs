namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// StarWreckEscape 游戏模式的模块接口（标记接口，用于 GameModeDefinitionImpl 的类型标识）
/// </summary>
public interface IStarWreckEscapeGameMode { }

/// <summary>
/// StarWreckEscape 游戏模式的模块实现
/// 注意：因 NebulaAPI 中 IModuleContainer.AddModule 为 internal，
/// 外部程序集无法实现 IGameModeModule/IModuleContainer。
/// 本类不实现上述接口，模块运行时通过预处理器注册。
/// </summary>
internal class StarWreckEscapeModule
{
    public bool AllowSpecialGameEnd => false;
    public bool ShowMap => true;
    public bool ShowStatistics => false;
    public bool ShowButtons => true;
    public bool CanUseStampOnly => true;
    public bool CanGetTitle => false;
    public bool CanOpenHelpScreen => false;
    public string? GetAlternativeWinOrLoseText() => null;
    public string? GetAlternativePlayerStatusText() => null;
}

namespace hvtXsvc.GameMode.Framework;

/// <summary>
/// 游戏模式模块基类 — 新建模式时只需继承此类，重写需要的属性
/// 
/// 注意：因 NebulaAPI 中 IModuleContainer.AddModule 为 internal，
/// 外部程序集无法实现 IGameModeModule/IModuleContainer 接口。
/// 故此类为纯虚基类，不实现任何 Nebula 接口。
/// 模块注入需通过 RegisterModule 等替代方式完成。
/// </summary>
public abstract class GameModeModuleBase
{
    // ===== 可重写的属性（默认值适合标准玩法） =====
    protected virtual bool AllowSpecialGameEnd => false;
    protected virtual bool ShowMap => true;
    protected virtual bool ShowStatistics => true;
    protected virtual bool ShowButtons => true;
    protected virtual bool CanUseStampOnly => false;
    protected virtual bool CanGetTitle => true;
    protected virtual bool CanOpenHelpScreen => true;
    protected virtual string? GetAlternativeWinOrLoseText() => null;
    protected virtual string? GetAlternativePlayerStatusText() => null;
    protected virtual IEnumerator? GetAlternativeRoutine(bool amHost) => null;
}

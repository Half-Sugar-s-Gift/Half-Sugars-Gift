using Nebula.Game;
using Virial.Game;

namespace hvtXsvc.GameMode.Framework;

/// <summary>
/// 游戏模式注册结果 — 代表一个已注册的模式
/// </summary>
public interface IGameModeRegistration
{
    /// <summary>Nebula 内部的 GameModeDefinition 实例</summary>
    GameModeDefinition Definition { get; }
    /// <summary>本地化键</summary>
    string TranslationKey { get; }
    /// <summary>最小玩家数</summary>
    int MinPlayers { get; }
}

/// <summary>
/// 模块类型提供者 — 指定实现 IGameModeModule 的具体类型
/// </summary>
public interface IGameModeModuleProvider
{
    /// <summary>模块类型（必须实现 IGameModeModule）</summary>
    Type ModuleType { get; }
}

/// <summary>
/// 角色分配器工厂 — 创建 IRoleAllocator 实例
/// </summary>
public interface IGameModeAllocatorFactory
{
    /// <summary>创建角色分配器</summary>
    IRoleAllocator CreateAllocator();
}

/// <summary>
/// 模式属性 — 控制游戏模式的基础行为
/// </summary>
public interface IGameModeProperties
{
    /// <summary>是否允许特殊游戏结束</summary>
    bool AllowSpecialGameEnd { get; }
    /// <summary>是否显示小地图</summary>
    bool ShowMap { get; }
    /// <summary>是否显示统计数据</summary>
    bool ShowStatistics { get; }
    /// <summary>是否能获得称号</summary>
    bool CanGetTitle { get; }
    /// <summary>是否能打开帮助界面</summary>
    bool CanOpenHelpScreen { get; }
}

/// <summary>
/// 可本地化 — 提供自定义本地化键和翻译
/// </summary>
public interface IGameModeLocalizable
{
    /// <summary>模式名称的本地化键</summary>
    string TranslationKey { get; }
}

/// <summary>
/// 可配置 — 提供配置项注册
/// </summary>
public interface IGameModeConfigurable
{
    /// <summary>注册配置项（由模式初始化时调用）</summary>
    void DefineConfigs();
}

/// <summary>
/// 生命周期钩子 — 允许模式在游戏前后执行自定义逻辑
/// </summary>
public interface IGameModeLifecycle
{
    /// <summary>替代协程，如自定义 intro 流程（返回 null 则使用默认）</summary>
    IEnumerator? GetAlternativeRoutine(bool amHost);
}

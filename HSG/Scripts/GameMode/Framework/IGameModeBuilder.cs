using Virial.Assignable;

namespace hvtXsvc.GameMode.Framework;

/// <summary>
/// 构建器入口 — 开始构建游戏模式
/// </summary>
public interface IGameModeBuilderEntry
{
    /// <summary>指定本地化键和最小玩家数，开始构建</summary>
    IGameModeBuilder For(string translationKey, int minPlayers);
}

/// <summary>
/// 游戏模式构建器 — Fluent API，链式调用后以 Register() 结束
/// </summary>
public interface IGameModeBuilder
{
    /// <summary>指定模块类型</summary>
    IGameModeBuilder WithModule<TModule>() where TModule : GameModeModuleBase;

    /// <summary>指定自定义角色分配器</summary>
    IGameModeBuilder WithAllocator(Func<IRoleAllocator> allocatorFactory);

    /// <summary>指定替代协程（如自定义 intro 流程）</summary>
    IGameModeBuilder WithAlternativeRoutine(Func<bool, IEnumerator> routine, bool withRoleSettings = true);

    /// <summary>是否不自动加入模式列表（用于测试）</summary>
    IGameModeBuilder WithoutAutoAdd();

    /// <summary>完成构建并注册到 GameModes</summary>
    IGameModeRegistration Register();
}

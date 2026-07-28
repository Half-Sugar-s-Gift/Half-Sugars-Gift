using hvtXsvc.GameMode.Framework;
using Nebula.Game;
using Nebula.Roles.Assignment;
using Virial.Configuration;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// StarWreckEscape 游戏模式注册
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
internal static class StarWreckEscapeRegistration
{
    /// <summary>本模式的 GameModeDefinition 引用，配置页面用它关联模式</summary>
    public static GameModeDefinition? Definition { get; private set; }

    /// <summary>
    /// Preprocess 方法：在 PostLoadAddons 阶段执行，此时 DIManager 已初始化完毕。
    /// 创建 GameModeDefinition 并在 DIManager.allContainers 中注册代理模块工厂。
    /// </summary>
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        // 注册到 Nebula 的游戏模式列表
        Definition = new GameModeDefinitionImpl(
            "gamemode.starwreckescape",
            4,
            typeof(IStarWreckEscapeGameMode),
            () => new StarWreckEscapeRoleAllocator()
        );

        // 在 DIManager 中注册模块类型
        // 使用 GameModeModuleProxy (Reflection.Emit) 绕过 IModuleContainer.AddModule 为 internal 的限制
        GameModeModuleProxy.RegisterModuleType(
            typeof(IStarWreckEscapeGameMode),
            () => new StarWreckEscapeModule()
        );

        // 同时注册到 IGameModeModule 类型键（兜底：部分 Nebula 版本可能用此类型查找）
        // 返回一个动态代理对象，实现 IGameModeModule 并将调用委托给 StarWreckEscapeModule
        GameModeModuleProxy.RegisterModuleType(
            typeof(IGameModeModule),
            () => new StarWreckEscapeModule()
        );

        HsgDebug.Log("[StarWreckEscape] 模块代理已注册到 DIManager");
    }
}

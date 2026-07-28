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

    static StarWreckEscapeRegistration()
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
    }
}

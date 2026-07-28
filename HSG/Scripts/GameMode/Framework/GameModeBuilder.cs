using Nebula.Game;
using Virial.Assignable;
using Virial.Game;

namespace hvtXsvc.GameMode.Framework;

/// <summary>
/// 游戏模式构建器入口 — Fluent API，链式调用后 Register()
/// </summary>
public static class GameModeBuilder
{
    /// <summary>开始构建</summary>
    public static IGameModeBuilder For(string translationKey, int minPlayers)
        => new Builder(translationKey, minPlayers);

    // ———— 便捷静态方法（一行注册） ————

    /// <summary>快速注册：标准模式，默认 1 内鬼</summary>
    public static IGameModeRegistration Register<TModule>(string key, int minPlayers)
        where TModule : GameModeModuleBase
        => For(key, minPlayers).WithModule<TModule>().Register();

    /// <summary>快速注册：自定义角色分配</summary>
    public static IGameModeRegistration RegisterCustom<TModule>(string key, int minPlayers, Func<IRoleAllocator> allocator)
        where TModule : GameModeModuleBase
        => For(key, minPlayers).WithModule<TModule>().WithAllocator(allocator).Register();

    /// <summary>快速注册：特殊模式（替代协程）</summary>
    public static IGameModeRegistration RegisterSpecial<TModule>(string key, int minPlayers, Func<bool, IEnumerator> routine, bool withRoleSettings = true)
        where TModule : GameModeModuleBase
        => For(key, minPlayers).WithModule<TModule>().WithAlternativeRoutine(routine, withRoleSettings).Register();

    // ———— 私有实现 ————

    private class Builder : IGameModeBuilder, IGameModeRegistration
    {
        private readonly string _key;
        private readonly int _minPlayers;
        private Type? _moduleType;
        private Func<IRoleAllocator>? _allocatorFactory;
        private Func<bool, IEnumerator>? _routine;
        private bool _withRoleSettings = true;
        private bool _noAutoAdd;
        private GameModeDefinition? _definition;

        public Builder(string key, int minPlayers)
        {
            _key = key;
            _minPlayers = minPlayers;
        }

        // ———— IGameModeBuilder ————

        IGameModeBuilder IGameModeBuilder.WithModule<TModule>()
        {
            _moduleType = typeof(TModule);
            return this;
        }

        IGameModeBuilder IGameModeBuilder.WithAllocator(Func<IRoleAllocator> allocatorFactory)
        {
            _allocatorFactory = allocatorFactory;
            return this;
        }

        IGameModeBuilder IGameModeBuilder.WithAlternativeRoutine(Func<bool, IEnumerator> routine, bool withRoleSettings)
        {
            _routine = routine;
            _withRoleSettings = withRoleSettings;
            return this;
        }

        IGameModeBuilder IGameModeBuilder.WithoutAutoAdd()
        {
            _noAutoAdd = true;
            return this;
        }

        IGameModeRegistration IGameModeBuilder.Register()
        {
            var moduleType = _moduleType ?? typeof(GameModeModuleBase);
            var allocator = _allocatorFactory ?? (() => new DefaultRoleAllocator());

            if (_routine != null)
            {
                _definition = new GameModeDefinitionImpl(_key, _minPlayers, moduleType, _routine, _withRoleSettings, _noAutoAdd);
            }
            else
            {
                _definition = new GameModeDefinitionImpl(_key, _minPlayers, moduleType, allocator);
            }

            // 在 DIManager 中注册模块类型，确保运行时 InstantiateModule 能找到它
            if (_moduleType != null && !_noAutoAdd && !_isRegistered)
            {
                _isRegistered = true;
                GameModeModuleProxy.RegisterModuleType(moduleType, () => Activator.CreateInstance(moduleType)!);
            }
            return this;
        }

        private bool _isRegistered;

        // ———— IGameModeRegistration ————

        GameModeDefinition IGameModeRegistration.Definition
            => _definition ?? throw new InvalidOperationException("尚未调用 Register()");

        string IGameModeRegistration.TranslationKey => _key;
        int IGameModeRegistration.MinPlayers => _minPlayers;
    }
}

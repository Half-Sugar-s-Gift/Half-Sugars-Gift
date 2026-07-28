namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 星骸逃生模式配置注册 — 在 FixStructureConfig 阶段执行，
/// 确保配置系统（LocalizedTextComponent 等）已完全初始化，
/// 且 StarWreckEscapeRegistration.Definition 已设置。
/// </summary>
[NebulaPreprocess(PreprocessPhase.FixStructureConfig)]
internal static class StarWreckEscapeConfigRegistration
{
    static StarWreckEscapeConfigRegistration()
    {
        StarWreckEscapeConfig.Register();
    }
}

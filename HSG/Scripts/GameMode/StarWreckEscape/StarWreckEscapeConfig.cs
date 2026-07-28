using Virial.Configuration;
using Virial.Game;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// StarWreckEscape 模式配置项
/// </summary>
internal static class StarWreckEscapeConfig
{
    // 内鬼通风管外最大停留时间（秒）
    static public FloatConfiguration ImpostorOutdoorTimeout = NebulaAPI.Configurations.Configuration(
        "options.starwreck.impostor.outdoor.timeout",
        (5f, 60f, 5f),
        10f,
        FloatConfigurationDecorator.Second
    );

    // 密码输入倒计时（秒）
    static public FloatConfiguration PasswordInputTime = NebulaAPI.Configurations.Configuration(
        "options.starwreck.password.input.time",
        (5f, 60f, 5f),
        15f,
        FloatConfigurationDecorator.Second
    );

    // 第一阶段氧气初始值（秒）
    static public FloatConfiguration OxygenInitialTime = NebulaAPI.Configurations.Configuration(
        "options.starwreck.oxygen.initial.time",
        (30f, 180f, 10f),
        60f,
        FloatConfigurationDecorator.Second
    );

    // 每完成一个任务增加的氧气时间（秒）
    static public FloatConfiguration OxygenPerTask = NebulaAPI.Configurations.Configuration(
        "options.starwreck.oxygen.per.task",
        (5f, 60f, 5f),
        10f,
        FloatConfigurationDecorator.Second
    );

    // 体温变化速率
    static public FloatConfiguration TemperatureChangeRate = NebulaAPI.Configurations.Configuration(
        "options.starwreck.temperature.changerate",
        (0.01f, 0.2f, 0.01f),
        0.05f
    );

    // 死亡温差（与默认体温的差值）
    static public FloatConfiguration TemperatureDeathThreshold = NebulaAPI.Configurations.Configuration(
        "options.starwreck.temperature.death.threshold",
        (0.1f, 2f, 0.1f),
        0.5f
    );

    // 医生默认治愈次数
    static public IntegerConfiguration DoctorCureUses = NebulaAPI.Configurations.Configuration(
        "options.starwreck.doctor.cure.uses",
        (1, 10, 1),
        5
    );

    // ===== 配置页面 Holder =====

    /// <summary>
    /// 创建配置 Holder 并注册到设置菜单
    /// </summary>
    internal static void Register()
    {
        NebulaAPI.Configurations.Holder(
            "options.starwreck",
            [ConfigurationTab.Settings],
            [StarWreckEscapeRegistration.Definition!]
        ).AppendConfigurations([
            ImpostorOutdoorTimeout,
            PasswordInputTime,
            OxygenInitialTime,
            OxygenPerTask,
            TemperatureChangeRate,
            TemperatureDeathThreshold,
            DoctorCureUses,
        ]);
    }
}

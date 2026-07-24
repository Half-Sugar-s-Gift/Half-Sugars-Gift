/*
 * 职业详情页星标按钮 Patch
 * 在职业文档详情页顶部右侧添加星标按钮
 */

using Nebula.Modules.GUIWidget;
using static Nebula.Modules.HelpScreen;

namespace HalfSugarGift.Core.Patch;

/// <summary>
/// 职业详情页星标按钮 Patch
/// </summary>
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public static class DocumentScreenPatch
{
    // FairyStarFlight 插件的 OpenAssignableHelp 方法缓存
    private static MethodInfo? _fairyOpenAssignableHelpMethod;

    /// <summary>
    /// 预处理方法，注册 Harmony Patch
    /// </summary>
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        // 检测 FairyStarFlight 插件是否存在
        var fairyStarFlightType = Type.GetType("FairyStarFlight.Core.PatchManager, FairyStarFlight");
        if (fairyStarFlightType != null)
        {
            HsgDebug.Log("[DocumentScreenPatch] 检测到 FairyStarFlight 插件");

            // 缓存 FairyStarFlight 的 OpenAssignableHelp 方法
            _fairyOpenAssignableHelpMethod = fairyStarFlightType.GetMethod(
                "OpenAssignableHelp",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (_fairyOpenAssignableHelpMethod != null)
            {
                HsgDebug.Log("[DocumentScreenPatch] 已缓存 FairyStarFlight.OpenAssignableHelp 方法");
            }
        }

        var harmony = new Harmony("HSG.DocumentScreenPatch");

        // Patch ShowDocumentScreen 方法
        // 使用 Priority.First 确保我们的 Patch 最先执行
        var showDocumentScreenMethod = typeof(HelpScreen).GetMethod(
            "ShowDocumentScreen",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (showDocumentScreenMethod != null)
        {
            var prefix = new HarmonyMethod(typeof(DocumentScreenPatch).GetMethod(nameof(ShowDocumentScreenPrefix)))
            {
                priority = Priority.First  // 最高优先级，最先执行
            };
            harmony.Patch(showDocumentScreenMethod, prefix: prefix);
            HsgDebug.Log("[DocumentScreenPatch] 已 Patch ShowDocumentScreen 方法（Priority.First）");
        }
    }

    /// <summary>
    /// Prefix 方法：拦截 ShowDocumentScreen，对 assignable 类型添加星标按钮
    /// </summary>
    public static bool ShowDocumentScreenPrefix(IDocument doc, ref MetaScreen? __result)
    {
        // 只处理可分配对象（角色/修改器/幽灵角色）
        var assignable = doc.RelatedAssignable;
        if (assignable == null)
        {
            return true; // 非 assignable 文档，继续执行原方法
        }

        try
        {
            __result = OpenAssignableHelpWithStar(assignable);
            return false; // 阻止原方法执行
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"ShowDocumentScreenPrefix 失败: {ex.Message}");
            return true; // 出错时继续执行原方法
        }
    }

    /// <summary>
    /// 自定义的 OpenAssignableHelp 方法，在详情页添加星标按钮
    /// </summary>
    private static MetaScreen? OpenAssignableHelpWithStar(DefinedAssignable assignable)
    {
        MetaScreen? screen = null;

        // 如果 FairyStarFlight 存在，调用其方法创建窗口
        if (_fairyOpenAssignableHelpMethod != null)
        {
            try
            {
                screen = _fairyOpenAssignableHelpMethod.Invoke(null, new object[] { assignable }) as MetaScreen;
                HsgDebug.Log($"[DocumentScreenPatch] 使用 FairyStarFlight 创建窗口: {assignable.DisplayName}");
            }
            catch (Exception ex)
            {
                HsgDebug.LogError($"[DocumentScreenPatch] 调用 FairyStarFlight.OpenAssignableHelp 失败: {ex.Message}");
                screen = null;
            }
        }

        // 如果 FairyStarFlight 不存在或调用失败，使用原版方法
        if (screen == null)
        {
            var doc = DocumentManager.GetDocument("role." + assignable.InternalName);
            if (doc == null) return null;

            // 创建窗口
            float width = Mathn.Max(doc.RequiredWidth ?? 7f, 7f);
            float height = Mathn.Max(doc.RequiredHeight ?? 4.5f, 4.5f);

            screen = MetaScreen.GenerateWindow(
                new Vector2(width, height),
                AmongUsLLImpl.HudManagerInstance.transform,
                Vector3.zero,
                true, true,
                background: BackgroundSetting.Modern);

            // 创建滚动视图
            Virial.Compat.Artifact<GUIScreen>? inner = null;
            var scrollView = new GUIScrollView(GUIAlignment.Left, new Virial.Compat.Vector2(width, height), () => doc.Build(inner) ?? GUIEmptyWidget.Default);
            inner = scrollView.Artifact;

            screen.SetWidget(scrollView, doc.Illustlation, out _);
        }

        // 添加星标按钮
        AddStarButton(screen, assignable);

        return screen;
    }

    /// <summary>
    /// 添加星标按钮到窗口右上角
    /// </summary>
    private static void AddStarButton(MetaScreen screen, DefinedAssignable assignable)
    {
        try
        {
            // 生成可分配对象ID
            string id = "role." + assignable.InternalName;

            // 获取当前星标状态
            bool isStarred = StarsManager.IsStarred(id);

            // 加载图标
            var image = NebulaAPI.AddonAsset.GetResource(
                "Star/" + (isStarred ? "YesStar" : "NoStar") + ".png"
            )?.AsImage(100f);

            if (image == null)
            {
                HsgDebug.LogError("无法加载星标图标");
                return;
            }

            // 在窗口顶部右侧创建星标按钮
            // 位置：窗口右上角
            var buttonObj = UnityHelper.CreateObject<SpriteRenderer>(
                "StarButton",
                screen.transform,
                new Vector3(2.9f, 2.0f, -1f)
            );

            // 设置按钮图标
            buttonObj.sprite = image.GetSprite();
            buttonObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

            // 添加碰撞器
            var collider = buttonObj.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.4f, 0.4f);
            collider.isTrigger = true;

            // 设置按钮行为
            var button = buttonObj.gameObject.SetUpButton(true);

            // 点击事件：切换星标状态
            button.OnClick.AddListener(() =>
            {
                try
                {
                    // 切换星标状态
                    StarsManager.ToggleStar(id);

                    // 获取新状态
                    bool newStarred = StarsManager.IsStarred(id);

                    // 重新加载图标
                    var newImage = NebulaAPI.AddonAsset.GetResource(
                        "Star/" + (newStarred ? "YesStar" : "NoStar") + ".png"
                    )?.AsImage(100f);

                    // 更新按钮图标
                    if (newImage != null && buttonObj != null)
                    {
                        buttonObj.sprite = newImage.GetSprite();
                    }

                    HsgDebug.Log($"已切换星标状态: {assignable.DisplayName} -> {newStarred}");
                }
                catch (Exception ex)
                {
                    HsgDebug.LogError($"切换星标失败: {ex.Message}");
                }
            });

            HsgDebug.Log($"已添加星标按钮: {assignable.DisplayName}");
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"添加星标按钮失败: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
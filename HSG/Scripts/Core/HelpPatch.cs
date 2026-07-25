using Nebula.Modules.GUIWidget;
using Nebula.Modules.MetaWidget;
using static Nebula.Modules.HelpScreen;

namespace HalfSugarGift.Core.Patch;

[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public static class HelpPatch
{
    private const string StarsTabTranslateKey = "help.tabs.stars";

    private static readonly TextAttributeOld TabButtonAttr = new(TextAttributeOld.BoldAttr)
    {
        Size = new Vector2(0.82f, 0.21f),
        FontSize = 1.6f,
        FontMaxSize = 1.6f
    };

    private static readonly TextAttributeOld RoleTitleAttr = new(TextAttributeOld.BoldAttr)
    {
        Size = new Vector2(1.2f, 0.29f),
        FontMaterial = VanillaAsset.StandardMaskedFontMaterial
    };

    private static MethodInfo? _cachedOpenAssignableHelpMethod;

    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        Language.Register(StarsTabTranslateKey, () => "星标");
        Language.Register("help.stars.empty", () => "暂无星标，点击职业详情页的星标按钮添加");

        _cachedOpenAssignableHelpMethod = typeof(HelpScreen).GetMethod(
            "OpenAssignableHelp",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        var harmony = new Harmony("HSG.HelpPatch");

        var getTabsWidgetMethod = typeof(HelpScreen).GetMethod(
            "GetTabsWidget",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (getTabsWidgetMethod != null)
        {
            var postfix = new HarmonyMethod(typeof(HelpPatch).GetMethod(nameof(GetTabsWidgetPostfix)));
            harmony.Patch(getTabsWidgetMethod, postfix: postfix);
            HsgDebug.Log("[HelpPatch] 已 Patch GetTabsWidget 方法");
        }
    }

    public static void GetTabsWidgetPostfix(MetaScreen screen, HelpTab tab, HelpTab validTabs, ref IMetaWidgetOld __result)
    {
        try
        {
            List<IMetaParallelPlacableOld> tabs = new();

            foreach (var info in AllHelpTabInfo)
            {
                if ((validTabs & info.Tab) != 0)
                {
                    tabs.Add(info.GetButton(screen, tab, validTabs));
                }
            }

            bool isStarTab = (int)(object)tab == 0;

            tabs.Add(new MetaWidgetOld.Button(() =>
            {
                ShowStarsContent(screen);
            }, TabButtonAttr)
            {
                TranslationKey = StarsTabTranslateKey,
                Color = isStarTab ? Virial.Color.White : Virial.Color.Gray,
                Alignment = IMetaWidgetOld.AlignmentOption.Center
            });

            __result = new CombinedWidgetOld(0.5f, tabs.ToArray());
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"[HelpPatch] GetTabsWidgetPostfix 失败: {ex.Message}");
        }
    }

    private static void ShowStarsContent(MetaScreen screen)
    {
        var content = BuildStarsContent();

        // 局内（游戏已开始）打开独立窗口，不干扰 MyInfo
        if (AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.Started)
        {
            var starScreen = MetaScreen.GenerateWindow(
                new Vector2(7.8f, 5f),
                AmongUsLLImpl.HudManagerInstance.transform,
                Vector3.zero,
                true, true,
                background: BackgroundSetting.Modern);

            MetaWidgetOld widget = new();
            widget.Append(new MetaWidgetOld.VerticalMargin(0.1f));
            widget.Append(content);

            starScreen.SetWidget(widget);
            return;
        }

        MetaWidgetOld widget2 = new();
        HelpTab validTabs = HelpTab.Search | HelpTab.Roles | HelpTab.Overview | HelpTab.Options | HelpTab.Achievements | HelpTab.Stamps;

        widget2.Append(GetTabsWidget(screen, (HelpTab)0, validTabs));
        widget2.Append(new MetaWidgetOld.VerticalMargin(0.1f));
        widget2.Append(content);

        screen.SetWidget(widget2);
        screen.SetBackImage(null, 0.2f);
    }

    private static IMetaWidgetOld BuildStarsContent()
    {
        var starred = StarsManager.GetAllStarred().ToList();

        if (starred.Count == 0)
        {
            return new MetaWidgetOld.Text(new TextAttributeOld(TextAttributeOld.BoldAttr)
            {
                Size = new Vector2(5f, 0.5f),
                Alignment = TMPro.TextAlignmentOptions.Center,
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial
            })
            {
                RawText = Language.Translate("help.stars.empty"),
                Alignment = IMetaWidgetOld.AlignmentOption.Center
            };
        }

        MetaWidgetOld inner = new();

        void AddCategory(string categoryKey, IEnumerable<DefinedAssignable> assignables, Virial.Color color)
        {
            var list = assignables.ToList();
            if (list.Count == 0) return;

            if (inner.Count > 0)
                inner.Append(new MetaWidgetOld.VerticalMargin(0.2f));

            inner.Append(new MetaWidgetOld.WrappedWidget(
                NebulaAPI.GUI.Text(
                    GUIAlignment.Left,
                    NebulaAPI.GUI.GetAttribute(AttributeAsset.DocumentTitle),
                    new ColorTextComponent(color, new TranslateTextComponent(categoryKey))
                )
            ));

            inner.Append(new MetaWidgetOld.VerticalMargin(0.1f));

            inner.Append(list, CreateAssignableButton, 4, -1, 0, 0.6f);
        }

        AddCategory("role.category.impostor",
            starred.Where(r => r is DefinedRole role && role.Category == RoleCategory.ImpostorRole),
            Virial.Color.ImpostorColor);

        AddCategory("role.category.neutral",
            starred.Where(r => r is DefinedRole role && role.Category == RoleCategory.NeutralRole),
            new Virial.Color(1f, 0.7f, 0f));

        AddCategory("role.category.crewmate",
            starred.Where(r => r is DefinedRole role && role.Category == RoleCategory.CrewmateRole),
            Virial.Color.CrewmateColor);

        AddCategory("role.category.ghost",
            starred.Where(r => r is DefinedGhostRole),
            Virial.Color.Gray);

        AddCategory("role.category.modifier",
            starred.Where(r => r is DefinedModifier),
            Virial.Color.White);

        return new MetaWidgetOld.ScrollView(new Vector2(7.4f, 4.1f), inner)
        {
            Alignment = IMetaWidgetOld.AlignmentOption.Center
        };
    }

    private static IMetaParallelPlacableOld CreateAssignableButton(DefinedAssignable assignable)
    {
        return new CombinedWidgetOld(
            new MetaWidgetOld.HorizonalMargin(0.12f),
            new MetaWidgetOld.Button(() =>
            {
                OpenAssignableHelpCustom(assignable);
            }, RoleTitleAttr)
            {
                RawText = assignable.DisplayColoredName,
                PostBuilder = (PassiveButton button, SpriteRenderer renderer, TMPro.TextMeshPro text) =>
                {
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    button.OnMouseOver.AddListener(() =>
                    {
                        NebulaManager.Instance.SetHelpWidget(button, GetAssignableOverlay(assignable));
                    });
                    button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
                    text.transform.localPosition += new Vector3(0.07f, 0f, 0f);
                    button.transform.localPosition -= new Vector3(0.15f, 0f, 0f);

                    var roleIcon = assignable.GetRoleIcon()?.GetSprite();
                    if (roleIcon != null)
                    {
                        var icon = UnityHelper.CreateObject<SpriteRenderer>("Icon", button.transform, new Vector3(-0.73f, 0f, -0.01f));
                        icon.sprite = roleIcon;
                        icon.material = RoleIcon.GetRoleIconMaterial(assignable, 0.8f);
                        icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                        icon.transform.localScale = new Vector3(0.275f, 0.275f, 1f);
                    }
                },
                Alignment = IMetaWidgetOld.AlignmentOption.Center,
                TextHorizonotalExtraMargin = 0.15f,
            });
    }

    private static void OpenAssignableHelpCustom(DefinedAssignable assignable)
    {
        try
        {
            if (_cachedOpenAssignableHelpMethod != null)
            {
                _cachedOpenAssignableHelpMethod.Invoke(null, new object[] { assignable });
            }
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"[HelpPatch] OpenAssignableHelpCustom 失败: {ex.Message}");
        }
    }

    private static GUIWidget GetAssignableOverlay(DefinedAssignable assignable)
    {
        List<GUIWidget> widgets = new();

        widgets.Add(NebulaAPI.GUI.RawText(
            GUIAlignment.Left,
            NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayTitle),
            assignable.DisplayColoredName
        ));

        widgets.Add(NebulaAPI.GUI.RawText(
            GUIAlignment.Left,
            NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayContent),
            ""
        ));

        var detail = assignable.ConfigurationHolder?.Detail;
        if (detail != null)
        {
            widgets.Add(NebulaAPI.GUI.Text(
                GUIAlignment.Left,
                NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayContent),
                detail
            ));
        }

        if (assignable is HasCitation hc && hc.Citation != null)
        {
            var citation = hc.Citation;
            widgets.Add(NebulaAPI.GUI.Margin(new FuzzySize(null, 0.35f)));
            widgets.Add(NebulaAPI.GUI.HorizontalHolder(GUIAlignment.Left,
                NebulaAPI.GUI.RawText(GUIAlignment.Bottom, NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayContent), "from"),
                NebulaAPI.GUI.HorizontalMargin(0.12f),
                citation.LogoImage != null
                    ? NebulaAPI.GUI.Image(GUIAlignment.Bottom, citation.LogoImage, new FuzzySize(1.5f, 0.37f))
                    : NebulaAPI.GUI.Text(GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayTitle), citation.Name)
            ));
        }

        var holder = NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left, widgets);
        holder.BackImage = assignable.ConfigurationHolder?.Illustration;
        return holder;
    }
}

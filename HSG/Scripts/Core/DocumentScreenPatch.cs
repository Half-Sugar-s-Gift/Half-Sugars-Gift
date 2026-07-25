using Nebula.Modules.GUIWidget;
using static Nebula.Modules.HelpScreen;

namespace HalfSugarGift.Core.Patch;

[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public static class DocumentScreenPatch
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        var harmony = new Harmony("HSG.DocumentScreenPatch");

        var openAssignableHelpMethod = typeof(HelpScreen).GetMethod(
            "OpenAssignableHelp",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (openAssignableHelpMethod != null)
        {
            var postfix = new HarmonyMethod(typeof(DocumentScreenPatch).GetMethod(nameof(OpenAssignableHelpPostfix)));
            harmony.Patch(openAssignableHelpMethod, postfix: postfix);
            HsgDebug.Log("[DocumentScreenPatch] 已 Patch OpenAssignableHelp 方法（Postfix）");
        }

        var showDocumentScreenMethod = typeof(HelpScreen).GetMethod(
            "ShowDocumentScreen",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (showDocumentScreenMethod != null)
        {
            var postfix = new HarmonyMethod(typeof(DocumentScreenPatch).GetMethod(nameof(ShowDocumentScreenPostfix)));
            harmony.Patch(showDocumentScreenMethod, postfix: postfix);
            HsgDebug.Log("[DocumentScreenPatch] 已 Patch ShowDocumentScreen 方法（Postfix）");
        }
    }

    public static void OpenAssignableHelpPostfix(DefinedAssignable assignable, MetaScreen? __result)
    {
        if (assignable == null || __result == null) return;

        try
        {
            AddStarButton(__result, assignable);
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"OpenAssignableHelpPostfix 失败: {ex.Message}");
        }
    }

    public static void ShowDocumentScreenPostfix(IDocument doc, MetaScreen __result)
    {
        var assignable = doc.RelatedAssignable;
        if (assignable == null || __result == null) return;

        try
        {
            AddStarButton(__result, assignable);
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"ShowDocumentScreenPostfix 失败: {ex.Message}");
        }
    }

    private static void AddStarButton(MetaScreen screen, DefinedAssignable assignable)
    {
        try
        {
            string id = "role." + assignable.InternalName;
            bool isStarred = StarsManager.IsStarred(id);

            var image = NebulaAPI.AddonAsset.GetResource(
                "Star/" + (isStarred ? "YesStar" : "NoStar") + ".png"
            )?.AsImage(100f);

            if (image == null)
            {
                HsgDebug.LogError("无法加载星标图标");
                return;
            }

            var buttonObj = UnityHelper.CreateObject<SpriteRenderer>(
                "StarButton",
                screen.transform,
                new Vector3(2.9f, 2.0f, -1f)
            );

            buttonObj.sprite = image.GetSprite();
            buttonObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

            var collider = buttonObj.gameObject.AddComponent<BoxCollider2D>();
            if (buttonObj.sprite != null)
                collider.size = buttonObj.sprite.bounds.size;
            else
                collider.size = new Vector2(0.4f, 0.4f);
            collider.isTrigger = true;

            var button = buttonObj.gameObject.SetUpButton(true);

            button.OnClick.AddListener(() =>
            {
                try
                {
                    StarsManager.ToggleStar(id);
                    bool newStarred = StarsManager.IsStarred(id);

                    var newImage = NebulaAPI.AddonAsset.GetResource(
                        "Star/" + (newStarred ? "YesStar" : "NoStar") + ".png"
                    )?.AsImage(100f);

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

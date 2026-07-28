using TMPro;

namespace hvtXsvc.GameMode.StarWreckEscape;

/// <summary>
/// 阶段标题卡片系统，用于 StarWreckEscape 各阶段切换时的全屏标题展示
/// </summary>
public static class TitleCardUI
{
    /// <summary>
    /// 三个阶段的预定义标题内容
    /// </summary>
    public static readonly (string title, string subtitle)[] PhaseTitles = new[]
    {
        ("第一章 薪火未熄", "在这艘铁棺材里，杀死你的从来不是怪物——是氧气一点一点归零的声音。抓住每一分，每一秒"),
        ("冰与火共舞", "距离不是你的朋友，是你的审判官。"),
        ("孢子黎明", "最后一个人站着的时候，他面对的不只是一只怪——是所有曾经的队友。"),
    };

    /// <summary>
    /// 获取指定阶段的标题内容
    /// </summary>
    /// <param name="phaseIndex">阶段索引（0-based）</param>
    /// <returns>(大标题, 小标题)</returns>
    public static (string title, string subtitle) GetPhaseTitle(int phaseIndex)
    {
        if (phaseIndex >= 0 && phaseIndex < PhaseTitles.Length)
            return PhaseTitles[phaseIndex];
        return PhaseTitles[0];
    }

    /// <summary>
    /// 显示全屏标题卡片，带淡入→停留→淡出动画
    /// </summary>
    /// <param name="title">大标题文字</param>
    /// <param name="subtitle">小标题文字（将用 &lt;size=60%&gt; 渲染）</param>
    /// <param name="callback">动画完成后回调</param>
    public static void ShowTitle(string title, string subtitle, Action? callback = null)
    {
        if (HudManager.Instance == null) return;
        if (HudManager.Instance.IntroPrefab == null) return;
        HudManager.Instance.StartCoroutine(CoShowTitle(title, subtitle, callback).WrapToIl2Cpp());
    }

    private static IEnumerator CoShowTitle(string title, string subtitle, Action? callback)
    {
        var hud = HudManager.Instance;
        if (hud == null || hud.IntroPrefab == null) yield break;

        // 创建全屏暗色覆盖层（参考 PatchManager.ShowScreenOverlay）
        var overlay = GameObject.Instantiate(hud.FullScreen, hud.transform);
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.enabled = true;
        overlay.gameObject.SetActive(true);

        // 创建大标题文本（参考 NebulaGameManager.TitleShower）
        var titleText = GameObject.Instantiate(hud.IntroPrefab.ImpostorTitle, overlay.transform);
        titleText.GetComponent<TextTranslatorTMP>().enabled = false;
        titleText.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        titleText.rectTransform.localScale = new Vector3(3f, 3f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(2.4f, 3f);
        titleText.outlineColor = new Color32(0, 0, 0, 0);
        titleText.text = title;
        titleText.color = new Color(1f, 1f, 1f, 0f);

        // 创建副标题文本
        var subtitleText = GameObject.Instantiate(hud.IntroPrefab.ImpostorTitle, overlay.transform);
        subtitleText.GetComponent<TextTranslatorTMP>().enabled = false;
        subtitleText.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        subtitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        subtitleText.rectTransform.localScale = new Vector3(2f, 2f, 1f);
        subtitleText.rectTransform.sizeDelta = new Vector2(2.4f, 3f);
        subtitleText.outlineColor = new Color32(0, 0, 0, 0);
        subtitleText.text = $"<size=60%>{subtitle}</size>";
        subtitleText.color = new Color(1f, 1f, 1f, 0f);

        // 动画参数
        float fadeInDuration = 2f;
        float holdDuration = 3f;
        float fadeOutDuration = 2f;

        // —— 淡入 ——
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float alpha = Mathf.Lerp(0f, 1f, t);

            var c = overlay.color;
            c.a = alpha * 0.7f;
            overlay.color = c;
            titleText.color = new Color(1f, 1f, 1f, alpha);
            subtitleText.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        // 确保最终状态
        {
            var c = overlay.color;
            c.a = 0.7f;
            overlay.color = c;
            titleText.color = Color.white;
            subtitleText.color = Color.white;
        }

        // —— 停留 ——
        yield return new WaitForSeconds(holdDuration);

        // —— 淡出 ——
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);

            var c = overlay.color;
            c.a = alpha * 0.7f;
            overlay.color = c;
            titleText.color = new Color(1f, 1f, 1f, alpha);
            subtitleText.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        // —— 清理 ——
        GameObject.Destroy(subtitleText.gameObject);
        GameObject.Destroy(titleText.gameObject);
        overlay.enabled = false;
        GameObject.Destroy(overlay.gameObject);

        // —— 回调 ——
        callback?.Invoke();
    }
}

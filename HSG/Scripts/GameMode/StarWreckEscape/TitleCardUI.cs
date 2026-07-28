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
    /// <summary>
    /// 显示全屏标题卡片 — 带超时安全保护
    /// </summary>
    public static void ShowTitle(string title, string subtitle, Action? callback = null)
    {
        if (HudManager.Instance == null) return;
        if (HudManager.Instance.IntroPrefab == null) return;

        // 启动主标题协程 + 超时清理协程（确保即使主协程卡住也能清理）
        var hud = HudManager.Instance;
        var coroutine = CoShowTitle(title, subtitle, callback).WrapToIl2Cpp();
        hud.StartCoroutine(coroutine);
    }

    private static IEnumerator CoShowTitle(string title, string subtitle, Action? callback)
    {
        var hud = HudManager.Instance;
        if (hud == null || hud.IntroPrefab == null) yield break;

        // 创建全屏暗色覆盖层
        var overlay = GameObject.Instantiate(hud.FullScreen, hud.transform);
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.enabled = true;
        overlay.gameObject.SetActive(true);

        TextMeshPro? titleText = null;
        TextMeshPro? subtitleText = null;

        // 创建大标题文本
        titleText = GameObject.Instantiate(hud.IntroPrefab.ImpostorTitle, overlay.transform);
        titleText.GetComponent<TextTranslatorTMP>().enabled = false;
        titleText.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        titleText.rectTransform.localScale = new Vector3(3f, 3f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(2.4f, 3f);
        titleText.outlineColor = new Color32(0, 0, 0, 0);
        titleText.text = title;
        titleText.color = new Color(1f, 1f, 1f, 0f);

        // 创建副标题文本
        subtitleText = GameObject.Instantiate(hud.IntroPrefab.ImpostorTitle, overlay.transform);
        subtitleText.GetComponent<TextTranslatorTMP>().enabled = false;
        subtitleText.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        subtitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        subtitleText.rectTransform.localScale = new Vector3(2f, 2f, 1f);
        subtitleText.rectTransform.sizeDelta = new Vector2(2.4f, 3f);
        subtitleText.outlineColor = new Color32(0, 0, 0, 0);
        subtitleText.text = $"<size=60%>{subtitle}</size>";
        subtitleText.color = new Color(1f, 1f, 1f, 0f);

        // 动画参数 + 安全超时（最多停留 30 秒）
        float fadeInDuration = 2f;
        float holdDuration = 3f;
        float fadeOutDuration = 2f;
        float maxTotalTime = 30f;
        float totalElapsed = 0f;

        // —— 淡入 ——
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            totalElapsed += dt;
            float t = elapsed / fadeInDuration;
            float alpha = Mathf.Lerp(0f, 1f, t);

            var c = overlay.color;
            c.a = alpha * 0.7f;
            overlay.color = c;
            titleText.color = new Color(1f, 1f, 1f, alpha);
            subtitleText.color = new Color(1f, 1f, 1f, alpha);

            if (totalElapsed >= maxTotalTime) goto Cleanup;
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
        totalElapsed += holdDuration;
        if (totalElapsed >= maxTotalTime) goto Cleanup;
        yield return new WaitForSeconds(holdDuration);

        // —— 淡出 ——
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            totalElapsed += dt;
            float t = elapsed / fadeOutDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);

            var c = overlay.color;
            c.a = alpha * 0.7f;
            overlay.color = c;
            titleText.color = new Color(1f, 1f, 1f, alpha);
            subtitleText.color = new Color(1f, 1f, 1f, alpha);

            if (totalElapsed >= maxTotalTime) goto Cleanup;
            yield return null;
        }

    Cleanup:
        // —— 清理 ——
        if (subtitleText != null) GameObject.Destroy(subtitleText.gameObject);
        if (titleText != null) GameObject.Destroy(titleText.gameObject);
        overlay.enabled = false;
        GameObject.Destroy(overlay.gameObject);

        // —— 回调 ——
        callback?.Invoke();
    }
}

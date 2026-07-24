using static Nebula.Roles.Roles;

namespace HalfSugarGift.Core;

/// <summary>
/// 星标数据管理器，用于管理角色、修改器和幽灵角色的星标状态
/// </summary>
public static class StarsManager
{
    // 星标数据存储路径
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "Star", "starred.json");

    // 星标数据实例
    private static StarData? _data;

    // 静态构造函数，初始化时加载数据
    static StarsManager()
    {
        Load();
    }

    /// <summary>
    /// 检查指定ID是否已星标
    /// </summary>
    /// <param name="assignableId">可分配对象ID（如 "role.impostor"）</param>
    /// <returns>已星标返回 true，否则返回 false</returns>
    public static bool IsStarred(string assignableId)
    {
        return _data?.starredIds?.Contains(assignableId) ?? false;
    }

    /// <summary>
    /// 切换星标状态（已星标则取消，未星标则添加）
    /// </summary>
    /// <param name="assignableId">可分配对象ID</param>
    public static void ToggleStar(string assignableId)
    {
        if (_data == null) return;

        if (_data.starredIds.Contains(assignableId))
        {
            _data.starredIds.Remove(assignableId);
        }
        else
        {
            _data.starredIds.Add(assignableId);
        }
        Save();
    }

    /// <summary>
    /// 获取所有星标的可分配对象
    /// </summary>
    /// <returns>星标对象的枚举集合</returns>
    public static IEnumerable<DefinedAssignable> GetAllStarred()
    {
        if (_data?.starredIds == null) yield break;

        // 遍历所有角色
        foreach (var role in AllRoles)
        {
            string id = "role." + role.InternalName;
            if (_data?.starredIds?.Contains(id) ?? false)
                yield return role;
        }

        // 遍历所有修改器
        foreach (var modifier in AllModifiers)
        {
            string id = "role." + modifier.InternalName;
            if (_data?.starredIds?.Contains(id) ?? false)
                yield return modifier;
        }

        // 遍历所有幽灵角色
        foreach (var ghostRole in AllGhostRoles)
        {
            string id = "role." + ghostRole.InternalName;
            if (_data?.starredIds?.Contains(id) ?? false)
                yield return ghostRole;
        }
    }

    /// <summary>
    /// 从文件加载星标数据
    /// </summary>
    private static void Load()
    {
        try
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 文件不存在则创建空数据
            if (!File.Exists(FilePath))
            {
                _data = new StarData();
                Save();
                return;
            }

            // 读取并反序列化
            string json = File.ReadAllText(FilePath);
            _data = JsonStructure.Deserialize<StarData>(json);

            // 反序列化失败则创建新数据
            if (_data == null)
            {
                _data = new StarData();
            }
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"加载星标数据失败: {ex.Message}");
            _data = new StarData();
        }
    }

    /// <summary>
    /// 保存星标数据到文件
    /// </summary>
    private static void Save()
    {
        try
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 序列化并写入文件
            string json = JsonStructure.Serialize(_data);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"保存星标数据失败: {ex.Message}");
        }
    }
}

/// <summary>
/// 星标数据结构
/// </summary>
[Serializable]
public class StarData
{
    /// <summary>
    /// 已星标的ID列表（格式："role.内部名称"）
    /// </summary>
    [JsonSerializableField(true, false)]
    public HashSet<string> starredIds = new HashSet<string>();
}
using static Nebula.Roles.Roles;

namespace HalfSugarGift.Core;

public static class StarsManager
{
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "Star", "starred.json");

    private static StarData? _data;

    static StarsManager()
    {
        Load();
    }

    public static bool IsStarred(string assignableId)
    {
        return _data?.starredIds?.Contains(assignableId) ?? false;
    }

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

    public static IEnumerable<DefinedAssignable> GetAllStarred()
    {
        if (_data?.starredIds == null) yield break;

        foreach (var role in AllRoles)
        {
            string id = "role." + role.InternalName;
            if (_data?.starredIds?.Contains(id) ?? false)
                yield return role;
        }

        foreach (var modifier in AllModifiers)
        {
            string id = "role." + modifier.InternalName;
            if (_data?.starredIds?.Contains(id) ?? false)
                yield return modifier;
        }

        foreach (var ghostRole in AllGhostRoles)
        {
            string id = "role." + ghostRole.InternalName;
            if (_data?.starredIds?.Contains(id) ?? false)
                yield return ghostRole;
        }
    }

    private static void Load()
    {
        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(FilePath))
            {
                _data = new StarData();
                Save();
                return;
            }

            string json = File.ReadAllText(FilePath);
            _data = JsonStructure.Deserialize<StarData>(json);

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

    private static void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonStructure.Serialize(_data);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            HsgDebug.LogError($"保存星标数据失败: {ex.Message}");
        }
    }
}

[Serializable]
public class StarData
{
    [JsonSerializableField(true, false)]
    public List<string> starredIds = new List<string>();
}

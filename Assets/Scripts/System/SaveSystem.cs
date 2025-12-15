using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private static string GetSaveFilePath(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);
        return Application.persistentDataPath + "/player" + slot + ".json";
    }

    private static float[] playTimeCache = new float[4]; // 1-based index for slots 1-3

    public static void SavePlayer(Player_Controller player, XP_System xp, Player_Health health, Inventory inventory, int slot)
    {
        float playTime = playTimeCache[slot];
        PlayerData data = new PlayerData(player, xp, health, inventory, playTime);
        var saveableEntities = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (var saveable in saveableEntities)
        {
            var guidComponent = (saveable as MonoBehaviour).GetComponent<GuidComponent>();
            if (guidComponent != null)
            {
                data.worldData[guidComponent.GetGuid()] = saveable.CaptureState();
            }
        }
        WritePlayerData(data, slot);
    }

    // Call this every frame from a manager to track play time for the current slot
    public static void AddPlayTime(float deltaTime, int slot)
    {
        if (slot < 1 || slot > 3) return;
        playTimeCache[slot] += deltaTime;
    }

    // Optionally, call this when loading a save to restore play time
    public static void SetPlayTime(float playTime, int slot)
    {
        if (slot < 1 || slot > 3) return;
        playTimeCache[slot] = playTime;
    }

    public static void CreateNewGameSave(PlayerStats stats, string targetScene, Vector3 spawnPosition, int slot)
    {
        Debug.Log($"[SaveSystem] Creating new game save for slot {slot}");
        Debug.Log($"[SaveSystem] Spawn position being saved: {spawnPosition}");
        Debug.Log($"[SaveSystem] Target scene: {targetScene}");
        
        int baseLevel = 1;
        int baseMaxXp = stats != null ? stats.Level1XpAmount : 10;
        int baseHealth = stats != null ? stats.GetHealth(baseLevel) : 10;

        var data = new PlayerData
        {
            level = baseLevel,
            currentXp = 0,
            maxXp = Mathf.Max(1, baseMaxXp),
            currentHealth = Mathf.Max(1, baseHealth),
            maxHealth = Mathf.Max(1, baseHealth),
            position = new float[] { spawnPosition.x, spawnPosition.y, spawnPosition.z },
            sceneName = string.IsNullOrEmpty(targetScene) ? SceneManager.GetActiveScene().name : targetScene,
            worldData = new Dictionary<string, object>(),
            inventoryItemNames = new List<string>(),
            inventoryResourceNames = new List<string>(),
            inventoryItemQuantities = new List<int>(),
            equippedItemName = null,
            equippedResourceName = null,
            mainWeaponType = Player_Controller.MainWeaponType.Melee.ToString()
        };
        WritePlayerData(data, slot);
        Debug.Log($"[SaveSystem] New game save created successfully at slot {slot}");
    }

    public static PlayerData LoadPlayer(int slot)
    {
        string path = GetSaveFilePath(slot);
        if (File.Exists(path))
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            string json = File.ReadAllText(path);
            PlayerData data = JsonConvert.DeserializeObject<PlayerData>(json, settings);
            Debug.Log("Game Loaded from: " + path);
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found in " + path);
            return null;
        }
    }

    public static void DeleteSave(int slot)
    {
        string path = GetSaveFilePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Save file deleted: {path}");
            
            // Reset play time for this slot
            if (slot >= 1 && slot <= 3)
            {
                playTimeCache[slot] = 0f;
            }
        }
        else
        {
            Debug.LogWarning($"No save file found to delete at: {path}");
        }
    }

    public static string GetSavedSceneName(int slot)
    {
        string path = GetSaveFilePath(slot);
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            var jo = JObject.Parse(json);
            var token = jo["sceneName"];
            return token != null ? token.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void WritePlayerData(PlayerData data, int slot)
    {
        if (data == null)
        {
            Debug.LogWarning("SaveSystem: No data provided to write.");
            return;
        }
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        string json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
        string path = GetSaveFilePath(slot);
        File.WriteAllText(path, json);
        Debug.Log("Game Saved to: " + path);
    }

    public static void RestoreWorldState(PlayerData data)
    {
        var saveableEntities = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (var saveable in saveableEntities)
        {
            var guidComponent = (saveable as MonoBehaviour).GetComponent<GuidComponent>();
            if (guidComponent != null && data.worldData.TryGetValue(guidComponent.GetGuid(), out object savedState))
            {
                saveable.RestoreState(savedState);
            }
        }
    }
}

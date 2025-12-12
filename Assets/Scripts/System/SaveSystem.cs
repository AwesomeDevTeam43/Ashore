using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private static readonly string SAVE_FILE = "/player.json";
    private static string SaveFilePath => Application.persistentDataPath + SAVE_FILE;

    public static void SavePlayer(Player_Controller player, XP_System xp, Player_Health health, Inventory inventory)
    {
        PlayerData data = new PlayerData(player, xp, health, inventory);

    var saveableEntities = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();

        foreach (var saveable in saveableEntities)
        {
            var guidComponent = (saveable as MonoBehaviour).GetComponent<GuidComponent>();
            if (guidComponent != null)
            {
                data.worldData[guidComponent.GetGuid()] = saveable.CaptureState();
            }
        }

        WritePlayerData(data);
    }

    public static void CreateNewGameSave(PlayerStats stats, string targetScene, Vector3 spawnPosition)
    {
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

        WritePlayerData(data);
    }

    public static PlayerData LoadPlayer()
    {
        string path = SaveFilePath;
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

    public static string GetSavedSceneName()
    {
        string path = SaveFilePath;
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

    private static void WritePlayerData(PlayerData data)
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

        File.WriteAllText(SaveFilePath, json);
        Debug.Log("Game Saved to: " + SaveFilePath);
    }

    public static void RestoreWorldState(PlayerData data)
    {
        if (data == null || data.worldData == null) return;
        
        var saveableEntities = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();

        foreach (var saveable in saveableEntities)
        {
            var guidComponent = (saveable as MonoBehaviour).GetComponent<GuidComponent>();
            if (guidComponent == null) continue;
            
            string guid = guidComponent.GetGuid();
            if (string.IsNullOrEmpty(guid)) continue;
            
            if (data.worldData.TryGetValue(guid, out object savedState))
            {
                saveable.RestoreState(savedState);
            }
        }
    }
}

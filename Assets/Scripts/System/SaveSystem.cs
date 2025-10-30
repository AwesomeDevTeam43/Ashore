using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class SaveSystem
{
    private static readonly string SAVE_FILE = "/player.json";

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

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        string json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);

        File.WriteAllText(Application.persistentDataPath + SAVE_FILE, json);
        Debug.Log("Game Saved to: " + Application.persistentDataPath + SAVE_FILE);
    }

    public static PlayerData LoadPlayer()
    {
        string path = Application.persistentDataPath + SAVE_FILE;
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
        string path = Application.persistentDataPath + SAVE_FILE;
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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EffectConfigCache
{
    private static readonly Dictionary<string, Dictionary<string, ParticleEffectConfigV2>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public static string ObjectsRoot = "";

    public static bool TryGetConfig(string dynamicCodingId, string effectName, out ParticleEffectConfigV2 cfg)
    {
        cfg = null;
        if (string.IsNullOrWhiteSpace(dynamicCodingId) || string.IsNullOrWhiteSpace(effectName)) return false;

        if (!_cache.TryGetValue(dynamicCodingId, out var map))
        {
            map = Load(dynamicCodingId);
            if (map == null) return false;
            _cache[dynamicCodingId] = map;
        }

        return map.TryGetValue(effectName, out cfg);
    }

    private static Dictionary<string, ParticleEffectConfigV2> Load(string id)
    {
        var file = $"{id}_DynamicCoding.json";
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(ObjectsRoot))
            candidates.Add(Path.Combine(ObjectsRoot, id, file));
        else
        {
            var dataParent = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            candidates.Add(Path.Combine(dataParent, "objects", id, file));
            if (!string.IsNullOrEmpty(Application.persistentDataPath))
                candidates.Add(Path.Combine(Application.persistentDataPath, "objects", id, file));
            if (!string.IsNullOrEmpty(Application.streamingAssetsPath))
                candidates.Add(Path.Combine(Application.streamingAssetsPath, "objects", id, file));
        }

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            var json = File.ReadAllText(path);
            var root = JsonUtility.FromJson<DynamicObjectDataV2>(json);
            if (root?.particle_json == null || root.particle_json.Length == 0) return null;

            var map = new Dictionary<string, ParticleEffectConfigV2>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < root.particle_json.Length; i++)
            {
                var cfg = root.particle_json[i];
                var name = !string.IsNullOrWhiteSpace(cfg.effectName) ? cfg.effectName : $"Effect_{i}";
                if (!map.ContainsKey(name)) map.Add(name, cfg);
            }
            return map;
        }

        Debug.LogWarning($"EffectConfigCache: package not found for {id}");
        return null;
    }
}

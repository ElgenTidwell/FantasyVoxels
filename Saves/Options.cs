using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Saves
{
    [System.Serializable]
    public static class Options
    {
        public enum RenderDistance
        {
            Tiny    = 5,
            Near    = 10,
            Normal  = 15,
            Far     = 20,
            Super   = 32,
            Crazy   = 48
        }
        struct GameSettings
        {
            [DefaultValue(RenderDistance.Normal)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public RenderDistance renderDistance;

            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool smoothLightingEnable;
            public GameSettings()
            {
                renderDistance = RenderDistance.Normal;
                smoothLightingEnable = true;
            }
        }

        static string optionsPath = $"{Environment.GetEnvironmentVariable("profilePath")}/gamesettings.json";
        static GameSettings settings;

        public static RenderDistance renderDistance => settings.renderDistance;
        public static bool smoothLightingEnable => settings.smoothLightingEnable;
        public static void SetRenderDistance(RenderDistance distance) { settings.renderDistance = distance; SaveOptions(); }
        public static void SetSmoothLighting(bool on) { settings.smoothLightingEnable = on; SaveOptions(); }

        public static void SaveOptions()
        {
            File.WriteAllText(optionsPath, JsonConvert.SerializeObject(settings));
        }
        public static void LoadOptions()
        {
            if (!File.Exists(optionsPath))
            {
                settings = new GameSettings();
                return;
            }

            settings = JsonConvert.DeserializeObject<GameSettings>(File.ReadAllText(optionsPath));
        }
        static Options()
        {
            LoadOptions();
        }
    }
}

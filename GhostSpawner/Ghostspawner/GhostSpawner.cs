using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using MelonLoader;
using Harmony;
using Managers;
using TwitchAPI;
using Menu;
using Network;
using UnityEngine.SceneManagement;

namespace Ghostspawner
{
    public class GhostSpawner : MelonMod
    {
        public class ModInfo
        {
            public const string GUID = "com.kittyskin.hellpoint.ghostspawner";
            public const string NAME = "Ghost Spawner";
            public const string AUTHOR = "Kitty Skin";
            public const string VERSION = "1.0.0";
            public const string GAME_NAME = "Hellpoint";
            public const string GAME_COMPANY = "Cradle Games";
        }

        public class ModConfig
        {
            public float Randomize_Interval = 300f;
            public Boolean Light_Enabled = false;
            public Boolean Screen_Announce = false;
        }

        private const string CONFIG_PATH = @"Mods\GhostSpawner\config.xml";
        private readonly XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfig));

        public ModConfig config;

        public static bool InMenu => SceneManager.GetActiveScene().name.ToLower().Contains("empty");
        private static bool m_noCharacter = true;
        private float m_timeOfLastRandomize = -1f;
        private string m_currentMessage = "";


        // Update and randomize time

        public override void OnUpdate()
        {
            if (LoadingView.Loading || m_noCharacter || InMenu) return;

            if (Time.time - m_timeOfLastRandomize > config.Randomize_Interval)
            {
                m_timeOfLastRandomize = Time.time;

                if (config.Light_Enabled)
                {
                    TurnOffLight();
                    SpawnGhost();
                    if (config.Screen_Announce)
                    m_currentMessage = "Darkness it's here!";
                }
                else
                {
                    SpawnGhost();
                    if (config.Screen_Announce)
                    m_currentMessage = "Ghost has invaded!";
                }
            }
        }

        private void SpawnGhost()
        {
            TwitchManager.Instance.GetComponentInChildren<VoteGhostOption>().Apply();
        }

        private void TurnOffLight()
        {
            TwitchManager.Instance.GetComponentInChildren<VoteTurnOffLightOption>().Apply();
            TwitchManager.Instance.GetComponentInChildren<VoteTurnOffLightOption>().duration = 1;
        }


        // gui draw message

        public override void OnGUI()
        {
            if (Time.time - m_timeOfLastRandomize < 5f)
            {
                var x = Screen.width / 2 - 300f;
                var y = Screen.height / 2 - 200f;

                GUILayout.BeginArea(new Rect(x, y, 600f, 400f));
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;

                GUILayout.Label("<size=30>" + m_currentMessage + "</size>", null);

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUILayout.EndArea();
            }
        }

        // Patches to track gameplay state

        [HarmonyPatch(typeof(SplashView), nameof(SplashView.OnSaveSelected))]
        public class SplashView_OnSaveSelected
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                m_noCharacter = false;
            }
        }

        [HarmonyPatch(typeof(SplashView), nameof(SplashView.NewGame))]
        public class SplashView_NewGame
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                m_noCharacter = false;
            }
        }

        [HarmonyPatch(typeof(SystemView), nameof(SystemView.SaveAndQuit))]
        public class SystemView_SaveAndQuit
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                m_noCharacter = true;
            }
        }

        [HarmonyPatch(typeof(SystemView), nameof(SystemView.QuitWithoutSaving))]
        public class SystemView_QuitWithoutSaving
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                m_noCharacter = true;
            }
        }

        // ======== Settings ========

        public override void OnApplicationStart()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var folder = Path.GetDirectoryName(CONFIG_PATH);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            else if (File.Exists(CONFIG_PATH))
            {
                var file = File.OpenRead(CONFIG_PATH);
                var obj = xmlSerializer.Deserialize(file);
                file.Close();

                if (obj is ModConfig loadedConfig)
                {
                    config = loadedConfig;
                    return;
                }
            }

            // Only reach this point if valid settings not loaded
            config = new ModConfig
            {
                Randomize_Interval = 300f,
                Light_Enabled = false,
                Screen_Announce = true,
            };

            SaveSettings();
        }

        private void SaveSettings()
        {
            FileStream file = File.Create(CONFIG_PATH);
            xmlSerializer.Serialize(file, config);
            file.Close();
        }
    }
}

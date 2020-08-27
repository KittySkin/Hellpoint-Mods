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
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using Debugs;
using Materials;
using Characters;

namespace EnergyFailure
{
public class EnergyFailureClass : MelonMod
    {
        public class ModInfo
        {
            public const string GUID = "com.kittyskin.hellpoint.EnergyFailure";
            public const string NAME = "Energy Failure";
            public const string AUTHOR = "Kitty Skin";
            public const string VERSION = "1.0.0";
            public const string GAME_NAME = "Hellpoint";
            public const string GAME_COMPANY = "Cradle Games";
        }

        public class ModConfig
        {
            public bool ghost_nightmare = false;
            public bool shake_enabled = true;
        }

        private const string CONFIG_PATH = @"Mods\EnergyFailure\config.xml";
        private readonly XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModConfig));

        public ModConfig config;

        public static bool InMenu => SceneManager.GetActiveScene().name.ToLower().Contains("empty");
        private static bool m_noCharacter = true;
        private float lightTimer = 0f;
        private float TurnOffTimer = 10f;
        private int flickerCount = 0;
        private int maxFlickers = 3;
        private bool energyFailing = false;
        private float shakeDuration = 0;
        private float shakeTime = 0;
        private float shakeWaiter = 0;

        public VoteCommand VoteManager => m_voteManager ?? (m_voteManager = TwitchManager.Instance?.GetComponentInChildren<VoteCommand>());
        private VoteCommand m_voteManager;

        public VoteTurnOffLightOption LightSwitch => m_turnOffLights ?? (m_turnOffLights = VoteManager?.GetComponentInChildren<VoteTurnOffLightOption>());
        private VoteTurnOffLightOption m_turnOffLights;

        private static Player m_cachedPlayer;

        // Update and randomize time

        public override void OnUpdate()
        {
            if (LoadingView.Loading || m_noCharacter || InMenu) return;

            lightTimer += UnityEngine.Time.deltaTime;

            if (lightTimer >= TurnOffTimer)
            {
                energyFailing = true;
                System.Random random = new System.Random();
                int chance = random.Next(1, 101);
                if (chance >= 95)
                {
                    TurnOffLight();
                    flickerCount = 0;
                    lightTimer -= TurnOffTimer;
                    maxFlickers = random.Next(1, 5);
                    TurnOffTimer = random.Next(300, 361);
                    //MelonLogger.Log("next failure in " + TurnOffTimer + " seconds");
                    //MelonLogger.Log("new flicker count " + maxFlickers);
                }
                else
                {
                    LightFlicker();
                    flickerCount++;
                    lightTimer -= TurnOffTimer;
                    TurnOffTimer = 2;
                    //MelonLogger.Log("flickering");
                    if (flickerCount >= maxFlickers)
                    {
                        flickerCount = 0;
                        lightTimer -= TurnOffTimer;
                        maxFlickers = random.Next(1, 5);
                        TurnOffTimer = random.Next(120,151);
                        //MelonLogger.Log("next failure in " + TurnOffTimer + " seconds");
                        //MelonLogger.Log("new flicker count " + maxFlickers);
                    }
                }
            }
            if (energyFailing && config.shake_enabled)
            {
                shakeWaiter += UnityEngine.Time.deltaTime;
                if (shakeWaiter >= 0.05)
                {
                    shakeWaiter = 0;
                    ScreenShaker();
                    //MelonLogger.Log("shaking " + shakeDuration + " times");
                    shakeTime += UnityEngine.Time.deltaTime;
                    if (shakeTime >= shakeDuration)
                    {
                        energyFailing = false;
                        shakeTime = 0;
                        //MelonLogger.Log("reset shaker");
                    }
                }
                
            }
        }

        private void TurnOffLight()
        {
            System.Random random = new System.Random();
            LightSwitch.duration = random.Next(1, 4);
            LightSwitch.Apply();
            int lightSwitchData = LightSwitch.duration;
            //MelonLogger.Log("full shutdown, duration " + lightSwitchData + " minutes");
            shakeDuration = 2;
                if (config.ghost_nightmare && lightSwitchData == 3)
                {
                    UDebug.Execute("spawn");
                    //MelonLogger.Log("spawning ghosts");
                }
        }

        private void LightFlicker()
        {
            LightSwitch.Apply();
            LightSwitch.duration = 0;
            shakeDuration = 0.4f;
        }

        private void ScreenShaker()
        {
            // Determines the direction of the shake, can set 0/1/2/3.
            System.Random random = new System.Random();
            int shakeIndex = random.Next(1, 5);
            Manager<Managers.CameraManager>.Instance.Shake(m_cachedPlayer, shakeIndex);
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

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.CreatePlayer))]
        public class PlayerManager_CreatePlayer
        {
            [HarmonyPostfix]
            public static void Postfix(Player __result)
            {
                m_cachedPlayer = __result;
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
                ghost_nightmare = false,
                shake_enabled = true,
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

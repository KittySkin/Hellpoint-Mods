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

 
        public static bool InMenu => SceneManager.GetActiveScene().name.ToLower().Contains("empty");
        private static bool m_noCharacter = true;
        private float lightTimer = 0f;
        private float TurnOffTimer = 120f;
        private int flickerCount = 0;

        public VoteCommand VoteManager => m_voteManager ?? (m_voteManager = TwitchManager.Instance?.GetComponentInChildren<VoteCommand>());
        private VoteCommand m_voteManager;

        public VoteTurnOffLightOption LightSwitch => m_turnOffLights ?? (m_turnOffLights = VoteManager?.GetComponentInChildren<VoteTurnOffLightOption>());
        private VoteTurnOffLightOption m_turnOffLights;

        // Update and randomize time

        public override void OnUpdate()
        {
            if (LoadingView.Loading || m_noCharacter || InMenu) return;

            lightTimer += UnityEngine.Time.deltaTime;

            if (lightTimer > TurnOffTimer)
            {
                System.Random random = new System.Random();
                int chance = random.Next(1, 101);
                if (chance >= 95)
                {
                    TurnOffLight();
                    lightTimer -= TurnOffTimer;
                    TurnOffTimer = random.Next(300, 361); ;
                    //MelonLogger.Log("turning lights off");
                }
                else
                {
                    LightFlicker();
                    lightTimer -= TurnOffTimer;
                    TurnOffTimer = 2;
                    flickerCount++;
                    //MelonLogger.Log("flickering");
                    if (flickerCount >= 3)
                    {
                        flickerCount = 0;
                        TurnOffTimer = random.Next(90,151);
                        //MelonLogger.Log("reseting count and timer after flickering");
                    }
                }

            }
        }

        private void TurnOffLight()
        {
            LightSwitch.Apply();
            LightSwitch.duration = 2;
        }

        private void LightFlicker()
        {
            LightSwitch.Apply();
            LightSwitch.duration = 0;
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

    }
}

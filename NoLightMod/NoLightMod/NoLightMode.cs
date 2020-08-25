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


namespace NoLightMod
{
    public class NoLightMode : MelonMod
    {
        public class ModInfo
        {
            public const string GUID = "com.kittyskin.hellpoint.NoLightMod";
            public const string NAME = "No Ligh Mode";
            public const string AUTHOR = "Kitty Skin";
            public const string VERSION = "1.0.0";
            public const string GAME_NAME = "Hellpoint";
            public const string GAME_COMPANY = "Cradle Games";
        }

 
        public static bool InMenu => SceneManager.GetActiveScene().name.ToLower().Contains("empty");
        private static bool m_noCharacter = true;
        private float lightTimer = 0f;
        private float TurnOffTimer = 5f;


        public VoteCommand VoteManager => m_voteManager ?? (m_voteManager = TwitchManager.Instance?.GetComponentInChildren<VoteCommand>());
        private VoteCommand m_voteManager;

        // Update and randomize time

        public override void OnUpdate()
        {
            if (LoadingView.Loading || m_noCharacter || InMenu)
            {
                TurnOffTimer = 5f;
                return;
            }

            lightTimer += Time.deltaTime;

            if (lightTimer > TurnOffTimer)
            {
                TurnOffLight();
                lightTimer -= TurnOffTimer;
                TurnOffTimer = 36000f;
            }
        }

        private void TurnOffLight()
        {
            var voteCommand = TwitchManager.Instance.GetComponentInChildren<VoteCommand>();
            voteCommand.GetComponentInChildren<VoteTurnOffLightOption>().Apply();
            voteCommand.GetComponentInChildren<VoteTurnOffLightOption>().duration = 3600;
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

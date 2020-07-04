using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine.SceneManagement;
using HarmonyLib;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;

namespace Quest_Spectator
{

	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin
	{
		internal static Plugin instance { get; private set; }
		internal static Harmony HarmonyInstance { get; private set; }
		internal static string Name => "Quest-Spectator";
		internal static IPALogger _logger { get; private set; }

		[Init]
		/// <summary>
		/// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
		/// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
		/// Only use [Init] with one Constructor.
		/// </summary>
		public void Init(IPALogger logger)
		{
			instance = this;
			_logger = logger;
			Logger.log = logger;
			Logger.log.Debug("Quest-Spectator Logger initialized.");
		}

		#region BSIPA Config
		//Uncomment to use BSIPA's config
		/*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Logger.log.Debug("Config loaded");
        }
        */
		#endregion

		[OnStart]
		public void OnApplicationStart()
		{
			Logger.log.Debug("OnApplicationStart");
			new GameObject("Quest_SpectatorController").AddComponent<Quest_SpectatorController>();
			Harmony.DEBUG = true;
			HarmonyInstance = new Harmony("com.wakamu.questSpectator");
			HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
		}

		[OnExit]
		public void OnApplicationQuit()
		{
			Logger.log.Debug("OnApplicationQuit");
			HarmonyInstance.UnpatchAll("com.wakamu.questSpectator");

		}
	}
}

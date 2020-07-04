using HarmonyLib;
using UnityEngine;

namespace Quest_Spectator.HarmonyPatches
{
	class Spectator
	{
		[HarmonyPatch(typeof(StandardLevelDetailView))]
		[HarmonyPatch("RefreshContent", MethodType.Normal)]
		class Player
		{
			static void Postfix(ref IBeatmapLevel ____level, ref IDifficultyBeatmap ____selectedDifficultyBeatmap)
			{
				Quest_SpectatorController.instance.readyToSpectate = true;
				if (!Quest_SpectatorController.instance.once)
                {
					Quest_SpectatorController.instance.StartClient();
				}
			}
		}

		[HarmonyPatch(typeof(PlayerController))]
		[HarmonyPatch("Update", MethodType.Normal)]
		class PlayerControllerUpdate
		{
			static void Postfix(ref Saber ____leftSaber, ref Saber ____rightSaber, ref Vector3 ____headPos)
			{
				if (Quest_SpectatorController.instance.spectating)
				{
					bool foundCorrectIndex = false;
					Quest_SpectatorController controller = Quest_SpectatorController.instance;
					float songTime = controller.songTime;
					while (!foundCorrectIndex)
					{
						if (controller.indexNum < controller.replaySong.replayLines.Count - 1)
						{
							if (controller.replaySong.replayLines[controller.indexNum].time > songTime)
							{
								foundCorrectIndex = true;
							}
							else if (controller.indexNum < controller.replaySong.replayLines.Count - 1)
							{
								controller.indexNum++;
							}
						}
						else
						{
							foundCorrectIndex = true;
						}
					}
					ReplayLine replayLine = controller.replaySong.replayLines[controller.indexNum];
					____leftSaber.transform.position = replayLine.leftPosition;
					____leftSaber.transform.eulerAngles = replayLine.leftRotation;
					____rightSaber.transform.position = replayLine.rightPosition;
					____rightSaber.transform.eulerAngles = replayLine.rightRotation;
				}
				//____headPos.y = replayLine.headPosition.y;
			}
		}

		[HarmonyPatch(typeof(ScoreUIController))]
		[HarmonyPatch("UpdateScore", MethodType.Normal)]
		class ScoreChanged
		{
			static bool Prefix(ref int rawScore,ref int modifiedScore)
			{
				if(Quest_SpectatorController.instance.spectating) {
					ReplayLine replayLine = Quest_SpectatorController.instance.replaySong.replayLines[Quest_SpectatorController.instance.indexNum];
					rawScore = replayLine.score;
					modifiedScore = (int) (rawScore * Quest_SpectatorController.instance.scoreMultiplier);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(GameEnergyCounter))]
		[HarmonyPatch("AddEnergy", MethodType.Normal)]
		class EnergyBarUpdate
		{
			static bool Prefix(ref int value)
			{
				if (Quest_SpectatorController.instance.spectating)
				{
					value = 0;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(ScoreController))]
		[HarmonyPatch("LateUpdate", MethodType.Normal)]
		class ScoreControllerLateUpdate
		{
			static void Postfix(ref float ____gameplayModifiersScoreMultiplier, ref int ____baseRawScore, ref int ____combo)
			{
				Quest_SpectatorController.instance.scoreMultiplier = ____gameplayModifiersScoreMultiplier;
				if (Quest_SpectatorController.instance.indexNum > 2 && Quest_SpectatorController.instance.spectating)
                {
					ReplayLine replayLine = Quest_SpectatorController.instance.replaySong.replayLines[Quest_SpectatorController.instance.indexNum];
					____baseRawScore = replayLine.score;
					____combo = replayLine.combo;
                }
			}
		}

		//[HarmonyPatch(typeof(ImmediateRankUIPanel))]
		//[HarmonyPatch("RefreshUI", MethodType.Normal)]
		//class RefreshRank
		//{
		//	static void Postfix(ref float _____prevRelativeScore)
		//	{
		//		if (Quest_SpectatorController.instance.spectating)
		//		{
		//			ReplayLine replayLine = Quest_SpectatorController.instance.replaySong.replayLines[Quest_SpectatorController.instance.indexNum];
		//			_____prevRelativeScore = replayLine.score;
		//			____combo = replayLine.combo;
		//		}
		//	}
		//}

		[HarmonyPatch(typeof(AudioTimeSyncController))]
		[HarmonyPatch("Update", MethodType.Normal)]
		class SongUpdate
		{
			static void Postfix(ref float ____songTime,ref float ____timeScale, ref AudioSource ____audioSource)
			{
				Quest_SpectatorController.instance.songTime = ____songTime;
			}
		}

		[HarmonyPatch(typeof(AudioTimeSyncController))]
		[HarmonyPatch("StartSong", MethodType.Normal)]
		class SongAudioStart
		{
			static bool Prefix(ref AudioTimeSyncController.InitData ____initData)
			{
				if (Quest_SpectatorController.instance.spectating)
				{
					____initData = new AudioTimeSyncController.InitData(____initData.audioClip, Quest_SpectatorController.instance.replaySong.replayLines[0].time, ____initData.songTimeOffset, ____initData.timeScale);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(StandardLevelScenesTransitionSetupDataSO))]
		[HarmonyPatch("Finish", MethodType.Normal)]
		class SongEnd
		{
			static bool Prefix()
			{
				Quest_SpectatorController.instance.spectating = false;
				return true;
			}
		}

		[HarmonyPatch(typeof(NoteController))]
		[HarmonyPatch("SendNoteWasMissedEvent", MethodType.Normal)]
		class NoteWasMissed
		{
			static bool Prefix()
			{
				if (Quest_SpectatorController.instance.spectating && Quest_SpectatorController.instance.hasFakeMiss())
                {
					return false;
                }
				return true;
			}
		}

        [HarmonyPatch(typeof(NoteController))]
        [HarmonyPatch("SendNoteWasCutEvent", MethodType.Normal)]
        class NoteWasCut
        {
			static bool Prefix(ref NoteCutInfo noteCutInfo)
			{
				if (Quest_SpectatorController.instance.spectating && !noteCutInfo.allIsOK && Quest_SpectatorController.instance.hasFakeMiss())
				{
					return false;
				}
				return true;
			}
		}
    }
}

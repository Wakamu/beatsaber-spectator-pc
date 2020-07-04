using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;
using BS_Utils.Utilities;

namespace Quest_Spectator
{
    internal static class LevelHelper
    {
        internal static void PlayLevel(SongInfo songInfo)
        {

            try
            {
                LoadBeatmapLevelAsync(songInfo.beatmap, (success, beatmapLevel) =>
                {
                    Logger.log.Log(IPALogger.Level.Info, "Loading Beatmap level Success:" + success);
                    if (success)
                    {
                        StartLevel(beatmapLevel, songInfo);
                    }

                });
            }
            catch (Exception ex)
            {
                Logger.log.Log(IPALogger.Level.Critical, ex);
            }
        }

        private static void StartLevel(IBeatmapLevel beatmapLevel, SongInfo songInfo)
        {
            MenuTransitionsHelper menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().FirstOrDefault();
            PlayerData playerSettings = Resources.FindObjectsOfTypeAll<PlayerDataModel>().First().playerData;
            playerSettings.playerSpecificSettings.leftHanded = songInfo.leftHanded;
            playerSettings.practiceSettings.songSpeedMul = 1.0f;
            songInfo.modifiers.noFail = true;
            IBeatmapLevel level = beatmapLevel;
            PreviewDifficultyBeatmapSet[] sets = songInfo.beatmap.previewDifficultyBeatmapSets;
            BeatmapCharacteristicSO characteristics = null;
            foreach (PreviewDifficultyBeatmapSet set in sets)
            {
                if (("Mode" + set.beatmapCharacteristic.compoundIdPartName) == Convert.ToString(songInfo.mode))
                {
                    characteristics = set.beatmapCharacteristic;
                }
            }
            //BeatmapCharacteristicSO characteristics = songInfo.beatmap.previewDifficultyBeatmapSets[0].beatmapCharacteristic;
            IDifficultyBeatmap levelDifficulty = BeatmapLevelDataExtensions.GetDifficultyBeatmap(level.beatmapLevelData, characteristics, songInfo.difficulty);
            menuSceneSetupData.StartStandardLevel(levelDifficulty,
                playerSettings.overrideEnvironmentSettings.overrideEnvironments ? playerSettings.overrideEnvironmentSettings : null,
                playerSettings.colorSchemesSettings.overrideDefaultColors ? playerSettings.colorSchemesSettings.GetSelectedColorScheme() : null,
                songInfo.modifiers,
                playerSettings.playerSpecificSettings,
                null, "Exit", false, () => { }, (StandardLevelScenesTransitionSetupDataSO sceneTransition, LevelCompletionResults results) =>
                {
                    bool newHighScore = false;

                    var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

                    if (results.levelEndAction == LevelCompletionResults.LevelEndAction.Restart)
                    {
                        return;
                    }

                    switch (results.levelEndStateType)
                    {
                        case LevelCompletionResults.LevelEndStateType.None:
                            break;
                        case LevelCompletionResults.LevelEndStateType.Cleared:
                            Logger.log.Info("Showing menu");
                            break;
                        case LevelCompletionResults.LevelEndStateType.Failed:
                            Logger.log.Info("Showing menu");
                            break;
                        default:
                            break;
                    }
                });
        }

        private static async void LoadBeatmapLevelAsync(IPreviewBeatmapLevel selectedLevel, Action<bool, IBeatmapLevel> callback)
        {
            var token = new CancellationTokenSource();

            var _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();

            var _loadedPreviewBeatmapLevels = _beatmapLevelsModel.GetPrivateField<Dictionary<string, IPreviewBeatmapLevel>>("_loadedPreviewBeatmapLevels");
            bool containsKey = _loadedPreviewBeatmapLevels.ContainsKey(selectedLevel.levelID);

            if (!containsKey)
                _loadedPreviewBeatmapLevels.Add(selectedLevel.levelID, selectedLevel);

            BeatmapLevelsModel.GetBeatmapLevelResult getBeatmapLevelResult = await _beatmapLevelsModel.GetBeatmapLevelAsync(selectedLevel.levelID, token.Token);

            callback?.Invoke(!getBeatmapLevelResult.isError, getBeatmapLevelResult.beatmapLevel);
        }
    }
}

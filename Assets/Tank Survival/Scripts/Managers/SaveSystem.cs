using System.IO;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Статический менеджер сохранения и загрузки прогресса игрока.
    /// Использует JSON-формат для сериализации данных.
    /// </summary>
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "player_progress.json");

        private const string EMPTY_PROGRESS = "{\"totalKills\":0,\"totalGamesPlayed\":0,\"highestDifficultyReached\":0,\"unlockedChassis\":[],\"unlockedTurrets\":[],\"isEasyUnlocked\":true,\"isMediumUnlocked\":false,\"isHardUnlocked\":false,\"currentSessionKills\":0,\"currentLevel\":1,\"currentXp\":0}";

        /// <summary>
        /// Сохранить прогресс игрока в файл
        /// </summary>
        public static void Save(PlayerProgress progress)
        {
            try
            {
                string json = JsonUtility.ToJson(progress, true); // true = pretty print
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Прогресс сохранён: {progress.totalKills} убийств, {progress.totalGamesPlayed} игр");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Ошибка сохранения: {e.Message}");
            }
        }

        /// <summary>
        /// Загрузить прогресс игрока из файла.
        /// Если файл не существует — возвращает новый прогресс (первый запуск).
        /// </summary>
        public static PlayerProgress Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    PlayerProgress progress = JsonUtility.FromJson<PlayerProgress>(json);
                    Debug.Log($"[SaveSystem] Прогресс загружен: {progress.totalKills} убийств, {progress.totalGamesPlayed} игр");
                    return progress;
                }
                else
                {
                    Debug.Log("[SaveSystem] Файл сохранения не найден — создан новый прогресс");
                    return CreateNewProgress();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Ошибка загрузки: {e.Message}. Создан новый прогресс.");
                return CreateNewProgress();
            }
        }

        /// <summary>
        /// Удалить файл сохранения (сброс прогресса)
        /// </summary>
        public static void Delete()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Прогресс удалён");
            }
        }

        /// <summary>
        /// Проверить, существует ли файл сохранения
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Создать новый прогресс (первый запуск игры)
        /// </summary>
        private static PlayerProgress CreateNewProgress()
        {
            PlayerProgress progress = JsonUtility.FromJson<PlayerProgress>(EMPTY_PROGRESS);
            progress.ResetSession();
            return progress;
        }
    }
}

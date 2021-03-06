using MeteorGame.Enemies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MeteorGame
{
    public class GameManager : MonoBehaviour
    {

        public static GameManager Instance { get; private set; }

        #region Variables

        [Header("Difficulty Settings")]
        
        [Tooltip("How many minutes should gamelevel take to reac max")]
        public float minutesToHitMaxGameLevel = 10f;
        [Tooltip("How many levels to reach a level-checkpoint")]
        public float checkpointLevelInterval = 5;
        [Tooltip("Curve of Min-Max level in Min-Max minutes")]
        [SerializeField] private AnimationCurve difficultyCurve;
        [SerializeField] private int maxGameLevel = 100;


        [Header("Misc Settings")]

        [Tooltip("Max aim assist range for projectiles")]
        public float AimAssistRange = 45f;
        [Tooltip("Max links allowed per spell slot")]
        public int MaxLinksAllowed = 7;

        [Header("References")]
        [SerializeField] private TabMenuManager tabMenuManager;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private Player player;
        [SerializeField] private SpellCaster spellCaster;

        public float MaxGameLevel => maxGameLevel;
        public AnimationCurve DifficultyCurve => difficultyCurve;
        public int GameLevel => gameLevel;
        public TabMenuManager TabMenuManager => tabMenuManager;
        public bool IsGamePaused { get; private set; }
        public ScriptableObjectManager ScriptableObjects => scriptableObjects;

        public TimeSpan PlayTime => gameplayTimeSW.Elapsed;

        public Action<int> OnDifficultyChanged;

        public event Action GameOver;
        public event Action GameRestart;

        private float debugElapsedSeconds;
        private float debugGameLevel = 0;
        private int gameLevel = 1; // derived from minutes since start and difficultyCurve
        private ScriptableObjectManager scriptableObjects = new();
        private Stopwatch gameplayTimeSW = Stopwatch.StartNew();

        #endregion

        #region Unity Methods

        private void Awake()
        {
            Instance = this;
            SetUnitySettings();
            SetupManagers();
        }

        private void Start()
        {
            StartCoroutine(StartGameWithDelay(1f));
        }


        private IEnumerator StartGameWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartGame();
        }

        private void Update()
        {

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ShowHideTabMenu();
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                debugGameLevel += 5;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                EnemyManager.Instance.SpawnPack();
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                EnemyManager.Instance.DestroyAllEnemies();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                Player.Instance.AddCurrency(999999999);
            }

            var secondsToMax = minutesToHitMaxGameLevel * 60;
            var val = HowFarIntoDifficulty() / secondsToMax;
            var rounded = Math.Round(val, 6);
            var secondsPercentage = (float)rounded;
            var eval = difficultyCurve.Evaluate(secondsPercentage);
            var res = Mathf.Floor(eval * maxGameLevel) + 1;

            gameLevel = (int)res + (int)debugGameLevel;

            if (gameLevel > maxGameLevel)
            {
                gameLevel = maxGameLevel;
            }

            OnDifficultyChanged?.Invoke(gameLevel);
        }



        #endregion

        #region Methods


        private void SetUnitySettings()
        {
            SetCursorMode(CursorLockMode.Locked);
            QualitySettings.vSyncCount = 1;
        }

        private void SetupManagers()
        {
            scriptableObjects.Load();
            tabMenuManager.Setup();

            enemyManager.Setup();
            player.Setup();
            spellCaster.Setup();
        }

        public void SetCursorMode(CursorLockMode newMode)
        {
            Cursor.lockState = newMode;
        }


        internal void InitGameOver(Enemy e)
        {
            PauseGame();
            GameOver?.Invoke();
        }

        public float HowFarIntoDifficulty()
        {
            return (int)gameplayTimeSW.Elapsed.TotalSeconds + debugElapsedSeconds;
        }


        public void PauseGame()
        {
            Time.timeScale = 0;
            IsGamePaused = true;
            gameplayTimeSW.Stop();
        }

        public void UnPauseGame()
        {
            Time.timeScale = 1;
            IsGamePaused = false;
            gameplayTimeSW.Start();
        }

        private void ShowHideTabMenu()
        {
            if (tabMenuManager.IsShowing)
            {
                tabMenuManager.Hide();
                UnPauseGame();
            }
            else
            {
                tabMenuManager.Show();
                PauseGame();
            }
        }

        private void StartGame()
        {
            gameplayTimeSW.Restart();
            debugGameLevel = 0;
            EnemyManager.Instance.DestroyAllEnemies();
            UnPauseGame();
            GameRestart?.Invoke();
        }


        public void OnRetryClicked()
        {
            StartGame();
        }


        #endregion

    }
}

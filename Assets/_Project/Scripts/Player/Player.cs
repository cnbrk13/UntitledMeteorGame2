using DG.Tweening;
using MeteorGame.Enemies;
using MeteorGame.Flight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeteorGame
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        #region Variables
        
        [SerializeField] private Inventory inventory;

        public Inventory Inv => inventory;

        public float Speed => flightController != null ? flightController.Speed : 0;

        public float TweeningCurrency => currencyTweening;

        public Action<int> OnCurrencyChanged;

        private float currencyTweening = 0; // used to tween and display currency

        [SerializeField] private float currency = 0;
        [SerializeField] private SpellSlot spellSlot1;
        [SerializeField] private SpellSlot spellSlot2;

        private Tween currencyTween;
        private Camera cameraObj;

        private bool isSetup = false;

        private float pitch = 1f;
        private Coroutine pitchResetCoroutine;
        private float pitchResetDurInSeconds = 1.5f;

        private Vector3 startingPos;
        private Quaternion startingRotation;

        private PhysicsFlight2 flightController;

        public event Action Ready;

        public BoostManager BoostManager { get; private set; }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            //QualitySettings.vSyncCount = 1;
            //Application.targetFrameRate = 144;

            Instance = this;
            startingPos = transform.position;
            startingRotation = transform.rotation;

            flightController = GetComponent<PhysicsFlight2>();
            BoostManager = GetComponent<BoostManager>();
        }

        private void Start()
        {
            GameManager.Instance.GameRestart += OnGameRestart;
        }

        #endregion

        #region Methods


        private void OnGameRestart()
        {
            var dist = Vector3.Distance(transform.position, startingPos);
            var dur = MathF.Min(dist / 50f, 2f);

            transform.DOMove(startingPos, dur);
            transform.DORotate(startingRotation.eulerAngles, dur).OnComplete(() => Ready?.Invoke());


            ResetCurrency();
        }


        private void ResetCurrency()
        {
            ResetCurrencyTween();

            currency = 0;
            currencyTweening = 0;
        }

        private void ResetCurrencyTween()
        {
            if (currencyTween != null && currencyTween.active)
            {
                currencyTween.Complete();
                currencyTween.Kill();
            }
        }

        public SpellSlot SpellSlot(int i)
        {
            if (i == 1)
            {
                return spellSlot1;
            }

            if (i == 2)
            {
                return spellSlot2;
            }

            return null;
        }


        public void Setup()
        {
            inventory = new Inventory();
            cameraObj = GetComponentInChildren<Camera>();

            spellSlot1 = new SpellSlot(1);
            spellSlot2 = new SpellSlot(2);

            spellSlot1.GemLinkedOrRemoved += OnGemAddedOrRemoved;
            spellSlot1.SpellChanged += OnSpellChanged;

            spellSlot2.GemLinkedOrRemoved += OnGemAddedOrRemoved;
            spellSlot2.SpellChanged += OnSpellChanged;

            //GameManager.Instance.TabMenuManager.RebuildTabMenu();

            currencyTweening = currency;

            DebugAddStuff();

            isSetup = true;
        }


        public void DebugAddStuff()
        {
            DebugAddAllGemsToInv();
            spellSlot1.UnlockSpellSlot();
            spellSlot1.IncreaseMaxLinks();
            spellSlot1.Equip(inventory.Spells.First(s => s.Name == "Fireball").Gem);
        }


        private void DebugAddAllGemsToInv()
        {
            foreach (GemSO gemSO in GameManager.Instance.ScriptableObjects.Gems)
            {
                GemItem gem = new(gemSO, level: 0);
                inventory.AddGem(gem);
            }
        }

        private void OnSpellChanged(SpellSlot slot, SpellItem spell)
        {
            if (isSetup)
            {
                //GameManager.Instance.TabMenuManager.RebuildTabMenu();
            }
        }

        private void OnGemAddedOrRemoved(SpellSlot slot, GemItem gem)
        {
            if (isSetup)
            {
                //GameManager.Instance.TabMenuManager.RebuildTabMenu();
            }
        }

        IEnumerator PitchResetCoroutine()
        {
            yield return new WaitForSeconds(pitchResetDurInSeconds);
            pitch = 1f;
        }

        internal void CollidedWithDroppedGold(GoldCoinDrop goldCoinDrop)
        {
            AddCurrency(goldCoinDrop.goldAmount);
            goldCoinDrop.PlayAuidoWithPitch(pitch);
            pitch += 0.05f;

            if (pitchResetCoroutine != null)
            {
                StopCoroutine(pitchResetCoroutine);
            }

            pitchResetCoroutine = StartCoroutine(PitchResetCoroutine());
        }


        /// <summary>
        /// Casts a ray in camera direction and returns where it hits the world,
        /// or infinity
        /// </summary>
        /// <param name="hitEnemy"></param>
        /// <returns></returns>
        public Vector3 AimingAt(out Enemy hitEnemy)
        {
            hitEnemy = null;
            Vector3 destination;

            int layer_mask = LayerMask.GetMask(new string[]{"Enemies", "Arena"});

            Ray ray = cameraObj.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, 900f, layer_mask))
            {
                if (hit.transform.gameObject.TryGetComponent<Enemy>(out var e))
                {
                    hitEnemy = e;
                }

                destination = hit.point;
            }
            else
            {
                destination = ray.GetPoint(1000);
            }

            return destination;
        }


        internal void AddCurrency(int amount)
        {
            ResetCurrencyTween();

            var curr = currency;
            var target = curr + amount;
            currency = target;
            currencyTween = DOTween.To(() => currencyTweening, x => currencyTweening = x, target, 0.5f).SetUpdate(true)
                .OnUpdate(() => OnCurrencyChanged?.Invoke((int)currencyTweening))
                .OnComplete(() => OnCurrencyChanged?.Invoke((int)target));
        }

        public bool CanAfford(int amount)
        {
            return currency >= amount;
        }


        #endregion
    }
}

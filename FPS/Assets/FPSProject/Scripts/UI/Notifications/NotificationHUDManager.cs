using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using Unity.FPS.AI;
using UnityEngine;
using Unity.FPS.FPSController;

namespace Unity.FPS.UI
{
    public class NotificationHUDManager : MonoBehaviour
    {
        [Tooltip("UI panel containing the layoutGroup for displaying notifications")]
        public RectTransform NotificationPanel;

        [Tooltip("Prefab for the notifications")]
        public GameObject NotificationPrefab;

        void Awake()
        {
            EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
            WeaponSpace playerWeaponsManager = FindObjectOfType<WeaponSpace>();
            DetectionModule detectionModule = FindObjectOfType<DetectionModule>();
            DebugUtility.HandleErrorIfNullFindObject<WeaponSpace, NotificationHUDManager>(playerWeaponsManager, this);

            enemyManager.OnDyingAlly += OnAllyDeath;
            playerWeaponsManager.OnAddedWeapon += OnCollectingWeapon;
            playerWeaponsManager.transform.parent.GetComponent<GrenadeThrower>().OnAddedGrenade += OnCollectingGrenade;
            detectionModule.onDetectedTarget += OnDetection;
            detectionModule.onLostTarget += OnLosingTarget;

            EventManager.AddListener<ObjectiveUpdateEvent>(OnObjectiveUpdateEvent);
        }

        void OnObjectiveUpdateEvent(ObjectiveUpdateEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.NotificationText))
                CreateNotification(evt.NotificationText);
        }

        void OnCollectingWeapon(WeaponCollection weaponController, int index)
        {
            if (index != 0)
            {
                CreateNotification("Collected weapon : " + weaponController.weapons[index].name);
            }
        }

        void OnCollectingGrenade(GrenadeThrower grenade)
        {
            CreateNotification("Collected grenade : Hand Grenade (Grenade)");
        }

        void OnDetection()
        {
            CreateNotification("Enemies spotted you");
        }

        void OnLosingTarget()
        {
            CreateNotification("Enemies lost you");
        }

        void OnAllyDeath()
        {
            CreateNotification("Ally died");
        }

        public void CreateNotification(string text)
        {
            GameObject notificationInstance = Instantiate(NotificationPrefab, NotificationPanel);
            notificationInstance.transform.SetSiblingIndex(0);

            NotificationToast toast = notificationInstance.GetComponent<NotificationToast>();
            if (toast)
            {
                toast.Initialize(text);
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<ObjectiveUpdateEvent>(OnObjectiveUpdateEvent);
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using cakeslice;

namespace Unity.FPS.FPSController
{
    public class CollectableObject : MonoBehaviour
    {
        public enum CollectionType { Weapon, Ammo, Grenade }

        [SerializeField] public CollectionType m_CollectionType = CollectionType.Weapon;

        [SerializeField] public Weapon m_Weapon;
        [SerializeField] public Ammo m_Ammo;
        [SerializeField] public Grenade m_Grenade;

        [SerializeField] public bool m_Enable = true;
        [SerializeField] public int m_AmmoInWeapon;
        [SerializeField] public int m_AddToAmmoTotal;

        [SerializeField] public GameObject m_AfterCollectionObject;
        [SerializeField] public float m_AfterCollectionDespawnTime;

        [Tooltip("Frequency at which the item will move up and down")]
        public float VerticalBobFrequency = 1f;

        [Tooltip("Distance the item will move up and down")]
        public float BobbingAmount = 1f;

        [Tooltip("Rotation angle per second")] public float RotatingSpeed = 360f;

        Vector3 m_StartPosition;

        WeaponSpace m_CurrentWeaponSpace;

        GameObject m_PlayerCamera;

        OutlineAnimation m_OutlineAnimation;

        public GrenadeThrower grenade;

        PickupGrenadeEvent evtGrenade;

        PickupWeaponEvent evtWeapon;

        bool m_HasPlayedFeedback;

        protected virtual void Start()
        {
            m_PlayerCamera = GameObject.FindGameObjectsWithTag("MainCamera")[0];

            m_OutlineAnimation = m_PlayerCamera.GetComponent<OutlineAnimation>();


            // Remember start position for animation
            m_StartPosition = transform.position;
        }


        private void Update()
        {
            // Handle bobbing
            float bobbingAnimationPhase = ((Mathf.Sin(Time.time * VerticalBobFrequency) * 0.5f) + 0.5f) * BobbingAmount;
            transform.position = m_StartPosition + Vector3.up * bobbingAnimationPhase;

            // Handle rotating
            transform.Rotate(Vector3.up, RotatingSpeed * Time.deltaTime, Space.Self);
        }

        public void WeaponPickup(FPSPlayerController other)
        {
            m_CurrentWeaponSpace = other.GetComponentInChildren<WeaponSpace>();

            if (m_CurrentWeaponSpace != null)
            {
                if (!m_CurrentWeaponSpace.isEnabled(m_Weapon))
                {
                    if (m_CollectionType == CollectionType.Weapon)
                    {
                        m_CurrentWeaponSpace.EnableWeapon(m_Weapon, m_Enable, m_AmmoInWeapon);
                        m_CurrentWeaponSpace.IncreaseAmmoCount(m_Weapon.ammo, m_AddToAmmoTotal);
                    }
                    else if (m_CollectionType == CollectionType.Ammo)
                    {
                        m_CurrentWeaponSpace.IncreaseAmmoCount(m_Ammo, m_AddToAmmoTotal);
                    }

                    if (m_AfterCollectionObject != null)
                    {
                        GameObject afterObjectInstance = Instantiate(m_AfterCollectionObject, transform.position, transform.rotation);
                        Destroy(afterObjectInstance, m_AfterCollectionDespawnTime);
                    }

                    m_Enable = false;
                    Destroy(gameObject);
                    PlayPickupFeedback();
                    evtWeapon = Events.PickupWeaponEvent;
                    evtWeapon.PickupWeapon = gameObject;
                    EventManager.Broadcast(evtWeapon);
                    OnMouseExit();
                }
            }
        }

        public void GrenadePickup(FPSPlayerController other)
        {
            m_CurrentWeaponSpace = other.GetComponentInChildren<WeaponSpace>();

            if (m_CurrentWeaponSpace != null)
            {
                if (m_CurrentWeaponSpace.IsGrenadeCollectable())
                {
                    if (m_CollectionType == CollectionType.Grenade)
                    {
                        grenade = m_CurrentWeaponSpace.transform.GetComponentInParent<GrenadeThrower>();
                        grenade.EnableGrenade();
                        if (m_AfterCollectionObject != null)
                        {
                            GameObject afterObjectInstance = Instantiate(m_AfterCollectionObject, transform.position, transform.rotation);
                            Destroy(afterObjectInstance, m_AfterCollectionDespawnTime);
                        }

                        Destroy(gameObject);
                        PlayPickupFeedback();
                        evtGrenade = Events.PickupGrenadeEvent;
                        evtGrenade.PickupGrenade = gameObject;
                        EventManager.Broadcast(evtGrenade);
                        OnMouseExit();
                    }
                }
            }
        }

        public void PlayPickupFeedback()
        {
            if (m_HasPlayedFeedback)
                return;

            m_HasPlayedFeedback = true;
        }

        void OnMouseOver()
        {
            m_OutlineAnimation.HoverHandler(true);
        }

        void OnMouseExit()
        {
            m_OutlineAnimation.HoverHandler(false);
        }
    }
}
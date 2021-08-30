using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.UI;

namespace Unity.FPS.FPSController
{
    static class GrenadeThrowerTooltip
    {
        public const string
            m_IsInfiniteGrenade = "Enables the infinte grenade mode unless it was disabled in the settings before starting the game.",
            startAmount = "Total amountof grenades that avaliable at start.",
            grenadeIcon = "Icon used in UI to resemble the grenades.";
    }

    public class GrenadeThrower : MonoBehaviour
    {
        [SerializeField] public GameObject grenadePrefab;
        [SerializeField] public AudioClip m_ThrowAudio;
        [SerializeField] [Tooltip(GrenadeThrowerTooltip.startAmount)] public int startAmount = 3;
        [SerializeField] [Tooltip(GrenadeThrowerTooltip.grenadeIcon)] public Sprite grenadeIcon;
        [SerializeField] KeyCode grenadeThrowKey;
        [SerializeField] public bool m_CanThrow = false;
        [SerializeField] public float throwForce = 40f;
        [SerializeField] public float cooldown = 1f;

        public AudioSource m_AudioSource;
        public UnityAction<GrenadeThrower> OnAddedGrenade;
        public bool m_IsInfiniteGrenade;
        public int totalAmount;
        public float m_Cooldown = 0f;

        // Start is called before the first frame update
        void Start()
        {
            if (GameObject.FindGameObjectsWithTag("Settings").Length != 0)
            {
                m_IsInfiniteGrenade = GameObject.FindWithTag("Settings").GetComponent<Settings>().m_IsInfiniteGrenade;
            }
            else
            {
                m_IsInfiniteGrenade = true;
            }

            m_AudioSource = GetComponent<AudioSource>();

            if (m_CanThrow)
            {
                if (!m_IsInfiniteGrenade)
                {
                    totalAmount = startAmount;
                }
            }
            else
            {
                totalAmount = 0;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (m_CanThrow)
            {
                if (m_Cooldown > 0f)
                {
                    m_Cooldown -= 1f * Time.deltaTime;
                }
                else if (Input.GetKey(grenadeThrowKey))
                {
                    if (!m_IsInfiniteGrenade && totalAmount <= 0)
                    {

                    }
                    else
                    {
                        if (!m_IsInfiniteGrenade && totalAmount > 0)
                        {
                            totalAmount--;
                        }

                        ThrowGrenade();
                        m_Cooldown = cooldown;
                    }
                }
            }
        }

        // Throwing a grenade is only enabled after collecting a grenade or if it is already enabled by settings
        public void EnableGrenade()
        {
            m_CanThrow = true;
            if (!m_IsInfiniteGrenade)
            {
                totalAmount += startAmount;

                if (OnAddedGrenade != null)
                {
                    OnAddedGrenade.Invoke(this);
                }
            }
        }

        void ThrowGrenade()
        {
            if (m_ThrowAudio != null)
            {
                m_AudioSource.clip = m_ThrowAudio;
                m_AudioSource.Play();
            }

            GameObject grenade = Instantiate(grenadePrefab, transform.position, transform.rotation);
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * throwForce, ForceMode.VelocityChange);
        }
    }
}
using UnityEngine;
using Unity.FPS.Game;
using System.Collections.Generic;

namespace Unity.FPS.FPSController
{
    static class GrenadeTooltip
    {
        public const string
            DamageRatioOverDistance = "Damage multiplier over distance for area of effect";
    }

    public class Grenade : MonoBehaviour
    {
        [SerializeField] GameObject m_explosionEffect;
        [SerializeField] AudioClip m_CollideAudio;
        [Space]
        [SerializeField] bool m_IsDetonateOnCollision = false;
        [SerializeField] float m_DetonationTime = 4f;
        [SerializeField] float explosionForce = 200f;
        [SerializeField] float explosionDamage = 60f;
        [SerializeField] float explosionRadius = 5f;
        [SerializeField] float m_ExplosionExistenceTime = 4f;
        [SerializeField] [Tooltip(GrenadeTooltip.DamageRatioOverDistance)] AnimationCurve DamageRatioOverDistance;

        AudioSource m_AudioSource;
        Damageable nearbyDamageable;
        Dictionary<Health, Damageable> uniqueDamagedHealths;
        float m_CurrentDetonationTime;

        void Start()
        {
            m_AudioSource = GetComponent<AudioSource>();
            m_CurrentDetonationTime = m_DetonationTime;
        }

        void Update()
        {
            if (m_CurrentDetonationTime > 0f)
            {
                m_CurrentDetonationTime -= 1f * Time.deltaTime;
            }
            else
            {
                SpawnExplosion();
            }
        }

        void OnCollisionEnter(Collision col)
        {
            if (m_IsDetonateOnCollision)
            {
                SpawnExplosion();
            }
            else
            {
                m_AudioSource.clip = m_CollideAudio;
                m_AudioSource.Play();
            }
        }

        public void SpawnExplosion()
        {
            GameObject explosionInstance = Instantiate(m_explosionEffect, transform.position, transform.rotation);
            uniqueDamagedHealths = new Dictionary<Health, Damageable>();
            Collider[] collidersToDestroy = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider nearbyObject in collidersToDestroy)
            {
                nearbyDamageable = nearbyObject.GetComponent<Damageable>();

                if (nearbyDamageable)
                {
                    Health health = nearbyDamageable.GetComponentInParent<Health>();

                    if (health && !uniqueDamagedHealths.ContainsKey(health))
                    {
                        uniqueDamagedHealths.Add(health, nearbyDamageable);
                    }
                }

                // Apply damages with distance falloff
                foreach (Damageable uniqueDamageable in uniqueDamagedHealths.Values)
                {
                    float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
                    uniqueDamageable.InflictDamage(explosionDamage * DamageRatioOverDistance.Evaluate(distance / explosionRadius), true);
                }

                //Destructible dest = nearbyObject.GetComponent<Destructible>();
                //if(dest != null)
                //{
                //    dest.Destory();
                //}
            }

            Collider[] collidersToMove = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider nearbyObject in collidersToMove)
            {
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }

            Destroy(gameObject);
            Destroy(explosionInstance, m_ExplosionExistenceTime);
        }

    }
}
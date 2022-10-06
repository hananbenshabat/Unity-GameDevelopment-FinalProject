using System.Collections.Generic;
using Unity.FPS.FPSController;
using UnityEngine;

namespace Unity.FPS.AI
{
    public class EnemyCollection : MonoBehaviour
    {
        EnemyController m_EnemyController;

        void OnTriggerEnter(Collider other)
        {
            m_EnemyController = transform.parent.GetComponent<EnemyController>();
            if (other.transform.gameObject.layer == 11 && other.tag == "Weapon")
            {
                m_EnemyController.EnemyCollectWeapon(other, other.transform.GetComponent<CollectableObject>());
            }
            else if (other.tag == "Grenade")
            {
                m_EnemyController.EnemyCollectGrenade(other, other.transform.GetComponent<CollectableObject>());
            }
        }
    }
}
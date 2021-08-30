using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.FPSController;

public class SpawnWeaponsRandomly : MonoBehaviour
{
    [SerializeField] WeaponCollection m_WeaponCollection;
    [SerializeField] GameObject weaponPrefab1, weaponPrefab2, weaponPrefab3, weaponPrefab4, weaponPrefab5;
    [SerializeField] GameObject grenadePrefab;
    [SerializeField] bool isGrenadeEnabled;

    GameObject[] m_SpawnObjectives;
    GameObject m_spawnWeapon;
    List<int> m_EnabledOnStartWeaponsIndexes;
    Transform m_spawnWeaponPosition;
    int m_WeaponsLength, randomIndex, i;

    private void Awake()
    {
        m_WeaponsLength = m_WeaponCollection.weapons.Count;
        m_EnabledOnStartWeaponsIndexes = new List<int>();


        for (i = 0; i < m_WeaponsLength; i++)
        {
            if (m_WeaponCollection.weapons[i].enableOnStart)
            {
                m_EnabledOnStartWeaponsIndexes.Add(i);
            }
        }

        if(isGrenadeEnabled)
        {
            m_WeaponsLength++;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SpawnWeapons();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnWeapons()
    {
        if (m_EnabledOnStartWeaponsIndexes.Count < m_WeaponsLength)
        {
            m_SpawnObjectives = GameObject.FindGameObjectsWithTag("WeaponSpawn");

            for (i = 0; i < m_SpawnObjectives.Length; i++)
            {
                randomIndex = Random.Range(0, m_WeaponsLength);

                while (m_EnabledOnStartWeaponsIndexes.Contains(randomIndex))
                {
                    randomIndex = Random.Range(0, m_WeaponsLength);
                }

                switch (randomIndex)
                {
                    case 0:
                        m_spawnWeapon = weaponPrefab1;
                        break;
                    case 1:
                        m_spawnWeapon = weaponPrefab2;
                        break;
                    case 2:
                        m_spawnWeapon = weaponPrefab3;
                        break;
                    case 3:
                        m_spawnWeapon = weaponPrefab4;
                        break;
                    case 4:
                        m_spawnWeapon = weaponPrefab5;
                        break;
                    case 5:
                        m_spawnWeapon = grenadePrefab;
                        break;
                }

                m_spawnWeaponPosition = m_SpawnObjectives[i].transform;
                m_spawnWeaponPosition.position = new Vector3(m_spawnWeaponPosition.position.x + 0.7f, m_spawnWeaponPosition.position.y + 1, m_spawnWeaponPosition.position.z);

                Instantiate(m_spawnWeapon, m_spawnWeaponPosition);
            }
        }
    }
}

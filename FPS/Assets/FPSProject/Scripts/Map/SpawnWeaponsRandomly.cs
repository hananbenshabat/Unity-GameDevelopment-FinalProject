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

    GameObject m_SpawnWeapon, m_RandomWeapon;
    List<int> m_EnabledOnStartWeaponsIndexes;
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
        WeaponSpawn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    GameObject RandomWeapon()
    {
        randomIndex = Random.Range(0, m_WeaponsLength);

        while (m_EnabledOnStartWeaponsIndexes.Contains(randomIndex))
        {
            randomIndex = Random.Range(0, m_WeaponsLength);
        }

        switch (randomIndex)
        {
            case 0:
                return weaponPrefab1;
            case 1:
                return weaponPrefab2;
            case 2:
                return weaponPrefab3;
            case 3:
                return weaponPrefab4;
            case 4:
                return weaponPrefab5;
            case 5:
                return grenadePrefab;
        }

        return null;
    }

    void WeaponSpawn()
    {
        if (m_EnabledOnStartWeaponsIndexes.Count < m_WeaponsLength)
        {
            m_RandomWeapon = RandomWeapon();
            if (m_RandomWeapon != null)
            {
                m_SpawnWeapon = Instantiate(m_RandomWeapon) as GameObject;

                m_SpawnWeapon.name = m_RandomWeapon.name;

                m_SpawnWeapon.transform.position = new Vector3(transform.position.x + 0.7f, transform.position.y + 1, transform.position.z);

                Instantiate(m_SpawnWeapon, m_SpawnWeapon.transform);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Unity.FPS.Game;
using Unity.FPS.UI;

namespace Unity.FPS.FPSController
{
    static class WeaponSpaceTooltip
    {
        public const string
            m_inputFire = "Key or button used to fire weapon.",
            m_inputAutoFire = "Key or button that is held down for automatic firing. Often the same as Input Auto Fire.",
            m_inputReload = "Key or button used to reload weapon.",
            m_inputSwitch = "Key or button used to switch weapon.",
            m_inputAim = "Key or button used to aim weapon.",
            m_inputRun = "Key or button that is held down to run.",

            m_MouseXInfluenceName = "Name of axis specified in Input Manager for left and right mouse movements.",
            m_MouseYInfluenceName = "Name of axis specified in Input Manager for up and down mouse movements.",

            m_WeaponCollection = "WeaponCollection to use on this character.",

            m_CameraRaySpawn = "Camera used to project firing ray via its Z axis.",
            m_FPSPlayer = "FPS player character in the scene.",

            m_UICrosshairSpace = "UI image GameObject used to display crosshair sprites.",
            m_UIMagAmmoCount = "UI text GameObject used to display weapon ammo count.",
            m_UITotalAmmoCount = "UI text GameObject used to display total ammo count.",
            m_UIAmmoIconSpace = "UI image GameObject used to display ammo icon sprites.",
            m_UITotalGrenadeCount = "UI text GameObject used to display total grenade count.",
            m_UIGrenadeIconSpace = "UI image GameObject used to display grenade icon sprites.";
    }

    public class WeaponSpace : MonoBehaviour
    {
        [SerializeField] CharacterController m_CharacterController;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_WeaponCollection)] WeaponCollection m_WeaponCollection;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_CameraRaySpawn)] Transform m_CameraRaySpawn;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_FPSPlayer)] Transform m_FPSPlayer;

        [Header("Input Keys")]
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_inputFire)] KeyCode m_inputFire;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_inputAutoFire)] KeyCode m_inputAutoFire;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_inputReload)] KeyCode m_inputReload;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_inputSwitch)] KeyCode m_inputSwitch;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_inputAim)] KeyCode m_inputAim;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_inputRun)] KeyCode m_inputRun;

        [Header("Mouse Influence Axes")]
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_MouseXInfluenceName)] string m_MouseXInfluenceName;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_MouseYInfluenceName)] string m_MouseYInfluenceName;

        [Header("UI Objects")]
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_UICrosshairSpace)] Image m_UICrosshairSpace;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_UIMagAmmoCount)] Text m_UIMagAmmoCount;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_UITotalAmmoCount)] Text m_UITotalAmmoCount;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_UIAmmoIconSpace)] Image m_UIAmmoIconSpace;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_UITotalGrenadeCount)] Text m_UITotalGrenadeCount;
        [SerializeField] [Tooltip(WeaponSpaceTooltip.m_UIGrenadeIconSpace)] Image m_UIGrenadeIconSpace;

        [Header("Layer Names")]
        [SerializeField] string m_FirstPersonLayerName = "FirstPerson";
        [SerializeField] string m_ThirdPersonLayerName = "ThirdPerson";
        [SerializeField] string m_ProjectileLayerName = "Projectile";

        [Tooltip("Layers this projectile can collide with")]
        public LayerMask HittableLayers = -1;

        Transform m_ProjectileSpawn, m_BarrelFlashSpawn, m_EjectedCartridgeSpawn;

        GrenadeThrower m_GrenadeThrower;

        Animator m_Animator;

        // Updated during runtime, thus should not belong in Weapon scriptable object (for initialisation only)
        bool[] m_IsWeaponEnabled;
        int[] m_CurrentCapacity;
        float[] m_TimeUntillNextRound;

        int m_SelectedWeapon, m_LastSelectedWeapon, m_BurstRoundsCount, m_WeaponsLength;

        Color tempOpacityColor;

        Dictionary<string, int> m_AmmoAmounts = new Dictionary<string, int>();


        // Ad-hoc transition timings to ensure certain actions are only avaliable after a defined duration
        // Useful for syncing mechanics with animations
        float m_CurrentAimingTime, m_CurrentRunningRecoveryTime, m_CurrentPReloadInterruptionTime;

        bool m_IsInfiniteAmmo, m_IsReloading, m_IsSwitching, m_IsCurrentWeaponDisabled, m_IsJumping, m_IsRunning, m_IsWalking, m_IsInputFire,
            m_IsInputAutoFire, m_IsInputReload, m_IsInputSwitch, m_IsInputAim, m_IsInputRun, m_IsWeaponLoadedAtStart = false;

        Coroutine m_ReloadWeaponCoroutine;

        GameObject m_InstantiatedWeaponObject, grenadeInstance;
        Rigidbody rb;

        Damageable damageable;
        Dictionary<Health, Damageable> uniqueDamagedHealths;

        Vector2 m_CurrentMouseInfluance;

        public UnityAction<WeaponCollection, int> OnAddedWeapon;

        void Awake()
        {
            if (GameObject.FindGameObjectsWithTag("Settings").Length != 0)
            {
                m_IsInfiniteAmmo = GameObject.FindWithTag("Settings").GetComponent<Settings>().m_IsInfiniteAmmo;
            }
            else
            {
                m_IsInfiniteAmmo = true;
            }

            m_Animator = GetComponent<Animator>();
            m_GrenadeThrower = transform.parent.GetComponent<GrenadeThrower>();

            m_WeaponsLength = m_WeaponCollection.weapons.Count;

            m_IsWeaponEnabled = new bool[m_WeaponsLength];
            m_CurrentCapacity = new int[m_WeaponsLength];
            m_TimeUntillNextRound = new float[m_WeaponsLength];


            for (int i = 0; i < m_WeaponsLength; i++)
            {
                if (!m_AmmoAmounts.ContainsKey(m_WeaponCollection.weapons[i].ammo.name))
                {
                    m_AmmoAmounts.Add(m_WeaponCollection.weapons[i].ammo.name, m_WeaponCollection.weapons[i].ammo.startAmount);
                }
            }

            for (int j = 0; j < m_WeaponsLength; j++)
            {
                if (m_WeaponCollection.weapons[j].enableOnStart)
                {
                    if (m_WeaponCollection.weapons[j].loadedAtStart)
                    {
                        EnableWeapon(j, true, m_WeaponCollection.weapons[j].capacity);
                    }
                    else
                    {
                        EnableWeapon(j, true, 0);
                    }
                }
            }
        }

        void Start()
        {
            for (int i = 0; i < m_WeaponsLength; i++)
            {
                if (m_WeaponCollection.weapons[i].enableOnStart)
                {
                    m_SelectedWeapon = i;
                    i = m_WeaponsLength;
                }
            }

            // Removes all child object of FPSWeaponSystems
            for (int j = 0; j < transform.childCount; j++)
            {
                Destroy(transform.GetChild(j).gameObject);
            }
        }

        void Update()
        {
            CheckInputs();

            // Prepares default weapon on start
            if (!m_IsWeaponLoadedAtStart)
            {
                LoadSelectedWeapon();
                m_IsWeaponLoadedAtStart = true;
            }

            CheckCharacterMovement();
            CheckMouseMovement();

            CheckWeaponFire();
            CheckWeaponAim();
            CheckWeaponSwitch();
            CheckWeaponReload();
            UpdateHUD();
        }

        void CheckWeaponFire()
        {
            // Decrement time untill next round for all weapons
            for (int i = 0; i < m_TimeUntillNextRound.Length; i++)
            {
                if (m_TimeUntillNextRound[i] > 0f)
                {
                    m_TimeUntillNextRound[i] -= 1f * Time.deltaTime;
                }
                else
                {
                    m_TimeUntillNextRound[i] = 0f;
                }
            }

            // Return firing animation
            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].fireAnimationVarName, false);

            // Check trigger press
            if (m_WeaponCollection.weapons[m_SelectedWeapon].firingType == Weapon.FiringType.Semi && m_IsInputFire || m_BurstRoundsCount < m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst)
            { }
            else if (m_WeaponCollection.weapons[m_SelectedWeapon].firingType == Weapon.FiringType.Auto && m_IsInputAutoFire || m_BurstRoundsCount < m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst)
            { }
            else
            {
                return;
            }

            // Check firing restrictions
            if (m_TimeUntillNextRound[m_SelectedWeapon] <= 0f && m_CurrentCapacity[m_SelectedWeapon] >= m_WeaponCollection.weapons[m_SelectedWeapon].ammoLossPerRound && !m_IsReloading && !m_IsSwitching && !m_IsRunning && m_CurrentRunningRecoveryTime <= 0f && m_CurrentPReloadInterruptionTime <= 0f) // ? isRunning needed with currentRunningTransition? & currentPReloadInter..?
            { }
            else
            {
                return;
            }

            Vector3 randomisedSpread;

            // The firing
            for (int i = 1; i <= m_WeaponCollection.weapons[m_SelectedWeapon].outputPerRound; i++)
            {
                // Spread
                if (m_CurrentAimingTime >= 1f)
                {
                    randomisedSpread = new Vector3(Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].aimingSpread, m_WeaponCollection.weapons[m_SelectedWeapon].aimingSpread), Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].aimingSpread, m_WeaponCollection.weapons[m_SelectedWeapon].aimingSpread), Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].aimingSpread, m_WeaponCollection.weapons[m_SelectedWeapon].aimingSpread));
                }
                else if (m_IsWalking && m_IsInputAutoFire)
                {
                    randomisedSpread = new Vector3(Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].movementSpread, m_WeaponCollection.weapons[m_SelectedWeapon].movementSpread), Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].movementSpread, m_WeaponCollection.weapons[m_SelectedWeapon].movementSpread), Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].movementSpread, m_WeaponCollection.weapons[m_SelectedWeapon].movementSpread));
                }
                else
                {
                    randomisedSpread = new Vector3(Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].spread, m_WeaponCollection.weapons[m_SelectedWeapon].spread), Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].spread, m_WeaponCollection.weapons[m_SelectedWeapon].spread), Random.Range(-m_WeaponCollection.weapons[m_SelectedWeapon].spread, m_WeaponCollection.weapons[m_SelectedWeapon].spread));
                }

                // Output type
                if (m_WeaponCollection.weapons[m_SelectedWeapon].outputType == Weapon.OutputType.Ray)
                {
                    RaycastHit hit;

                    // Needed to ignore multiple layers (FirstPerson, ThirdPerson & Projectile)
                    // Raycast's integer parameter behaves like bool array at bit level
                    // '~' converts integer to negitive spectrum, thus defines listed layers will be ignored
                    int layersToIgnore = ~LayerMask.GetMask(m_FirstPersonLayerName, m_ThirdPersonLayerName, m_ProjectileLayerName);

                    // ?    need raycast to ignore collider of FPSController, without ignoring other player's FPSController, may involve RaycastAll
                    if (Physics.Raycast(m_CameraRaySpawn.position, m_CameraRaySpawn.forward + randomisedSpread, out hit, m_WeaponCollection.weapons[m_SelectedWeapon].rayMode.range, layersToIgnore))
                    {
                        if (m_WeaponCollection.weapons[m_SelectedWeapon].rayMode.rayImpact != null)
                        {
                            GameObject hitInstance = Instantiate(m_WeaponCollection.weapons[m_SelectedWeapon].rayMode.rayImpact, hit.point, Quaternion.LookRotation(hit.normal));
                            Destroy(hitInstance, 0.5f); // ? should be user controllable?
                        }

                        if (IsHitValid(hit))
                        {
                            // point damage
                            damageable = hit.collider.GetComponent<Damageable>();

                            if (damageable)
                            {
                                damageable.InflictDamage(m_WeaponCollection.weapons[m_SelectedWeapon].rayMode.damage, false);
                            }
                        }
                    }
                }
                else if (m_WeaponCollection.weapons[m_SelectedWeapon].outputType == Weapon.OutputType.Projectile)
                {
                    if (m_WeaponCollection.weapons[m_SelectedWeapon].projectileMode.projectileObject != null)
                    {
                        grenadeInstance = Instantiate(m_WeaponCollection.weapons[m_SelectedWeapon].projectileMode.projectileObject, m_ProjectileSpawn.position, m_ProjectileSpawn.rotation);

                        grenadeInstance.transform.Rotate(90f, 0f, 0f);
                        rb = grenadeInstance.GetComponent<Rigidbody>();
                        rb.AddForce((transform.forward + randomisedSpread) * m_WeaponCollection.weapons[m_SelectedWeapon].projectileMode.launchForce, ForceMode.VelocityChange);
                    }
                }
            }

            // Barrel flash
            if (m_WeaponCollection.weapons[m_SelectedWeapon].barrelFlash != null)
            {
                GameObject flashInstance = Instantiate(m_WeaponCollection.weapons[m_SelectedWeapon].barrelFlash, m_BarrelFlashSpawn.position, m_BarrelFlashSpawn.rotation);
                Destroy(flashInstance, 0.5f);
            }

            // Sound
            m_BarrelFlashSpawn.GetComponent<AudioSource>().Play();

            // Animation
            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].fireAnimationVarName, true);

            // Carrage ejection
            if (m_WeaponCollection.weapons[m_SelectedWeapon].ejectedCartridge.ejectedObject != null)
            {
                GameObject cartridgeInstance = Instantiate(m_WeaponCollection.weapons[m_SelectedWeapon].ejectedCartridge.ejectedObject, m_EjectedCartridgeSpawn.position, m_EjectedCartridgeSpawn.rotation);
                Destroy(cartridgeInstance, 2.0f);

                Physics.IgnoreCollision(cartridgeInstance.GetComponent<Collider>(), m_FPSPlayer.GetComponent<Collider>());

                Vector3 ejectionTrajectory = m_EjectedCartridgeSpawn.rotation * m_WeaponCollection.weapons[m_SelectedWeapon].ejectedCartridge.ejectionTrajectory.normalized;

                cartridgeInstance.GetComponent<Rigidbody>().AddForce((ejectionTrajectory * m_WeaponCollection.weapons[m_SelectedWeapon].ejectedCartridge.ejectionForce) + (m_CharacterController.velocity * 3f));
            }

            // Mange burst count
            m_BurstRoundsCount--;
            if (m_BurstRoundsCount <= 0)
            {
                m_BurstRoundsCount = m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst;
            }

            // After calculations
            m_TimeUntillNextRound[m_SelectedWeapon] = 1f / m_WeaponCollection.weapons[m_SelectedWeapon].fireRate;
            m_CurrentCapacity[m_SelectedWeapon] -= m_WeaponCollection.weapons[m_SelectedWeapon].ammoLossPerRound;
        }

        void CheckWeaponSwitch()
        {
            // Prevent switching while switching & allow switching if current weapon is disabled
            if (!((m_IsInputSwitch || m_IsCurrentWeaponDisabled) && !m_IsSwitching))
            {
                return;
            }

            m_LastSelectedWeapon = m_SelectedWeapon;
            m_SelectedWeapon++;

            if (m_SelectedWeapon >= m_WeaponsLength)
            {
                m_SelectedWeapon = 0;
            }

            // Find next enabled weapon
            while (!m_IsWeaponEnabled[m_SelectedWeapon])
            {
                m_SelectedWeapon++;

                if (m_SelectedWeapon >= m_WeaponsLength)
                {
                    m_SelectedWeapon = 0;
                }
            }

            // Abort if no other weapon if found
            if (m_LastSelectedWeapon == m_SelectedWeapon)
            {
                return;
            }

            // End weapon reloading process
            if (m_ReloadWeaponCoroutine != null)
            {
                StopCoroutine(m_ReloadWeaponCoroutine);
            }

            StartCoroutine(WaitSwitchWeapon());

            m_IsReloading = false;
        }

        IEnumerator WaitSwitchWeapon()
        {
            m_IsSwitching = true;

            GetComponent<AudioSource>().clip = m_WeaponCollection.weapons[m_LastSelectedWeapon].switchOutSound;
            GetComponent<AudioSource>().Play();

            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].switchAnimationVarName, true);

            // Switching out last weapon
            yield return new WaitForSeconds(m_WeaponCollection.weapons[m_SelectedWeapon].switchingTime);

            GetComponent<AudioSource>().clip = m_WeaponCollection.weapons[m_SelectedWeapon].switchInSound;
            GetComponent<AudioSource>().Play();

            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].switchAnimationVarName, false);

            LoadSelectedWeapon();

            // Switching in next weapon
            yield return new WaitForSeconds(m_WeaponCollection.weapons[m_SelectedWeapon].switchingTime);

            m_IsSwitching = false;
            m_IsCurrentWeaponDisabled = false;

        }

        void CheckWeaponReload()
        {

            // Decrement interruption time
            if (m_CurrentPReloadInterruptionTime > 0f)
            {
                m_CurrentPReloadInterruptionTime -= 1f * Time.deltaTime;
            }
            else
            {
                m_CurrentPReloadInterruptionTime = 0f;
            }

            // Stop repeated partical reload on fire input
            if (
                m_IsReloading &&
                (m_WeaponCollection.weapons[m_SelectedWeapon].firingType == Weapon.FiringType.Semi && m_IsInputFire || m_WeaponCollection.weapons[m_SelectedWeapon].firingType == Weapon.FiringType.Auto && m_IsInputAutoFire) &&
                m_WeaponCollection.weapons[m_SelectedWeapon].reloadingType == Weapon.ReloadingType.PartialRepeat && m_CurrentCapacity[m_SelectedWeapon] > 0 // #
                )
            {
                if (m_ReloadWeaponCoroutine != null)
                {
                    StopCoroutine(m_ReloadWeaponCoroutine);
                }

                m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].reloadAniamtionVarName, false);
                m_BurstRoundsCount = m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst;

                if (GetComponent<AudioSource>().clip == m_WeaponCollection.weapons[m_SelectedWeapon].reloadSound)
                {
                    GetComponent<AudioSource>().Stop();
                }

                m_CurrentPReloadInterruptionTime = m_WeaponCollection.weapons[m_SelectedWeapon].partialReloadInterruptionTime;
                m_IsReloading = false;
            }

            // Start reload coroutines
            if (
                (m_CurrentCapacity[m_SelectedWeapon] < m_WeaponCollection.weapons[m_SelectedWeapon].ammoLossPerRound || (m_CurrentCapacity[m_SelectedWeapon] < m_WeaponCollection.weapons[m_SelectedWeapon].capacity && m_IsInputReload)) &&
                m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] > 0 &&
                !m_IsReloading && !m_IsSwitching && !m_IsRunning
                )
            {
                if (m_WeaponCollection.weapons[m_SelectedWeapon].reloadingType == Weapon.ReloadingType.PartialRepeat)
                {
                    m_ReloadWeaponCoroutine = StartCoroutine(WaitReloadWeaponRepeat());
                }
                else
                {
                    m_ReloadWeaponCoroutine = StartCoroutine(WaitReloadWeapon());
                }
            }
        }

        IEnumerator WaitReloadWeapon()
        {
            m_IsReloading = true;

            GetComponent<AudioSource>().clip = m_WeaponCollection.weapons[m_SelectedWeapon].reloadSound;
            GetComponent<AudioSource>().Play();

            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].reloadAniamtionVarName, true);

            // Reloading weapon
            yield return new WaitForSeconds(m_WeaponCollection.weapons[m_SelectedWeapon].reloadingTime);

            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].reloadAniamtionVarName, false);

            m_BurstRoundsCount = m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst;

            if (m_IsInfiniteAmmo)
            {
                m_CurrentCapacity[m_SelectedWeapon] = m_WeaponCollection.weapons[m_SelectedWeapon].capacity;
            }
            else
            {
                // Will reload fill weapon capacity completely
                if (
                    m_WeaponCollection.weapons[m_SelectedWeapon].reloadingType == Weapon.ReloadingType.Full ||
                    (m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload > (m_WeaponCollection.weapons[m_SelectedWeapon].capacity - m_CurrentCapacity[m_SelectedWeapon]))
                    )
                {
                    // Is there enough ammo for reload
                    if (m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] < (m_WeaponCollection.weapons[m_SelectedWeapon].capacity - m_CurrentCapacity[m_SelectedWeapon]))
                    {
                        // Reload weapon with remaining total ammo
                        m_CurrentCapacity[m_SelectedWeapon] += m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name];
                        m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] = 0;
                    }
                    else
                    {
                        // Reload weapon fully
                        m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] -= (m_WeaponCollection.weapons[m_SelectedWeapon].capacity - m_CurrentCapacity[m_SelectedWeapon]);
                        m_CurrentCapacity[m_SelectedWeapon] = m_WeaponCollection.weapons[m_SelectedWeapon].capacity;
                    }
                }
                else
                {
                    // Is there enough ammo for reload
                    if (m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] < m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload)
                    {
                        // Reload weapon with remaining total ammo
                        m_CurrentCapacity[m_SelectedWeapon] += m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name];
                        m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] = 0;
                    }
                    else
                    {
                        // Reload weapon with expected partial amount
                        m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] -= m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload;
                        m_CurrentCapacity[m_SelectedWeapon] += m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload;

                    }
                }
            }

            // Prevents firing when weapon is transitioning from reloading to idle animations
            if (m_WeaponCollection.weapons[m_SelectedWeapon].reloadingType == Weapon.ReloadingType.Partial)
            {
                m_CurrentPReloadInterruptionTime = m_WeaponCollection.weapons[m_SelectedWeapon].partialReloadInterruptionTime;
            }

            m_IsReloading = false;
        }

        IEnumerator WaitReloadWeaponRepeat()
        {
            m_IsReloading = true;

            GetComponent<AudioSource>().clip = m_WeaponCollection.weapons[m_SelectedWeapon].reloadSound;

            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].reloadAniamtionVarName, true);

            // Repeat reloading process
            for (int i = m_CurrentCapacity[m_SelectedWeapon]; i < m_WeaponCollection.weapons[m_SelectedWeapon].capacity; i += m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload)
            {
                GetComponent<AudioSource>().Play();

                // Reloading weapon
                yield return new WaitForSeconds(m_WeaponCollection.weapons[m_SelectedWeapon].reloadingTime);

                if (m_IsInfiniteAmmo)
                {
                    m_CurrentCapacity[m_SelectedWeapon] = m_WeaponCollection.weapons[m_SelectedWeapon].capacity;
                }
                else
                {
                    // Will reload fill weapon capacity completely
                    if (m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload > (m_WeaponCollection.weapons[m_SelectedWeapon].capacity - m_CurrentCapacity[m_SelectedWeapon]))
                    {
                        // Is there enough ammo for reload
                        if (m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] < (m_WeaponCollection.weapons[m_SelectedWeapon].capacity - m_CurrentCapacity[m_SelectedWeapon]))
                        {
                            // Reload weapon with remaining total ammo
                            m_CurrentCapacity[m_SelectedWeapon] += m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name];
                            m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] = 0;
                        }
                        else
                        {
                            // Reload weapon fully
                            m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] -= (m_WeaponCollection.weapons[m_SelectedWeapon].capacity - m_CurrentCapacity[m_SelectedWeapon]);
                            m_CurrentCapacity[m_SelectedWeapon] = m_WeaponCollection.weapons[m_SelectedWeapon].capacity;
                        }
                    }
                    else
                    {
                        // Is there enough ammo for reload
                        if (m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] < m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload)
                        {
                            // Reload weapon with remaining total ammo
                            m_CurrentCapacity[m_SelectedWeapon] += m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name];
                            m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] = 0;
                        }
                        else
                        {
                            // Reload weapon with expected partial amount
                            m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] -= m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload;
                            m_CurrentCapacity[m_SelectedWeapon] += m_WeaponCollection.weapons[m_SelectedWeapon].ammoAddedPerReload;

                        }
                    }
                }

                // Ensures reloading stops when total ammo runs out
                if (m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name] <= 0)
                {
                    i = m_WeaponCollection.weapons[m_SelectedWeapon].capacity;
                }
            }

            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].reloadAniamtionVarName, false);

            m_BurstRoundsCount = m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst;

            // Prevents firing when weapon is transitioning from reloading to idle animations
            m_CurrentPReloadInterruptionTime = m_WeaponCollection.weapons[m_SelectedWeapon].partialReloadInterruptionTime;

            m_IsReloading = false;
        }

        void CheckWeaponAim()
        {
            if (m_IsInputAim && !m_IsReloading && !m_IsSwitching && !m_IsRunning)
            {
                m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].aimAnimationVarName, true);

                // Increase aiming effect
                if (m_CurrentAimingTime < 1f)
                {
                    m_CurrentAimingTime += (1f / m_WeaponCollection.weapons[m_SelectedWeapon].aimingTime) * Time.deltaTime;

                    if (m_CurrentAimingTime > 1f)
                    {
                        m_CurrentAimingTime = 1f;
                    }
                }
            }
            else
            {
                m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].aimAnimationVarName, false);

                // Decrease aiming effect
                if (m_CurrentAimingTime > 0f)
                {
                    m_CurrentAimingTime -= (1f / m_WeaponCollection.weapons[m_SelectedWeapon].aimingTime) * Time.deltaTime;

                    if (m_CurrentAimingTime < 0f)
                    {
                        m_CurrentAimingTime = 0f;
                    }
                }
            }
        }

        void LoadSelectedWeapon()
        {
            // Remove old weapon instance
            if (m_InstantiatedWeaponObject != null)
            {
                Destroy(m_InstantiatedWeaponObject);
            }

            // Spawn new weapon instance
            if (m_WeaponCollection.weapons[m_SelectedWeapon].weaponPrefab != null)
            {
                m_InstantiatedWeaponObject = Instantiate(m_WeaponCollection.weapons[m_SelectedWeapon].weaponPrefab, transform);

                // Removes "(clone)" from name of spawned weapon object, to prevent animations from disconnecting with object
                const int numberOfCharacterToRemove = 7;

                m_InstantiatedWeaponObject.gameObject.name = m_InstantiatedWeaponObject.gameObject.name.Remove(m_InstantiatedWeaponObject.gameObject.name.Length - numberOfCharacterToRemove);
            }

            // Find points for weapon functionality
            try
            {
                m_BarrelFlashSpawn = GameObject.Find("WeaponSpace/" + m_WeaponCollection.weapons[m_SelectedWeapon].barrelFlashSpawnName).transform;
            }
            catch
            {
                throw new System.Exception("Cannot find Barrel Flash Spawn object, ensure Barrel Flash Spawn Name field matches object's name.");
            }

            try
            {
                m_ProjectileSpawn = GameObject.Find("WeaponSpace/" + m_WeaponCollection.weapons[m_SelectedWeapon].projectileSpawnName).transform;
            }
            catch
            {
                // ?    Change text when field name changes
                throw new System.Exception("Cannot find Projectile Spawn object, ensure Projectile Spawn Name field matches object's name.");
            }

            try
            {
                m_EjectedCartridgeSpawn = GameObject.Find("WeaponSpace/" + m_WeaponCollection.weapons[m_SelectedWeapon].cartridgeSpawnName).transform;
            }
            catch
            {
                // ?    Change text when field name changes
                throw new System.Exception("Cannot find Cartridge Spawn object, ensure Cartridge Spawn Name field matches object's name.");
            }

            m_BarrelFlashSpawn.GetComponent<AudioSource>().clip = m_WeaponCollection.weapons[m_SelectedWeapon].barrelSound;
            m_BurstRoundsCount = m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst;

            // Prevents animation disconnection issue (if first weapon controller is pre-applied to animator)
            m_Animator.runtimeAnimatorController = null;

            m_Animator.runtimeAnimatorController = m_WeaponCollection.weapons[m_SelectedWeapon].animatorController;

        }

        public void EnableWeapon(Weapon weapon, bool state, int ammoInWeapon)
        {
            for (int i = 0; i < m_WeaponsLength; i++)
            {
                if (m_WeaponCollection.weapons[i].name == weapon.name)
                {
                    if (state && !m_IsWeaponEnabled[i])
                    {
                        m_IsWeaponEnabled[i] = true;
                        m_TimeUntillNextRound[i] = 1f / m_WeaponCollection.weapons[i].fireRate;

                        // Prevents current ammo being negitive or over weapon capacity
                        if (ammoInWeapon > m_WeaponCollection.weapons[i].capacity)
                        {
                            m_CurrentCapacity[i] = m_WeaponCollection.weapons[i].capacity;

                        }
                        else if (ammoInWeapon < 0)
                        {
                            m_CurrentCapacity[i] = 0;
                        }
                        else
                        {
                            m_CurrentCapacity[i] = ammoInWeapon;
                        }


                        if (OnAddedWeapon != null)
                        {
                            OnAddedWeapon.Invoke(m_WeaponCollection, i);

                            break;
                        }
                    }
                    else if (!state && m_IsWeaponEnabled[i])
                    {
                        m_IsWeaponEnabled[i] = false;

                        // ? should switch out weapon is currently weilded 
                        if (i == m_SelectedWeapon)
                        {
                            m_IsCurrentWeaponDisabled = true;
                        }

                    }

                    i = m_WeaponsLength;
                }
            }
        }

        public bool isEnabled(Weapon weapon)
        {
            if (weapon != null)
            {
                for (int i = 0; i < m_WeaponsLength; i++)
                {
                    if (m_WeaponCollection.weapons[i].name == weapon.name)
                    {
                        return m_IsWeaponEnabled[i];
                    }
                }
            }

            return false;
        }

        void EnableWeapon(int index, bool state, int ammoInWeapon)
        {
            if (state && !m_IsWeaponEnabled[index])
            {
                m_IsWeaponEnabled[index] = true;
                m_TimeUntillNextRound[index] = 1f / m_WeaponCollection.weapons[index].fireRate;

                // Prevents current ammo being negitive or over weapon capacity
                if (ammoInWeapon > m_WeaponCollection.weapons[index].capacity)
                {
                    m_CurrentCapacity[index] = m_WeaponCollection.weapons[index].capacity;
                }
                else if (ammoInWeapon < 0)
                {
                    m_CurrentCapacity[index] = 0;
                }
                else
                {
                    m_CurrentCapacity[index] = ammoInWeapon;
                }
            }
            else if (!state && m_IsWeaponEnabled[index])
            {
                m_IsWeaponEnabled[index] = false;

                // ? should switch out weapon is currently weilded
                if (index == m_SelectedWeapon)
                {
                    m_IsCurrentWeaponDisabled = true;
                }
            }
        }

        public void IncreaseAmmoCount(Ammo ammo, int amount)
        {
            for (int i = 0; i < m_WeaponsLength; i++)
            {
                if (m_WeaponCollection.weapons[i].ammo.name == ammo.name)
                {
                    m_AmmoAmounts[m_WeaponCollection.weapons[i].ammo.name] += amount;

                    i = m_WeaponsLength;
                }
            }
        }

        void CheckCharacterMovement()
        {
            // Decrement running transition
            if (m_CurrentRunningRecoveryTime > 0f)
            {
                m_CurrentRunningRecoveryTime -= 1f * Time.deltaTime;
            }
            else
            {
                m_CurrentRunningRecoveryTime = 0f;
            }

            // State conditions, used across script
            m_IsJumping = false;
            m_IsRunning = false;
            m_IsWalking = false;

            if (!m_CharacterController.isGrounded) // Jumping
            {
                m_IsJumping = true;
            }
            if (m_IsInputRun && new Vector2(m_CharacterController.velocity.x, m_CharacterController.velocity.z).magnitude > 0f) // Running
            {
                m_IsRunning = true;
            }
            else if (new Vector2(m_CharacterController.velocity.x, m_CharacterController.velocity.z).magnitude > 0f) // Walking
            {
                m_IsWalking = true;
            }

            // Animation effected by the states
            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].jumpAnimationVarName, false);
            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].runAnimationVarName, false);
            m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].walkAnimationVarName, false);

            if (m_IsJumping)
            {
                m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].jumpAnimationVarName, true);
            }
            if (m_IsRunning)
            {
                m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].runAnimationVarName, true);
                m_CurrentRunningRecoveryTime = m_WeaponCollection.weapons[m_SelectedWeapon].runningRecoveryTime;

                // Abort weapon reloading
                if (m_IsReloading)
                {
                    if (m_ReloadWeaponCoroutine != null)
                    {
                        // Prevents continuing burst-fire after partial repeat reloading is aborted
                        m_BurstRoundsCount = m_WeaponCollection.weapons[m_SelectedWeapon].roundsPerBurst;

                        StopCoroutine(m_ReloadWeaponCoroutine);
                    }

                    m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].reloadAniamtionVarName, false);

                    m_IsReloading = false;
                }
            }
            if (m_IsWalking)
            {
                m_Animator.SetBool(m_WeaponCollection.weapons[m_SelectedWeapon].walkAnimationVarName, true);
            }
        }

        void CheckMouseMovement()
        {
            const float mouseMovementRate = 0.1f;             // ? should be public property, for users to access without inspector
            const float mouseMovementRounding = 0.001f;       // ? should be public property, for users to access without inspector

            // Allows mouse movement influance to adjust gradully, relying on mouse GetAxis alone causes jittering 
            if (m_CurrentMouseInfluance.x < Input.GetAxis(m_MouseXInfluenceName) - mouseMovementRounding || m_CurrentMouseInfluance.x > Input.GetAxis(m_MouseXInfluenceName) + mouseMovementRounding)
            {
                m_CurrentMouseInfluance.x += (Input.GetAxis(m_MouseXInfluenceName) - m_CurrentMouseInfluance.x) * mouseMovementRate;
            }
            else
            {
                m_CurrentMouseInfluance.x = Input.GetAxis(m_MouseXInfluenceName);
            }

            // Allows mouse movement influance to adjust gradully, relying on mouse GetAxis alone causes jittering 
            if (m_CurrentMouseInfluance.y < Input.GetAxis(m_MouseYInfluenceName) - mouseMovementRounding || m_CurrentMouseInfluance.y > Input.GetAxis(m_MouseYInfluenceName) + mouseMovementRounding)
            {
                m_CurrentMouseInfluance.y += (Input.GetAxis(m_MouseYInfluenceName) - m_CurrentMouseInfluance.y) * mouseMovementRate;
            }
            else
            {
                m_CurrentMouseInfluance.y = Input.GetAxis(m_MouseYInfluenceName);
            }


            m_Animator.SetFloat(m_WeaponCollection.weapons[m_SelectedWeapon].mouseXAnimationVarName, m_CurrentMouseInfluance.x);
            m_Animator.SetFloat(m_WeaponCollection.weapons[m_SelectedWeapon].mouseYAnimationVarName, m_CurrentMouseInfluance.y);
        }

        void UpdateHUD()
        {
            if (!m_IsSwitching)
            {

                m_UICrosshairSpace.sprite = m_WeaponCollection.weapons[m_SelectedWeapon].crosshairSprite;

                if (m_UICrosshairSpace.sprite != null && m_CurrentAimingTime == 0f)
                {
                    m_UICrosshairSpace.color = m_WeaponCollection.weapons[m_SelectedWeapon].crosshairColour;
                }
                else
                {
                    Color c = m_WeaponCollection.weapons[m_SelectedWeapon].crosshairColour;
                    m_UICrosshairSpace.color = new Color(c.r, c.g, c.b, 0f);
                }

                //TODO: add infinity ammo mode: ∞ works
                m_UIMagAmmoCount.text = m_CurrentCapacity[m_SelectedWeapon].ToString();

                if (m_IsInfiniteAmmo)
                {
                    m_UITotalAmmoCount.text = "∞";
                }
                else
                {
                    m_UITotalAmmoCount.text = m_AmmoAmounts[m_WeaponCollection.weapons[m_SelectedWeapon].ammo.name].ToString();
                }
                m_UIAmmoIconSpace.sprite = m_WeaponCollection.weapons[m_SelectedWeapon].ammo.ammoIcon;

                if (m_GrenadeThrower.m_IsInfiniteGrenade && m_GrenadeThrower.m_CanThrow)
                {
                    m_UITotalGrenadeCount.text = "∞";
                }
                else
                {
                    m_UITotalGrenadeCount.text = m_GrenadeThrower.totalAmount.ToString();
                }

                m_UIGrenadeIconSpace.sprite = m_GrenadeThrower.grenadeIcon;
                tempOpacityColor = m_UIAmmoIconSpace.color;
                tempOpacityColor.a = 1f;
                m_UIAmmoIconSpace.color = tempOpacityColor;
                tempOpacityColor = m_UIGrenadeIconSpace.color;
                tempOpacityColor.a = 1f;
                m_UIGrenadeIconSpace.color = tempOpacityColor;
            }
        }

        void CheckInputs()
        {
            if (Input.GetKeyDown(m_inputFire))
            {
                m_IsInputFire = true;
            }
            else
            {
                m_IsInputFire = false;
            }

            if (Input.GetKey(m_inputAutoFire))
            {
                m_IsInputAutoFire = true;
            }
            else
            {
                m_IsInputAutoFire = false;
            }

            if (Input.GetKeyDown(m_inputReload))
            {
                m_IsInputReload = true;
            }
            else
            {
                m_IsInputReload = false;
            }

            if (Input.GetKeyDown(m_inputSwitch))
            {
                m_IsInputSwitch = true;
            }
            else
            {
                m_IsInputSwitch = false;
            }

            if (Input.GetKey(m_inputAim))
            {
                m_IsInputAim = true;
            }
            else
            {
                m_IsInputAim = false;
            }

            if (Input.GetKey(m_inputRun))
            {
                m_IsInputRun = true;
            }
            else
            {
                m_IsInputRun = false;
            }
        }

        public bool IsGrenadeCollectable()
        {
            if(!m_GrenadeThrower.m_IsInfiniteGrenade || 
                (m_GrenadeThrower.m_CanThrow && !m_GrenadeThrower.m_IsInfiniteGrenade) ||
                (!m_GrenadeThrower.m_CanThrow && m_GrenadeThrower.m_IsInfiniteGrenade))
            {
                return true;
            }

            return false;
        }

        // ignore hits with triggers that don't have a Damageable component or hits that dont have a rigid body
        bool IsHitValid(RaycastHit hit)
        {
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
            {
                return false;
            }

            return true;
        }
    }
}
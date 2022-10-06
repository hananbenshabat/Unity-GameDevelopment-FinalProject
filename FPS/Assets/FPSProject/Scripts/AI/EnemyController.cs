using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.UI;
using Unity.FPS.FPSController;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [System.Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        [Tooltip("tag")]
        public string m_AITag;

        [Header("Parameters")]
        [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
        public float SelfDestructYHeight = -20f;

        [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
        public float PathReachingRadius = 2f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 10f;

        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 0f;


        [Header("Weapons Parameters")] [Tooltip("Allow weapon swapping for this enemy")]
        public bool SwapToNextWeapon = false;

        [Tooltip("Time delay between a weapon swap and the next attack")]
        public float DelayAfterWeaponSwap = 0f;

        [Header("Eye color")] [Tooltip("Material for the eye color")]
        public Material EyeColorMaterial;

        [Tooltip("The default color of the bot's eye")] [ColorUsageAttribute(true, true)]
        public Color DefaultEyeColor;

        [Tooltip("The attack color of the bot's eye")] [ColorUsageAttribute(true, true)]
        public Color AttackEyeColor;

        [Header("Flash on hit")] [Tooltip("The material used for the body of the hoverbot")]
        public Material BodyMaterial;

        [Tooltip("The gradient representing the color of the flash on hit")] [GradientUsageAttribute(true)]
        public Gradient OnHitBodyGradient;

        [Tooltip("The duration of the flash on hit")]
        public float FlashOnHitDuration = 0.5f;

        [Header("Sounds")] [Tooltip("Sound played when recieving damages")]
        public AudioClip DamageTick;

        [Tooltip("Sound played when Throwing a grenade")]
        public AudioClip m_ThrowAudio;

        [Header("VFX")] [Tooltip("The VFX prefab spawned when the enemy dies")]
        public GameObject DeathVfx;

        [Tooltip("The point at which the death VFX is spawned")]
        public Transform DeathVfxSpawnPoint;

        [Header("Loot")] [Tooltip("The object this enemy can drop when dying")]
        public GameObject LootPrefab;

        [Tooltip("The chance the object has to drop")] [Range(0, 1)]
        public float DropRate = 1f;

        [Header("Debug Display")] [Tooltip("Color of the sphere gizmo representing the path reaching range")]
        public Color PathReachingRangeColor = Color.yellow;

        [Tooltip("Color of the sphere gizmo representing the attack range")]
        public Color AttackRangeColor = Color.red;

        [Tooltip("Color of the sphere gizmo representing the detection range")]
        public Color DetectionRangeColor = Color.blue;

        [Header("Layers to ignore")]
        [SerializeField] string m_IgnoredLayerName1 = "AICollect";
        [SerializeField] string m_IgnoredLayerName2 = "Enemy";

        [Header("Weapons")]
        [SerializeField] WeaponCollection m_WeaponCollection;

        [Header("Grenade")]
        [SerializeField] GameObject grenadePrefab;

        [Header("Bullet Holes, Trails")]
        [SerializeField] TrailRenderer BulletTrail;
        [SerializeField] ParticleSystem ImpactParticleSystem;

        public UnityAction onAttack;
        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;
        public UnityAction onDamaged;

        List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
        MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
        float m_LastTimeDamaged = float.NegativeInfinity;

        RendererIndexData m_EyeRendererData;
        MaterialPropertyBlock m_EyeColorMaterialPropertyBlock;

        public PatrolPath PatrolPath { get; set; }
        public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
        public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;
        public NavMeshAgent NavMeshAgent { get; private set; }
        public DetectionModule DetectionModule { get; private set; }

        int m_PathDestinationNodeIndex, m_PreviousDestinationNodeIndex, m_LastSelectedWeapon, m_CurrentWeaponIndex, m_WeaponsLength, m_BurstRoundsCount, totalAmount, startAmount = 3;
        EnemyManager m_EnemyManager;
        ActorsManager m_ActorsManager;
        Health m_Health;
        Actor m_Actor;
        Collider[] m_SelfColliders;
        GameFlowManager m_GameFlowManager;
        Dictionary<string, int> m_AmmoAmounts;
        bool[] m_IsWeaponEnabled;
        int[] m_CurrentCapacity;
        float[] m_TimeUntillNextRound;
        float m_CurrentAimingTime, m_CurrentRunningRecoveryTime, m_CurrentPReloadInterruptionTime;
        bool m_IsInfiniteAmmo, m_IsInfiniteGrenade, m_WasDamagedThisFrame, m_IsReloading, m_IsSwitching,
            m_IsRunning, m_IsWalking, m_IsInputFire, m_IsInputAutoFire, m_IsWeaponLoadedAtStart = false, m_CanThrow = false;
        float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity, m_Cooldown = 5f, throwForce = 40f;
        Coroutine m_ReloadWeaponCoroutine;
        Animator m_Animator;
        Rigidbody rb;
        Damageable damageable;
        Dictionary<Health, Damageable> uniqueDamagedHealths;
        GameObject m_InstantiatedWeaponObject, grenadeInstance;
        Transform m_GunSpawn, m_ProjectileSpawn, m_EjectedCartridgeSpawn, m_BarrelFlashSpawn, m_CameraRaySpawn;
        Weapon m_SelectedWeapon, m_CollectedWeapon;
        CollectableObject m_Collect;
        NavigationModule m_NavigationModule;

        private IEnumerator SpawnTrail(TrailRenderer Trail, RaycastHit Hit)
        {
            float time = 0;
            Vector3 startPosition = Trail.transform.position;

            while (time < 1)
            {
                Trail.transform.position = Vector3.Lerp(startPosition, Hit.point, time);
                time += Time.deltaTime / Trail.time;

                yield return null;
            }
            Trail.transform.position = Hit.point;
            Instantiate(ImpactParticleSystem, Hit.point, Quaternion.LookRotation(Hit.normal));

            Destroy(Trail.gameObject, Trail.time);
        }

        void Start()
        {
            m_EnemyManager = FindObjectOfType<EnemyManager>();
            DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyController>(m_EnemyManager, this);

            m_ActorsManager = FindObjectOfType<ActorsManager>();
            DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyController>(m_ActorsManager, this);

            m_EnemyManager.RegisterEnemy(this);

            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);

            m_Actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyController>(m_Actor, this, gameObject);

            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_SelfColliders = GetComponentsInChildren<Collider>();

            m_GameFlowManager = FindObjectOfType<GameFlowManager>();
            DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, EnemyController>(m_GameFlowManager, this);

            if (GameObject.FindGameObjectsWithTag("Settings").Length != 0)
            {
                m_IsInfiniteAmmo = GameObject.FindWithTag("Settings").GetComponent<Settings>().m_IsInfiniteAmmo;
                m_IsInfiniteGrenade = GameObject.FindWithTag("Settings").GetComponent<Settings>().m_IsInfiniteGrenade;
            }
            else
            {
                m_IsInfiniteAmmo = true;
                m_IsInfiniteGrenade = true;
            }

            m_Animator = transform.GetChild(1).transform.gameObject.GetComponent<Animator>();

            m_WeaponsLength = m_WeaponCollection.weapons.Count;

            GrenadeInit();

            m_PreviousDestinationNodeIndex = 0;

            // Subscribe to damage & death actions
            m_Health.OnDie += OnDie;
            m_Health.OnDamaged += OnDamaged;

            // Find and initialize all weapons
            FindAndInitializeAllWeapons();
            var weapon = GetCurrentWeapon();
            //weapon.ShowWeapon(true);

            var detectionModules = GetComponentsInChildren<DetectionModule>();
            DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this,
                gameObject);
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length,
                this, gameObject);
            // Initialize detection module
            DetectionModule = detectionModules[0];
            DetectionModule.onDetectedTarget += OnDetectedTarget;
            DetectionModule.onLostTarget += OnLostTarget;
            onAttack += DetectionModule.OnAttack;

            var navigationModules = GetComponentsInChildren<NavigationModule>();
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length,
                this, gameObject);

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == EyeColorMaterial)
                    {
                        m_EyeRendererData = new RendererIndexData(renderer, i);
                    }

                    if (renderer.sharedMaterials[i] == BodyMaterial)
                    {
                        m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                    }
                }
            }

            m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

            // Check if we have an eye renderer for this enemy
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.MaterialIndex);
            }
        }

        void Update()
        {
            DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

            Color currentColor = OnHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / FlashOnHitDuration);
            m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
            foreach (var data in m_BodyRenderers)
            {
                data.Renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.MaterialIndex);
            }

            m_WasDamagedThisFrame = false;

            // Prepares default weapon on start
            if (!m_IsWeaponLoadedAtStart)
            {
                LoadSelectedWeapon();
                m_IsWeaponLoadedAtStart = true;
            }
        }

        void OnLostTarget()
        {
            onLostTarget.Invoke();

            // Set the eye attack color and property block if the eye renderer is set
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
        }

        void OnDetectedTarget()
        {
            onDetectedTarget.Invoke();

            // Set the eye default color and property block if the eye renderer is set
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", AttackEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
        }

        public void OrientTowards(Vector3 lookPosition)
        {
            Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
            if (lookDirection.sqrMagnitude != 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
            }
        }

        bool IsPathValid()
        {
            return PatrolPath && PatrolPath.PathNodes.Count > 0;
        }

        public void ResetPathDestination()
        {
            m_PathDestinationNodeIndex = 0;
        }

        public void SetPathDestinationToClosestNode()
        {
            if (IsPathValid())
            {
                int closestPathNodeIndex = 0;
                if (PatrolPath.PathNodes.Count > 1)
                {
                    for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
                    {
                        float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                        if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex) &&
                           m_PreviousDestinationNodeIndex != closestPathNodeIndex)
                        {
                            closestPathNodeIndex = i;
                        }
                    }

                }
                m_PathDestinationNodeIndex = closestPathNodeIndex;
                m_PreviousDestinationNodeIndex = m_PathDestinationNodeIndex;
            }
            else
            {
                m_PathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath()
        {
            if (IsPathValid())
            {
                return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
            }
            else
            {
                return transform.position;
            }
        }

        public void SetNavDestination(Vector3 destination)
        {
            if (NavMeshAgent)
            {
                if ((transform.position - GetDestinationOnPath()).magnitude > PathReachingRadius)
                {
                    NavMeshAgent.SetDestination(destination);
                }
            }
        }

        public void UpdatePathDestination(bool inverseOrder = false)
        {
            if (IsPathValid())
            {
                // Check if reached the path destination
                if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
                {
                    // increment path destination index
                    m_PathDestinationNodeIndex =
                        inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                    if (m_PathDestinationNodeIndex < 0)
                    {
                        m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                    }

                    if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
                    {
                        m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                    }
                }
            }
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // test if the damage source is the player
            if (damageSource && !damageSource.GetComponent<EnemyController>())
            {
                // pursue the player
                DetectionModule.OnDamaged(damageSource);

                OnDamaged(damage);
            }
        }

        void OnDamaged(float damage)
        {
            onDamaged?.Invoke();
            m_LastTimeDamaged = Time.time;

            // play the damage tick sound
            if (DamageTick && !m_WasDamagedThisFrame)
                AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

            m_WasDamagedThisFrame = true;
        }

        void OnDie()
        {
            // spawn a particle system when dying
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);

            // tells the game flow manager to handle the enemy destuction
            m_EnemyManager.UnregisterEnemy(this);

            // loot an object
            if (TryDropItem())
            {
                Instantiate(LootPrefab, transform.position, Quaternion.identity);
            }

            // this will call the OnDestroy function
            Destroy(gameObject, DeathDuration);
        }

        void OnDrawGizmosSelected()
        {
            // Path reaching range
            Gizmos.color = PathReachingRangeColor;
            Gizmos.DrawWireSphere(transform.position, PathReachingRadius);

            if (DetectionModule != null)
            {
                // Detection range
                Gizmos.color = DetectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.DetectionRange);

                // Attack range
                Gizmos.color = AttackRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.AttackRange);
            }
        }

        public void OrientWeaponsTowards(Vector3 lookPosition)
        {
            for (int i = 0; i < m_WeaponsLength; i++)
            {
                // orient weapon towards player
                Vector3 weaponForward = (lookPosition - m_GunSpawn.position).normalized;
                m_GunSpawn.forward = weaponForward;

                //weaponForward = (lookPosition - m_BarrelFlashSpawn.position).normalized;
                //m_BarrelFlashSpawn.forward = weaponForward;
            }
        }

        //public bool TryAtack(Vector3 enemyPosition)
        public bool TryAtack(Vector3 enemyPosition)
        {
            bool didFire = false;


            if (m_GameFlowManager.GameIsEnding)
                return false;

            OrientWeaponsTowards(enemyPosition);
            m_IsInputFire = true;
            m_IsInputAutoFire = true;
            CheckWeaponFire();
            CheckWeaponReload();

            if ((m_LastTimeWeaponSwapped + DelayAfterWeaponSwap) >= Time.time)
                return false;

            didFire = true;

            if (didFire && onAttack != null)
            {
                onAttack.Invoke();

                if (SwapToNextWeapon && m_WeaponsLength > 1)
                {
                    int nextWeaponIndex = (m_CurrentWeaponIndex + 1) % m_WeaponsLength;
                    SetCurrentWeapon(nextWeaponIndex);
                }
            }

            CheckGrenade();


            return didFire;
        }

        public bool TryDropItem()
        {
            if (DropRate == 0 || LootPrefab == null)
                return false;
            else if (DropRate == 1)
                return true;
            else
                return (Random.value <= DropRate);
        }

        void FindAndInitializeAllWeapons()
        {
            m_AmmoAmounts = new Dictionary<string, int>();
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
                m_CurrentWeaponIndex = j;
                m_SelectedWeapon = m_WeaponCollection.weapons[j];

                if (m_SelectedWeapon.enableOnStart)
                {
                    if (m_SelectedWeapon.loadedAtStart)
                    {
                        EnableWeapon(m_SelectedWeapon, true, m_SelectedWeapon.capacity);
                    }
                    else
                    {
                        EnableWeapon(m_SelectedWeapon, true, 0);
                    }
                }
            }

            for (int i = 0; i < m_WeaponsLength; i++)
            {
                if (m_WeaponCollection.weapons[i].enableOnStart)
                {
                    m_CurrentWeaponIndex = i;
                    m_SelectedWeapon = m_WeaponCollection.weapons[i];
                    i = m_WeaponsLength;
                }
            }
        }

        public WeaponCollection GetCurrentWeapon()
        {
            FindAndInitializeAllWeapons();
            // Check if no weapon is currently selected
            if (m_SelectedWeapon == null)
            {
                // Set the first weapon of the weapons list as the current weapon
                SetCurrentWeapon(0);
            }

            //DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyController>(m_WeaponCollection, this, gameObject);

            return m_WeaponCollection;
        }

        void SetCurrentWeapon(int index)
        {
            m_CurrentWeaponIndex = index;
            m_SelectedWeapon = m_WeaponCollection.weapons[m_CurrentWeaponIndex];
            if (SwapToNextWeapon)
            {
                m_LastTimeWeaponSwapped = Time.time;
            }
            else
            {
                m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
            }
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
            m_Animator.SetBool(m_SelectedWeapon.fireAnimationVarName, false);

            // Check trigger press
            if (m_SelectedWeapon.firingType == Weapon.FiringType.Semi && m_IsInputFire || m_BurstRoundsCount < m_SelectedWeapon.roundsPerBurst)
            { }
            else if (m_SelectedWeapon.firingType == Weapon.FiringType.Auto && m_IsInputAutoFire || m_BurstRoundsCount < m_SelectedWeapon.roundsPerBurst)
            { }
            else
            {
                return;
            }

            // Check firing restrictions
            if (m_TimeUntillNextRound[m_CurrentWeaponIndex] <= 0f && m_CurrentCapacity[m_CurrentWeaponIndex] >= m_SelectedWeapon.ammoLossPerRound 
                && !m_IsReloading && !m_IsSwitching && !m_IsRunning && m_CurrentRunningRecoveryTime <= 0f && m_CurrentPReloadInterruptionTime <= 0f) // ? isRunning needed with currentRunningTransition? & currentPReloadInter..?
            { }
            else
            {
                return;
            }

            Vector3 randomisedSpread;

            // The firing
            for (int i = 1; i <= m_SelectedWeapon.outputPerRound; i++)
            {
                // Spread
                if (m_CurrentAimingTime >= 1f)
                {
                    randomisedSpread = new Vector3(Random.Range(-m_SelectedWeapon.aimingSpread, m_SelectedWeapon.aimingSpread), 
                        Random.Range(-m_SelectedWeapon.aimingSpread, m_SelectedWeapon.aimingSpread), 
                        Random.Range(-m_SelectedWeapon.aimingSpread, m_SelectedWeapon.aimingSpread));
                }
                else if (m_IsWalking && m_IsInputAutoFire)
                {
                    randomisedSpread = new Vector3(Random.Range(-m_SelectedWeapon.movementSpread, m_SelectedWeapon.movementSpread), 
                        Random.Range(-m_SelectedWeapon.movementSpread, m_SelectedWeapon.movementSpread), 
                        Random.Range(-m_SelectedWeapon.movementSpread, m_SelectedWeapon.movementSpread));
                }
                else
                {
                    randomisedSpread = new Vector3(Random.Range(-m_SelectedWeapon.spread, m_SelectedWeapon.spread), 
                        Random.Range(-m_SelectedWeapon.spread, m_SelectedWeapon.spread), Random.Range(-m_SelectedWeapon.spread, m_SelectedWeapon.spread));
                }

                // Output type
                if (m_SelectedWeapon.outputType == Weapon.OutputType.Ray)
                {
                    RaycastHit hit;

                    // Needed to ignore multiple layers (FirstPerson, ThirdPerson & Projectile)
                    // Raycast's integer parameter behaves like bool array at bit level
                    // '~' converts integer to negitive spectrum, thus defines listed layers will be ignored
                    
                    // I don't really need to ignore any layers anymore..
                    int layersToIgnore = ~LayerMask.GetMask("Water", m_IgnoredLayerName1, m_IgnoredLayerName2);

                    // ?    need raycast to ignore collider of Enemy_HoverBot, without ignoring other player's Enemy_HoverBot, may involve RaycastAll
                    if (Physics.Raycast(m_GunSpawn.position, m_GunSpawn.forward + randomisedSpread, out hit, m_SelectedWeapon.rayMode.range, layersToIgnore))
                    {
                        TrailRenderer trail = Instantiate(BulletTrail, m_ProjectileSpawn.position, Quaternion.identity);

                        StartCoroutine(SpawnTrail(trail, hit));

                        if (m_SelectedWeapon.rayMode.rayImpact != null)
                        {
                            GameObject hitInstance = Instantiate(m_SelectedWeapon.rayMode.rayImpact, hit.point, Quaternion.LookRotation(hit.normal));
                            Destroy(hitInstance, 0.5f); // ? should be user controllable?
                        }

                        if (IsHitValid(hit))
                        {
                            // point damage
                            damageable = hit.collider.GetComponent<Damageable>();

                            if (damageable)
                            {
                                damageable.InflictDamage(m_SelectedWeapon.rayMode.damage, false);
                            }
                        }
                    }
                }
                else if (m_SelectedWeapon.outputType == Weapon.OutputType.Projectile)
                {
                    if (m_SelectedWeapon.projectileMode.projectileObject != null)
                    {
                        grenadeInstance = Instantiate(m_SelectedWeapon.projectileMode.projectileObject, m_ProjectileSpawn.position, m_ProjectileSpawn.rotation);

                        grenadeInstance.transform.Rotate(90f, 0f, 0f);
                        rb = grenadeInstance.GetComponent<Rigidbody>();
                        rb.AddForce((transform.forward + randomisedSpread) * m_SelectedWeapon.projectileMode.launchForce, ForceMode.VelocityChange);
                    }
                }
            }

            // Barrel flash
            if (m_SelectedWeapon.barrelFlash != null)
            {
                GameObject flashInstance = Instantiate(m_SelectedWeapon.barrelFlash, m_BarrelFlashSpawn.position, m_BarrelFlashSpawn.rotation);
                Destroy(flashInstance, 0.5f);
            }


            // Carrage ejection
            if (m_SelectedWeapon.ejectedCartridge.ejectedObject != null)
            {
                GameObject cartridgeInstance = Instantiate(m_SelectedWeapon.ejectedCartridge.ejectedObject, m_EjectedCartridgeSpawn.position, m_EjectedCartridgeSpawn.rotation);
                Destroy(cartridgeInstance, 2.0f);

                //Physics.IgnoreCollision(cartridgeInstance.GetComponent<Collider>(), m_FPSPlayer.GetComponent<Collider>());

                Vector3 ejectionTrajectory = m_EjectedCartridgeSpawn.rotation * m_SelectedWeapon.ejectedCartridge.ejectionTrajectory.normalized;

                cartridgeInstance.GetComponent<Rigidbody>().AddForce((ejectionTrajectory * m_SelectedWeapon.ejectedCartridge.ejectionForce));
            }


            // Sound
            m_BarrelFlashSpawn.GetComponent<AudioSource>().Play();

            // Animation
            m_Animator.SetBool(m_SelectedWeapon.fireAnimationVarName, true);

            // Mange burst count
            m_BurstRoundsCount--;
            if (m_BurstRoundsCount <= 0)
            {
                m_BurstRoundsCount = m_SelectedWeapon.roundsPerBurst;
            }

            // After calculations
            m_TimeUntillNextRound[m_CurrentWeaponIndex] = 1f / m_SelectedWeapon.fireRate;
            m_CurrentCapacity[m_CurrentWeaponIndex] -= m_SelectedWeapon.ammoLossPerRound;
        }

        void CheckWeaponReload()
        {
            // Switching to another weapon when run out of ammo
            if ((m_CurrentCapacity[m_CurrentWeaponIndex] == 0) && m_AmmoAmounts[m_SelectedWeapon.ammo.name] == 0 && !m_IsReloading && !m_IsSwitching && !m_IsRunning)
            {
                CheckWeaponSwitch();
            }

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
                (m_SelectedWeapon.firingType == Weapon.FiringType.Semi && m_IsInputFire || m_SelectedWeapon.firingType == Weapon.FiringType.Auto && m_IsInputAutoFire) &&
                m_SelectedWeapon.reloadingType == Weapon.ReloadingType.PartialRepeat && m_CurrentCapacity[m_CurrentWeaponIndex] > 0 // #
                )
            {
                if (m_ReloadWeaponCoroutine != null)
                {
                    StopCoroutine(m_ReloadWeaponCoroutine);
                }

                m_Animator.SetBool(m_SelectedWeapon.reloadAniamtionVarName, false);
                m_BurstRoundsCount = m_SelectedWeapon.roundsPerBurst;

                if (GetComponent<AudioSource>().clip == m_SelectedWeapon.reloadSound)
                {
                    GetComponent<AudioSource>().Stop();
                }

                m_CurrentPReloadInterruptionTime = m_SelectedWeapon.partialReloadInterruptionTime;
                m_IsReloading = false;
            }

            // Start reload coroutines
            if ((m_CurrentCapacity[m_CurrentWeaponIndex] == 0) && m_AmmoAmounts[m_SelectedWeapon.ammo.name] > 0 && !m_IsReloading && !m_IsSwitching && !m_IsRunning)
            {
                if (m_SelectedWeapon.reloadingType == Weapon.ReloadingType.PartialRepeat)
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

            //GetComponent<AudioSource>().clip = m_SelectedWeapon.reloadSound;
            //GetComponent<AudioSource>().Play();

            AudioUtility.CreateSFX(m_SelectedWeapon.reloadSound, transform.position, AudioUtility.AudioGroups.EnemyReload, 0f);

            m_Animator.SetBool(m_SelectedWeapon.reloadAniamtionVarName, true);

            // Reloading weapon
            yield return new WaitForSeconds(m_SelectedWeapon.reloadingTime);

            m_Animator.SetBool(m_SelectedWeapon.reloadAniamtionVarName, false);

            m_BurstRoundsCount = m_SelectedWeapon.roundsPerBurst;

            if (m_IsInfiniteAmmo)
            {
                m_CurrentCapacity[m_CurrentWeaponIndex] = m_SelectedWeapon.capacity;
            }
            else
            {
                // Will reload fill weapon capacity completely
                if (
                    m_SelectedWeapon.reloadingType == Weapon.ReloadingType.Full ||
                    (m_SelectedWeapon.ammoAddedPerReload > (m_SelectedWeapon.capacity - m_CurrentCapacity[m_CurrentWeaponIndex]))
                    )
                {
                    // Is there enough ammo for reload
                    if (m_AmmoAmounts[m_SelectedWeapon.ammo.name] < (m_SelectedWeapon.capacity - m_CurrentCapacity[m_CurrentWeaponIndex]))
                    {
                        // Reload weapon with remaining total ammo
                        m_CurrentCapacity[m_CurrentWeaponIndex] += m_AmmoAmounts[m_SelectedWeapon.ammo.name];
                        m_AmmoAmounts[m_SelectedWeapon.ammo.name] = 0;
                    }
                    else
                    {
                        // Reload weapon fully
                        m_AmmoAmounts[m_SelectedWeapon.ammo.name] -= (m_SelectedWeapon.capacity - m_CurrentCapacity[m_CurrentWeaponIndex]);
                        m_CurrentCapacity[m_CurrentWeaponIndex] = m_SelectedWeapon.capacity;
                    }
                }
                else
                {
                    // Is there enough ammo for reload
                    if (m_AmmoAmounts[m_SelectedWeapon.ammo.name] < m_SelectedWeapon.ammoAddedPerReload)
                    {
                        // Reload weapon with remaining total ammo
                        m_CurrentCapacity[m_CurrentWeaponIndex] += m_AmmoAmounts[m_SelectedWeapon.ammo.name];
                        m_AmmoAmounts[m_SelectedWeapon.ammo.name] = 0;
                    }
                    else
                    {
                        // Reload weapon with expected partial amount
                        m_AmmoAmounts[m_SelectedWeapon.ammo.name] -= m_SelectedWeapon.ammoAddedPerReload;
                        m_CurrentCapacity[m_CurrentWeaponIndex] += m_SelectedWeapon.ammoAddedPerReload;

                    }
                }
            }

            // Prevents firing when weapon is transitioning from reloading to idle animations
            if (m_SelectedWeapon.reloadingType == Weapon.ReloadingType.Partial)
            {
                m_CurrentPReloadInterruptionTime = m_SelectedWeapon.partialReloadInterruptionTime;
            }

            m_IsReloading = false;
        }

        IEnumerator WaitReloadWeaponRepeat()
        {
            m_IsReloading = true;

            GetComponent<AudioSource>().clip = m_SelectedWeapon.reloadSound;

            m_Animator.SetBool(m_SelectedWeapon.reloadAniamtionVarName, true);

            // Repeat reloading process
            for (int i = m_CurrentCapacity[m_CurrentWeaponIndex]; i < m_SelectedWeapon.capacity; i += m_SelectedWeapon.ammoAddedPerReload)
            {
                GetComponent<AudioSource>().Play();

                // Reloading weapon
                yield return new WaitForSeconds(m_SelectedWeapon.reloadingTime);

                if (m_IsInfiniteAmmo)
                {
                    m_CurrentCapacity[m_CurrentWeaponIndex] = m_SelectedWeapon.capacity;
                }
                else
                {
                    // Will reload fill weapon capacity completely
                    if (m_SelectedWeapon.ammoAddedPerReload > (m_SelectedWeapon.capacity - m_CurrentCapacity[m_CurrentWeaponIndex]))
                    {
                        // Is there enough ammo for reload
                        if (m_AmmoAmounts[m_SelectedWeapon.ammo.name] < (m_SelectedWeapon.capacity - m_CurrentCapacity[m_CurrentWeaponIndex]))
                        {
                            // Reload weapon with remaining total ammo
                            m_CurrentCapacity[m_CurrentWeaponIndex] += m_AmmoAmounts[m_SelectedWeapon.ammo.name];
                            m_AmmoAmounts[m_SelectedWeapon.ammo.name] = 0;
                        }
                        else
                        {
                            // Reload weapon fully
                            m_AmmoAmounts[m_SelectedWeapon.ammo.name] -= (m_SelectedWeapon.capacity - m_CurrentCapacity[m_CurrentWeaponIndex]);
                            m_CurrentCapacity[m_CurrentWeaponIndex] = m_SelectedWeapon.capacity;
                        }
                    }
                    else
                    {
                        // Is there enough ammo for reload
                        if (m_AmmoAmounts[m_SelectedWeapon.ammo.name] < m_SelectedWeapon.ammoAddedPerReload)
                        {
                            // Reload weapon with remaining total ammo
                            m_CurrentCapacity[m_CurrentWeaponIndex] += m_AmmoAmounts[m_SelectedWeapon.ammo.name];
                            m_AmmoAmounts[m_SelectedWeapon.ammo.name] = 0;
                        }
                        else
                        {
                            // Reload weapon with expected partial amount
                            m_AmmoAmounts[m_SelectedWeapon.ammo.name] -= m_SelectedWeapon.ammoAddedPerReload;
                            m_CurrentCapacity[m_CurrentWeaponIndex] += m_SelectedWeapon.ammoAddedPerReload;

                        }
                    }
                }

                // Ensures reloading stops when total ammo runs out
                if (m_AmmoAmounts[m_SelectedWeapon.ammo.name] <= 0)
                {
                    i = m_SelectedWeapon.capacity;
                }
            }

            m_Animator.SetBool(m_SelectedWeapon.reloadAniamtionVarName, false);

            m_BurstRoundsCount = m_SelectedWeapon.roundsPerBurst;

            // Prevents firing when weapon is transitioning from reloading to idle animations
            m_CurrentPReloadInterruptionTime = m_SelectedWeapon.partialReloadInterruptionTime;

            m_IsReloading = false;
        }


        void LoadSelectedWeapon()
        {
            // Remove old weapon instance
            if (m_InstantiatedWeaponObject != null)
            {
                Destroy(m_InstantiatedWeaponObject);
            }

            // Spawn new weapon instance
            if (m_SelectedWeapon.weaponPrefab != null)
            {
                m_GunSpawn = GameObject.FindGameObjectsWithTag(m_AITag)[0].transform.Find("WeaponRoot").transform;


                m_InstantiatedWeaponObject = Instantiate(m_SelectedWeapon.weaponPrefab, 
                    GameObject.FindGameObjectsWithTag(m_AITag)[0].transform.Find("WeaponRoot").transform);

                // Removes "(clone)" from name of spawned weapon object, to prevent animations from disconnecting with object
                const int numberOfCharacterToRemove = 7;

                m_InstantiatedWeaponObject.gameObject.name = m_InstantiatedWeaponObject.gameObject.name.Remove(m_InstantiatedWeaponObject.gameObject.name.Length - numberOfCharacterToRemove);
            }

            // Find points for weapon functionality
            try
            {
                m_BarrelFlashSpawn = GameObject.FindGameObjectsWithTag(m_AITag)[0].transform.Find("WeaponRoot/" + m_SelectedWeapon.barrelFlashSpawnName).transform;
            }
            catch
            {
                throw new System.Exception("Cannot find Barrel Flash Spawn object, ensure Barrel Flash Spawn Name field matches object's name.");
            }

            try
            {
                m_ProjectileSpawn = GameObject.FindGameObjectsWithTag(m_AITag)[0].transform.Find("WeaponRoot/" + m_SelectedWeapon.projectileSpawnName).transform;
            }
            catch
            {
                // ?    Change text when field name changes
                throw new System.Exception("Cannot find Projectile Spawn object, ensure Projectile Spawn Name field matches object's name.");
            }

            try
            {
                m_EjectedCartridgeSpawn = GameObject.FindGameObjectsWithTag(m_AITag)[0].transform.Find("WeaponRoot/" + m_SelectedWeapon.cartridgeSpawnName).transform;
            }
            catch
            {
                // ?    Change text when field name changes
                throw new System.Exception("Cannot find Cartridge Spawn object, ensure Cartridge Spawn Name field matches object's name.");
            }


            m_BarrelFlashSpawn.GetComponent<AudioSource>().clip = m_SelectedWeapon.barrelSound;
            m_BurstRoundsCount = m_SelectedWeapon.roundsPerBurst;

            // Prevents animation disconnection issue (if first weapon controller is pre-applied to animator)
            m_Animator.runtimeAnimatorController = null;

            m_Animator.runtimeAnimatorController = m_SelectedWeapon.animatorController;
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

        public void EnableGrenade()
        {
            m_CanThrow = true;
            if (!m_IsInfiniteGrenade)
            {
                totalAmount += startAmount;
            }
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
                    }
                    else if (!state && m_IsWeaponEnabled[i])
                    {
                        m_IsWeaponEnabled[i] = false;
                    }

                    i = m_WeaponsLength;
                }
            }
        }

        void CheckWeaponSwitch()
        {
            m_LastSelectedWeapon = m_CurrentWeaponIndex;
            m_CurrentWeaponIndex++;

            if (m_CurrentWeaponIndex >= m_WeaponsLength)
            {
                m_CurrentWeaponIndex = 0;
            }

            // Find next enabled weapon
            while (!m_IsWeaponEnabled[m_CurrentWeaponIndex])
            {
                m_CurrentWeaponIndex++;

                if (m_CurrentWeaponIndex >= m_WeaponsLength)
                {
                    m_CurrentWeaponIndex = 0;
                }
            }

            m_IsWeaponEnabled[m_LastSelectedWeapon] = false;
            m_SelectedWeapon = m_WeaponCollection.weapons[m_CurrentWeaponIndex];

            // Abort if no other weapon if found
            if (m_LastSelectedWeapon == m_CurrentWeaponIndex)
            {
                m_IsWeaponEnabled[m_LastSelectedWeapon] = true;
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

            AudioUtility.CreateSFX(m_WeaponCollection.weapons[m_LastSelectedWeapon].switchOutSound, transform.position, AudioUtility.AudioGroups.EnemyWeaponSwitch, 1f);

            m_Animator.SetBool(m_SelectedWeapon.switchAnimationVarName, true);

            // Switching out last weapon
            yield return new WaitForSeconds(m_SelectedWeapon.switchingTime);

            AudioUtility.CreateSFX(m_SelectedWeapon.switchInSound, transform.position, AudioUtility.AudioGroups.EnemyWeaponSwitch, 1f);

            m_Animator.SetBool(m_SelectedWeapon.switchAnimationVarName, false);

            LoadSelectedWeapon();

            // Switching in next weapon
            yield return new WaitForSeconds(m_SelectedWeapon.switchingTime);

            m_IsSwitching = false;
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

        bool IsGrenadeCollectable()
        {
            if (!m_IsInfiniteGrenade ||
                (m_CanThrow && !m_IsInfiniteGrenade) ||
                (!m_CanThrow && m_IsInfiniteGrenade))
            {
                return true;
            }

            return false;
        }

        // Collecting
        //void OnTriggerEnter(Collider other)
        //{
        //    if (other.transform.gameObject.layer == 11 && other.tag == "Weapon")
        //    {
        //        EnemyCollectWeapon(other, other.transform.GetComponent<CollectableObject>());
        //    }
        //    else if (other.tag == "Grenade")
        //    {
        //        EnemyCollectGrenade(other, other.transform.GetComponent<CollectableObject>());
        //    }
        //}

        public void EnemyCollectWeapon(Collider other, CollectableObject collect)
        {
            m_Collect = collect;

            m_CollectedWeapon = m_Collect.m_Weapon;

            if (!isEnabled(m_CollectedWeapon))
            {
                if (m_Collect.m_CollectionType == CollectableObject.CollectionType.Weapon)
                {
                    EnableWeapon(m_CollectedWeapon, m_Collect.m_Enable, m_Collect.m_AmmoInWeapon);
                    IncreaseAmmoCount(m_CollectedWeapon.ammo, m_Collect.m_AddToAmmoTotal);
                }
                else if (m_Collect.m_CollectionType == CollectableObject.CollectionType.Ammo)
                {
                    IncreaseAmmoCount(m_Collect.m_Ammo, m_Collect.m_AddToAmmoTotal);
                }

                if (m_Collect.m_AfterCollectionObject != null)
                {
                    GameObject afterObjectInstance = Instantiate(m_Collect.m_AfterCollectionObject, m_Collect.transform.position, m_Collect.transform.rotation);
                    Destroy(afterObjectInstance, m_Collect.m_AfterCollectionDespawnTime);
                }

                m_Collect.m_Enable = false;
                Destroy(m_Collect.gameObject);
                CheckWeaponSwitch();
            }
        }

        public void EnemyCollectGrenade(Collider other, CollectableObject collect)
        {
            m_Collect = collect;

            if (IsGrenadeCollectable())
            {
                if (m_Collect.m_CollectionType == CollectableObject.CollectionType.Grenade)
                {
                    EnableGrenade();

                    if (m_Collect.m_AfterCollectionObject != null)
                    {
                        GameObject afterObjectInstance = Instantiate(m_Collect.m_AfterCollectionObject, m_Collect.transform.position, m_Collect.transform.rotation);
                        Destroy(afterObjectInstance, m_Collect.m_AfterCollectionDespawnTime);
                    }

                    Destroy(m_Collect.gameObject);
                }
            }
        }

        void GrenadeInit()
        {
            m_Cooldown = Random.Range(2, 10);

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

        void ThrowGrenade()
        {
            if (m_ThrowAudio != null)
            {
                GetComponent<AudioSource>().clip = m_ThrowAudio;
                GetComponent<AudioSource>().Play();
            }

            GameObject grenade = Instantiate(grenadePrefab, transform.position, transform.rotation);
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * throwForce, ForceMode.VelocityChange);
        }

        void CheckGrenade()
        {
            if (m_CanThrow)
            {
                if (m_Cooldown > 0f)
                {
                    m_Cooldown -= 1f * Time.deltaTime;
                }
                else
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
                        m_Cooldown = Random.Range(2, 10);
                    }
                }
            }
        }
    }
}
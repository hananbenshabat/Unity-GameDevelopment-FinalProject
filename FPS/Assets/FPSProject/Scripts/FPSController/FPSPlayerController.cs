using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.FPSController
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FPSPlayerController : MonoBehaviour
    {
        [SerializeField] Camera m_CharacterCamera;

        [Header("Movement Input Axes")]
        [SerializeField] string m_MovementXName;
        [SerializeField] string m_MovementYName;

        [Header("Collecting Key")]
        [SerializeField] KeyCode m_CollectingKey;

        [Header("Movement Input Keys")]
        [SerializeField] KeyCode m_JumpKey;
        [SerializeField] KeyCode m_RunKey;

        [Header("Mouse Input Axes")]
        [SerializeField] string m_MouseXName;
        [SerializeField] string m_MouseYName;

        [Header("Movement Speeds")]
        [SerializeField] float m_WalkSpeed = 5f;
        [SerializeField] float m_RunSpeed = 10f;

        [Header("Mouse Sensitivities")]
        [SerializeField] float m_MouseXSensitivity = 1f;
        [SerializeField] float m_MouseYSensitivity = 1f;

        [Header("Aero Mobility")]
        [SerializeField] float m_JumpForce = 10f;
        [SerializeField] float m_AeroMobilityMultiplier = 1f;
        [SerializeField] float m_GravityMultiplier = 2f;

        [Header("Camera Bobbing Effects")]
        [SerializeField] float m_WalkCycleRate = 2f;
        [SerializeField] bool m_IsCameraBobbingEnabled = true;
        [SerializeField] float m_CameraBobIntensityMoving = 0.05f;
        [SerializeField] float m_CameraBobIntensityLanding = 0.2f;

        [Header("Audio")]
        [SerializeField] AudioClip m_FirstFootstepAudio;
        [SerializeField] AudioClip m_SecondFootstepAudio;
        [SerializeField] AudioClip m_JumpAudio;
        [SerializeField] AudioClip m_LandAudio;

        CharacterController m_CharacterController;

        CollectableObject m_CollectableObject;

        AudioSource m_AudioSource;

        Coroutine m_LandBobCoroutine;

        Vector2 m_MouseDifference = Vector2.zero;

        Vector3 m_GroundMovementDirection, m_CameraLocalPositionStart, m_MovementDirection = Vector3.zero;

        bool m_WasGrounded = false, m_IsSinePeakPassed = false, m_IsSecondFootstepNext = false, m_IsCursorLocked = true;

        float m_CurrentWalkCycleTime = 0f;

        string weaponTag = "Weapon", grenadeTag = "Grenade";

        public bool IsDead { get; private set; }
        Health m_Health;

        void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_AudioSource = GetComponent<AudioSource>();
            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, FPSPlayerController>(m_Health, this, gameObject);
            m_Health.OnDie += OnDie;

            m_CameraLocalPositionStart = m_CharacterCamera.transform.localPosition;
        }

        void Update()
        {
            Movement();
            Rotation();
            WalkCycleTiming();
            collectWeapon();
        }

        void OnDie()
        {
            IsDead = true;

            ////TODO: Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
            //m_WeaponsManager.SwitchToWeaponIndex(-1, true);

            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void Movement()
        {
            // Movement across surface
            m_GroundMovementDirection = (transform.forward * Input.GetAxis(m_MovementYName)) + (transform.right * Input.GetAxis(m_MovementXName));

            RaycastHit hit;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hit, m_CharacterController.height / 2f);

            m_GroundMovementDirection = Vector3.ProjectOnPlane(m_GroundMovementDirection, hit.normal).normalized;

            float speedMultiplyer = Input.GetKey(m_RunKey) ? m_RunSpeed : m_WalkSpeed;
            speedMultiplyer *= m_CharacterController.isGrounded ? 1f : m_AeroMobilityMultiplier;

            m_MovementDirection.x = m_GroundMovementDirection.x * speedMultiplyer;
            m_MovementDirection.z = m_GroundMovementDirection.z * speedMultiplyer;


            // Landing
            if (m_CharacterController.isGrounded && !m_WasGrounded)
            {
                m_AudioSource.clip = m_LandAudio;
                m_AudioSource.Play();

                m_LandBobCoroutine = StartCoroutine(LandBobOverTime());

                m_WasGrounded = true;
            }

            // Jumping & airborne
            if (m_CharacterController.isGrounded)
            {
                m_MovementDirection.y = -10f;

                if (Input.GetKey(m_JumpKey))
                {
                    m_MovementDirection.y = m_JumpForce;

                    m_AudioSource.clip = m_JumpAudio;
                    m_AudioSource.Play();
                }
            }
            else
            {
                m_MovementDirection += Physics.gravity * m_GravityMultiplier * Time.deltaTime;
                m_WasGrounded = false;
            }

            // Prevent landing bob if moving across surface
            if (m_GroundMovementDirection.magnitude > 0f)
            {
                if (m_LandBobCoroutine != null)
                {
                    StopCoroutine(m_LandBobCoroutine);
                }
            }

            // Apply movement to character controller
            m_CharacterController.Move(m_MovementDirection * Time.deltaTime);
        }

        void Rotation()
        {
            // Cursor locking input
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                m_IsCursorLocked = false;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_IsCursorLocked = true;
            }

            // Cursor locking
            if (m_IsCursorLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                m_MouseDifference = new Vector2(Input.GetAxis(m_MouseXName), Input.GetAxis(m_MouseYName));
            }
            else
            {
                m_MouseDifference = Vector2.zero;
            }

            m_MouseDifference.x *= m_MouseXSensitivity;
            m_MouseDifference.y *= m_MouseYSensitivity;

            // Vertical camera rotation
            Quaternion xAxisRotation = Quaternion.AngleAxis(m_MouseDifference.y, -Vector3.right);

            Quaternion cameraRotation = m_CharacterCamera.transform.localRotation;
            cameraRotation *= xAxisRotation;

            cameraRotation.x = Mathf.Clamp(cameraRotation.x, -0.707f, 0.707f);
            cameraRotation.w = Mathf.Clamp(cameraRotation.w, 0.707f, 2f);

            m_CharacterCamera.transform.localRotation = cameraRotation;

            // Horizontal character rotation
            Quaternion yAxisRotation = Quaternion.AngleAxis(m_MouseDifference.x, Vector3.up);

            Quaternion characterRotation = m_CharacterController.transform.localRotation;
            characterRotation *= yAxisRotation;

            m_CharacterController.transform.localRotation = characterRotation;
        }

        IEnumerator LandBobOverTime()
        {
            float currentTime = 0f;
            const float endTime = 0.4f;

            float currentSine = 0f;

            while (currentTime < endTime)
            {
                currentTime += Time.deltaTime;

                currentSine = Mathf.Sin(currentTime);

                if (m_IsCameraBobbingEnabled)
                {
                    m_CharacterCamera.transform.localPosition = m_CameraLocalPositionStart + new Vector3(0f, currentSine * m_CameraBobIntensityLanding, 0f);
                }

                yield return null;
            }
        }

        void WalkCycleTiming()
        {
            // Reture if not moving across surface
            if (m_GroundMovementDirection.magnitude <= 0f)
            {
                return;
            }

            // Reture if airborne
            if (!m_CharacterController.isGrounded)
            {
                return;
            }

            const float pi = Mathf.PI;
            float walkCycleRate = Input.GetKey(KeyCode.LeftShift) ? (m_WalkCycleRate * (m_RunSpeed / m_WalkSpeed)) : m_WalkCycleRate;

            m_CurrentWalkCycleTime += Time.deltaTime * pi * 2f * walkCycleRate;

            // Reset cycle
            if (m_CurrentWalkCycleTime > pi * 2f)
            {
                m_CurrentWalkCycleTime -= (pi * 2f);
            }

            // Determine if peak has passed & prepare for footstep
            if (m_CurrentWalkCycleTime > pi * 0.5f && m_CurrentWalkCycleTime <= pi * 1.5f)
            {
                m_IsSinePeakPassed = true;
            }

            float currentSine = Mathf.Sin(m_CurrentWalkCycleTime);

            // Bob camera while moving across surface
            if (m_IsCameraBobbingEnabled)
            {
                m_CharacterCamera.transform.localPosition = m_CameraLocalPositionStart + new Vector3(0f, currentSine * m_CameraBobIntensityMoving, 0f);
            }

            // Play footstep
            if (m_IsSinePeakPassed && !(m_CurrentWalkCycleTime > pi * 0.5f && m_CurrentWalkCycleTime <= pi * 1.5f))
            {
                m_AudioSource.clip = m_IsSecondFootstepNext ? m_SecondFootstepAudio : m_FirstFootstepAudio;
                m_AudioSource.Play();

                m_IsSinePeakPassed = false;
                m_IsSecondFootstepNext = !m_IsSecondFootstepNext;
            }
        }

        void collectWeapon()
        {
            if (Input.GetKey(m_CollectingKey))
            {
                RaycastHit hit;

                Ray ray = new Ray(m_CharacterCamera.transform.position, m_CharacterCamera.transform.forward);

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.CompareTag(weaponTag))
                    {
                        // Clicked weapon
                        m_CollectableObject = hit.transform.gameObject.GetComponent<CollectableObject>();
                        m_CollectableObject.WeaponPickup(this);
                    }
                    else if (hit.transform.CompareTag(grenadeTag))
                    {
                        // Clicked grenade
                        m_CollectableObject = hit.transform.gameObject.GetComponent<CollectableObject>();
                        m_CollectableObject.GrenadePickup(this);
                    }
                }
            }
        }
    }
}
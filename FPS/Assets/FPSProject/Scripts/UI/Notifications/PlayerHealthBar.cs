using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using Unity.FPS.FPSController;

namespace Unity.FPS.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        [Tooltip("Image component displaying current health")]
        public Image HealthFillImage;
        [Tooltip("Text component displaying current health")]
        public Text HealthFillText;

        Health m_PlayerHealth;

        void Start()
        {
            FPSPlayerController playerCharacterController =
                GameObject.FindObjectOfType<FPSPlayerController>();
            DebugUtility.HandleErrorIfNullFindObject<FPSPlayerController, PlayerHealthBar>(
                playerCharacterController, this);

            m_PlayerHealth = playerCharacterController.GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerHealthBar>(m_PlayerHealth, this,
                playerCharacterController.gameObject);
        }

        void Update()
        {
            // update health bar value
            HealthFillImage.fillAmount = m_PlayerHealth.CurrentHealth / m_PlayerHealth.MaxHealth;
            // update current health bar's text value
            HealthFillText.text = m_PlayerHealth.CurrentHealth.ToString();
        }
    }
}
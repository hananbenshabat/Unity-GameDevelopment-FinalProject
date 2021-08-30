using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class MenuNavigation : MonoBehaviour
    {
        public string[] m_DisplayMessages;
        public Sprite[] m_BackgroundImages;
        public bool m_IsInfiniteAmmo, m_IsInfiniteGrenade;
        public GameObject m_InfiniteGrenadeToggler, m_InfiniteAmmoToggler;
        public Selectable m_DefaultSelection;

        int m_GameStatus = -1;
        string m_CurrentDisplayMessage;
        Sprite m_CurrentBackgroundImage;
        Settings settings;

        void Start()
        {
            ApplyGameStatusToMenu();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EventSystem.current.SetSelectedGameObject(null);
        }

        void LateUpdate()
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                if (Input.GetButtonDown(GameConstants.k_ButtonNameSubmit)
                    || Input.GetAxisRaw(GameConstants.k_AxisNameHorizontal) != 0
                    || Input.GetAxisRaw(GameConstants.k_AxisNameVertical) != 0)
                {
                    EventSystem.current.SetSelectedGameObject(m_DefaultSelection.gameObject);
                }
            }
        }
        public void InfiniteGrenadeToggle(bool toggleValue)
        {
            if (toggleValue == true)
            {
                m_IsInfiniteGrenade = true;
            }
            else
            {
                m_IsInfiniteGrenade = false;
            }

            ApplyGrenadeSettings(m_IsInfiniteGrenade);
        }

        public void InfiniteAmmoToggle(bool toggleValue)
        {
            if (toggleValue == true)
            {
                m_IsInfiniteAmmo = true;
            }
            else
            {
                m_IsInfiniteAmmo = false;
            }

            ApplyAmmoSettings(m_IsInfiniteAmmo);
        }

        public void ApplySettings(bool InfiniteGrenadeStatus, bool InfiniteAmmoStatus)
        {
            ApplyGrenadeSettings(InfiniteGrenadeStatus);
            ApplyAmmoSettings(InfiniteAmmoStatus);
        }

        public void ApplyGrenadeSettings(bool InfiniteGrenadeStatus)
        {
            settings.m_IsInfiniteGrenade = InfiniteGrenadeStatus;
        }

        public void ApplyAmmoSettings(bool InfiniteAmmoStatus)
        {
            settings.m_IsInfiniteAmmo = InfiniteAmmoStatus;
        }

        public void ApplyGameStatusToMenu()
        {
            settings = GameObject.FindWithTag("Settings").GetComponent<Settings>();
            m_IsInfiniteGrenade = m_InfiniteGrenadeToggler.GetComponent<Toggle>().isOn;
            m_IsInfiniteAmmo = m_InfiniteAmmoToggler.GetComponent<Toggle>().isOn;

            ApplySettings(m_IsInfiniteGrenade, m_IsInfiniteAmmo);

            if (GameObject.FindWithTag("GameStatus") != null)
            {
                m_GameStatus = GameObject.FindWithTag("GameStatus").GetComponent<GameStatus>().Status;
            }

            // Intro by default
            if (m_GameStatus != -1)
            {
                m_CurrentDisplayMessage = m_DisplayMessages[m_GameStatus];
                m_CurrentBackgroundImage = m_BackgroundImages[m_GameStatus];

                // Pause
                if (m_GameStatus == 0)
                {
                    GameObject.FindWithTag("PlayButtonText").GetComponent<TMPro.TextMeshProUGUI>().text = "Continue";
                }

                // Defeat or Victory
                else if (m_GameStatus == 1 || m_GameStatus == 2)
                {
                    GameObject.FindWithTag("PlayButtonText").GetComponent<TMPro.TextMeshProUGUI>().text = "Play Again";
                }

                if (m_CurrentDisplayMessage != null)
                {
                    GameObject.FindWithTag("MenuMessage").GetComponent<TMPro.TextMeshProUGUI>().text = m_CurrentDisplayMessage;
                }

                if (m_CurrentBackgroundImage != null)
                {
                    GameObject.FindWithTag("MenuImage").GetComponent<Image>().sprite = m_CurrentBackgroundImage;
                }
            }
        }
    }
}
/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-30
 * BUILD_TIME: 00:00
 * DESCRIPTION: Handles the main menu button flow.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Exponentia.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string characterSelectionSceneName = "CharacterSelection";

        [Header("Optional Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Unavailable Buttons")]
        [SerializeField] private bool disableOptionsButton = true;
        [SerializeField] private bool disableCreditsButton = true;
        [SerializeField] private bool bindAssignedButtonsOnAwake = true;

        private void Awake()
        {
            if (bindAssignedButtonsOnAwake)
            {
                BindAssignedButtons();
            }

            ApplyUnavailableButtonState();
        }

        private void OnDestroy()
        {
            if (!bindAssignedButtonsOnAwake)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveListener(OpenOptions);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(OpenCredits);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }
        }

        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(characterSelectionSceneName))
            {
                Debug.LogError("MainMenuController: Character selection scene name is empty.");
                return;
            }

            SceneManager.LoadScene(characterSelectionSceneName);
        }

        public void OpenOptions()
        {
            Debug.Log("MainMenuController: Options menu is not implemented yet.");
        }

        public void OpenCredits()
        {
            Debug.Log("MainMenuController: Credits menu is not implemented yet.");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BindAssignedButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
                startButton.onClick.AddListener(StartGame);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveListener(OpenOptions);
                optionsButton.onClick.AddListener(OpenOptions);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(OpenCredits);
                creditsButton.onClick.AddListener(OpenCredits);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void ApplyUnavailableButtonState()
        {
            if (optionsButton != null && disableOptionsButton)
            {
                optionsButton.interactable = false;
            }

            if (creditsButton != null && disableCreditsButton)
            {
                creditsButton.interactable = false;
            }
        }
    }
}

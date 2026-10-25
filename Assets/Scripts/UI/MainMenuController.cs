using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        public event Action StartRequested;
        public event Action ContinueRequested;

        void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(() => StartRequested?.Invoke());
            if (continueButton != null) continueButton.onClick.AddListener(() => ContinueRequested?.Invoke());
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        public void Open()
        {
            if (continueButton != null) continueButton.gameObject.SetActive(GameSession.HasSave());
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

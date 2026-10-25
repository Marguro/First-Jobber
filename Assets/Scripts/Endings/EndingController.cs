using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>One reusable ending screen: background, dialogue, final text, restart / menu buttons.</summary>
    public class EndingController : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text finalText;
        [SerializeField] GameObject buttonsRoot;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;
        [SerializeField] AudioSource audioSource;

        public event Action RestartRequested;
        public event Action MenuRequested;

        bool clicked;

        void Awake()
        {
            if (restartButton != null) restartButton.onClick.AddListener(() => Press(RestartRequested));
            if (menuButton != null) menuButton.onClick.AddListener(() => Press(MenuRequested));
        }

        void Press(Action action)
        {
            if (clicked) return;
            clicked = true;
            action?.Invoke();
        }

        public void Show(GameContext ctx, EndingData ending)
        {
            clicked = false;
            if (background != null)
            {
                background.sprite = ending.background;
                background.color = ending.background != null ? Color.white : ending.backgroundColor;
            }
            if (titleText != null) titleText.text = ending.title;
            if (finalText != null) finalText.text = "";
            if (buttonsRoot != null) buttonsRoot.SetActive(false);
            if (audioSource != null && ending.sound != null) audioSource.PlayOneShot(ending.sound);

            ctx.Dialogue.Play(ending.lines, () =>
            {
                if (finalText != null) finalText.text = ending.finalText;
                if (buttonsRoot != null) buttonsRoot.SetActive(true);
            });
        }
    }
}

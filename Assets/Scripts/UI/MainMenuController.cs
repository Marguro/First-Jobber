using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>
    /// Main menu drawn as a large resume: type a name and an age, press Submit.
    /// Submitting zooms the paper out into the Resume Editor (see ResumeEditorController.Intro Start From).
    /// Continue / Quit are optional: leave their references empty to hide the feature.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Resume paper")]
        [Tooltip("The paper. The Resume Editor's intro zooms out from this rectangle.")]
        [SerializeField] RectTransform paperRect;
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_InputField ageInput;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button submitButton;
        [Header("Optional")]
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        public event Action<string, int> SubmitRequested;
        public event Action ContinueRequested;

        public RectTransform PaperRect => paperRect;

        GameDatabase db;
        bool submitted;

        void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(TrySubmit);
            if (ageInput != null)
            {
                ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                ageInput.onSubmit.AddListener(_ => TrySubmit()); // Enter in the age field submits
            }
            if (continueButton != null) continueButton.onClick.AddListener(() => ContinueRequested?.Invoke());
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        public void Open(GameDatabase database)
        {
            db = database;
            submitted = false;
            if (nameInput != null) nameInput.text = "";
            if (ageInput != null) ageInput.text = "";
            if (errorText != null) errorText.text = "";
            if (submitButton != null) submitButton.interactable = true;
            if (continueButton != null) continueButton.gameObject.SetActive(GameSession.HasSave());
        }

        void TrySubmit()
        {
            if (submitted || db == null) return;
            if (!CreateApplicantController.Validate(db, nameInput != null ? nameInput.text : "", ageInput != null ? ageInput.text : "",
                    out string playerName, out int age, out string error))
            {
                if (errorText != null) errorText.text = error;
                return;
            }
            submitted = true;
            if (submitButton != null) submitButton.interactable = false;
            if (errorText != null) errorText.text = "";
            SubmitRequested?.Invoke(playerName, age);
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

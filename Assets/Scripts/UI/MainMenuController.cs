using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>
    /// Main menu drawn as a large resume: type a name, pick a gender, press Submit.
    /// Submitting zooms the paper out into the Resume Editor (see ResumeEditorController.Intro Start From).
    /// Continue / Quit are optional: leave their references empty to hide the feature.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Resume paper")]
        [Tooltip("The paper. The Resume Editor's intro zooms out from this rectangle.")]
        [SerializeField] RectTransform paperRect;
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_Dropdown genderDropdown;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button submitButton;
        [Header("Optional")]
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        /// <summary>(name, gender id)</summary>
        public event Action<string, string> SubmitRequested;
        public event Action ContinueRequested;

        public RectTransform PaperRect => paperRect;

        GameDatabase db;
        bool submitted;

        void OnEnable() { LocalizationManager.LanguageChanged += OnLanguageChanged; }
        void OnDisable() { LocalizationManager.LanguageChanged -= OnLanguageChanged; }

        // Rebuild the gender captions in the new language but keep what the player already picked.
        void OnLanguageChanged()
        {
            if (db == null || genderDropdown == null) return;
            int picked = genderDropdown.value;
            CreateApplicantController.PopulateGenderDropdown(genderDropdown, db);
            genderDropdown.SetValueWithoutNotify(Mathf.Clamp(picked, 0, Mathf.Max(0, genderDropdown.options.Count - 1)));
            genderDropdown.RefreshShownValue();
        }

        void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(TrySubmit);
            if (continueButton != null) continueButton.onClick.AddListener(() => ContinueRequested?.Invoke());
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        public void Open(GameDatabase database)
        {
            db = database;
            if (db == null || nameInput == null || genderDropdown == null || submitButton == null)
                Debug.LogError("[MainMenu] Applicant form requires database, NameInput, GenderDropdown and SubmitButton references.", this);
            submitted = false;
            if (nameInput != null) nameInput.text = "";
            CreateApplicantController.PopulateGenderDropdown(genderDropdown, db);
            if (errorText != null) errorText.text = "";
            if (submitButton != null) submitButton.interactable = true;
            if (continueButton != null) continueButton.gameObject.SetActive(GameSession.HasSave());
            Debug.Log("[MainMenu] Applicant form initialized; gender options=" + (genderDropdown != null ? genderDropdown.options.Count : 0), this);
        }

        void TrySubmit()
        {
            Debug.Log("[Applicant] Submit clicked.", this);
            if (submitted) return;
            if (db == null) { Debug.LogError("[Applicant] Main Menu has not received its GameDatabase.", this); return; }
            if (!CreateApplicantController.Validate(db, nameInput != null ? nameInput.text : "", genderDropdown,
                    out string playerName, out string genderId, out string error))
            {
                Debug.LogWarning("[Applicant] Validation failed: " + error, this);
                if (errorText != null) errorText.text = error;
                return;
            }
            submitted = true;
            Debug.Log("[Applicant] Name valid; gender ID=" + genderId + ". [MainMenu] New Game requested.", this);
            if (submitButton != null) submitButton.interactable = false;
            if (errorText != null) errorText.text = "";
            SubmitRequested?.Invoke(playerName, genderId);
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

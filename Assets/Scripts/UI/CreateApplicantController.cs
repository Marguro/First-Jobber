using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public class CreateApplicantController : MonoBehaviour
    {
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_Dropdown genderDropdown;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button submitButton;

        /// <summary>(name, gender id)</summary>
        public event Action<string, string> Submitted;

        GameDatabase db;
        bool submitted;

        void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        }

        public void Open(GameDatabase database)
        {
            db = database;
            submitted = false;
            if (nameInput != null) nameInput.text = "";
            PopulateGenderDropdown(genderDropdown, db);
            if (errorText != null) errorText.text = "";
            if (submitButton != null) submitButton.interactable = true;
        }

        /// <summary>Fills a dropdown from GameDatabase.genders. Entry 0 is the prompt and cannot be submitted.</summary>
        public static void PopulateGenderDropdown(TMP_Dropdown dropdown, GameDatabase db)
        {
            if (dropdown == null || db == null) return;
            dropdown.ClearOptions();
            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData(string.IsNullOrEmpty(db.genderPrompt) ? "Select gender..." : db.genderPrompt)
            };
            foreach (var g in db.genders)
                if (g != null && !string.IsNullOrWhiteSpace(g.id)) options.Add(new TMP_Dropdown.OptionData(g.displayName));
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
        }

        /// <summary>
        /// Shared name / gender rules (also used by the main menu resume). A scene without a gender dropdown
        /// (not yet converted) still plays: the gender is then 'unspecified'.
        /// </summary>
        public static bool Validate(GameDatabase db, string nameText, TMP_Dropdown dropdown, out string playerName, out string genderId, out string error)
        {
            playerName = (nameText ?? "").Trim();
            genderId = Genders.Unspecified;
            error = null;
            if (string.IsNullOrEmpty(playerName)) { error = "Please enter a name."; return false; }
            if (playerName.Length > db.maxNameLength) { error = "Name is too long (max " + db.maxNameLength + ")."; return false; }

            if (dropdown == null)
            {
                Debug.LogWarning("[Applicant] No gender dropdown assigned - using '" + Genders.Unspecified + "'. Run Tools > First Jobber > 9. Convert Main Menu To Resume Style.");
                return true;
            }

            // Dropdown index 0 is the prompt; options i>=1 map to the i-th VALID database gender.
            int wanted = dropdown.value;
            int seen = 0;
            foreach (var g in db.genders)
            {
                if (g == null || string.IsNullOrWhiteSpace(g.id)) continue;
                seen++;
                if (seen == wanted) { genderId = g.id; return true; }
            }
            error = "Please select a gender.";
            return false;
        }

        void OnSubmit()
        {
            if (submitted || db == null) return;
            if (!Validate(db, nameInput != null ? nameInput.text : "", genderDropdown,
                    out string playerName, out string genderId, out string error))
            {
                ShowError(error);
                return;
            }

            submitted = true;
            if (submitButton != null) submitButton.interactable = false;
            if (errorText != null) errorText.text = "";
            Submitted?.Invoke(playerName, genderId);
        }

        void ShowError(string message)
        {
            if (errorText != null) errorText.text = message;
        }
    }
}

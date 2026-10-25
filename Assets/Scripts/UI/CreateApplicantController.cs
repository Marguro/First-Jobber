using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public class CreateApplicantController : MonoBehaviour
    {
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_InputField ageInput;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button submitButton;

        public event Action<string, int> Submitted;

        GameDatabase db;
        bool submitted;

        void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
            if (ageInput != null) ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        public void Open(GameDatabase database)
        {
            db = database;
            submitted = false;
            if (nameInput != null) nameInput.text = "";
            if (ageInput != null) ageInput.text = "";
            if (errorText != null) errorText.text = "";
            if (submitButton != null) submitButton.interactable = true;
        }

        /// <summary>Shared name / age rules (also used by the main menu resume).</summary>
        public static bool Validate(GameDatabase db, string nameText, string ageText, out string playerName, out int age, out string error)
        {
            playerName = (nameText ?? "").Trim();
            age = 0;
            error = null;
            if (string.IsNullOrEmpty(playerName)) { error = UiText.NameRequired.Text; return false; }
            if (playerName.Length > db.maxNameLength) { error = UiText.NameTooLong.Format(db.maxNameLength); return false; }
            if (!int.TryParse((ageText ?? "").Trim(), out age) || age < db.minAge || age > db.maxAge)
            {
                error = UiText.AgeRange.Format(db.minAge, db.maxAge);
                return false;
            }
            return true;
        }

        void OnSubmit()
        {
            if (submitted || db == null) return;
            if (!Validate(db, nameInput != null ? nameInput.text : "", ageInput != null ? ageInput.text : "",
                    out string playerName, out int age, out string error))
            {
                ShowError(error);
                return;
            }

            submitted = true;
            if (submitButton != null) submitButton.interactable = false;
            if (errorText != null) errorText.text = "";
            Submitted?.Invoke(playerName, age);
        }

        void ShowError(string message)
        {
            if (errorText != null) errorText.text = message;
        }
    }

    /// <summary>Background + caption for OnDayStart daily events.</summary>
}

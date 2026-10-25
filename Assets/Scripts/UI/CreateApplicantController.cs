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

        void OnSubmit()
        {
            if (submitted || db == null) return;

            string playerName = nameInput != null ? nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(playerName)) { ShowError("Please enter a name."); return; }
            if (playerName.Length > db.maxNameLength) { ShowError("Name is too long (max " + db.maxNameLength + ")."); return; }

            int age = 0;
            if (ageInput == null || !int.TryParse(ageInput.text.Trim(), out age) || age < db.minAge || age > db.maxAge)
            {
                ShowError("Please enter an age between " + db.minAge + " and " + db.maxAge + ".");
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

using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>The paper resume: applicant header + one row per category (rows are clickable category selectors).</summary>
    public class ResumePreviewUI : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text ageText;
        [SerializeField] ChoiceListUI categoryList;

        public ChoiceListUI CategoryList => categoryList;

        string realName = "";
        string realAge = "";
        Coroutine distortRoutine;
        bool distorted;

        public void SetApplicant(string applicantName, int age)
        {
            realName = applicantName ?? "";
            realAge = "Age: " + age;
            if (!distorted)
            {
                if (nameText != null) nameText.text = realName;
                if (ageText != null) ageText.text = realAge;
            }
        }

        /// <summary>Horror effect (flag resume_distorted): the header text glitches now and then.</summary>
        public void SetDistorted(bool value)
        {
            if (value == distorted) return;
            distorted = value;
            if (distortRoutine != null) StopCoroutine(distortRoutine);
            distortRoutine = null;
            transform.localRotation = Quaternion.identity;
            if (value) distortRoutine = StartCoroutine(DistortRoutine());
            else
            {
                if (nameText != null) nameText.text = realName;
                if (ageText != null) ageText.text = realAge;
            }
        }

        IEnumerator DistortRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(0.6f, 1.8f));
                transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-1.5f, 1.5f));
                if (nameText != null) nameText.text = Scramble(realName);
                if (ageText != null) ageText.text = Scramble(realAge);
                yield return new WaitForSecondsRealtime(0.12f);
                transform.localRotation = Quaternion.identity;
                if (nameText != null) nameText.text = realName;
                if (ageText != null) ageText.text = realAge;
            }
        }

        static string Scramble(string source)
        {
            const string glyphs = "#@%&?*!";
            var sb = new StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
                sb.Append(source[i] != ' ' && Random.value < 0.5f ? glyphs[Random.Range(0, glyphs.Length)] : source[i]);
            return sb.ToString();
        }
    }
}

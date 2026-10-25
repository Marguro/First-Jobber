using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public class DailyEventPanelUI : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text captionText;

        public void Show(string caption, Sprite sprite)
        {
            if (captionText != null) captionText.text = caption;
            if (background != null)
            {
                background.sprite = sprite;
                background.color = sprite != null ? Color.white : new Color(0.04f, 0.04f, 0.06f);
            }
        }

        public void Clear() { Show("", null); }
    }
}

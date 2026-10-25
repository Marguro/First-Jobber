using UnityEngine;

namespace FirstJobber
{
    [CreateAssetMenu(menuName = "First Jobber/Ending", fileName = "Ending_New")]
    public class EndingData : ScriptableObject
    {
        public string id;
        public string title;

        [Header("Final decision button")]
        public string optionLabel;
        [TextArea] public string optionDescription;

        [Header("Ending scene")]
        public DialogueLine[] lines;
        public Sprite background;
        public Color backgroundColor = Color.black;
        [TextArea] public string finalText;
        public AudioClip sound;
    }
}

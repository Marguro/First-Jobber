using UnityEngine;

namespace FirstJobber
{
    [CreateAssetMenu(menuName = "First Jobber/Resume Category", fileName = "Category_New")]
    public class ResumeCategoryData : ScriptableObject
    {
        [Tooltip("Stable unique id. Never change after the game is released.")]
        public string id;
        public string displayName;
        [TextArea] public string emptyMessage = "Nothing is left to choose here.";
    }
}

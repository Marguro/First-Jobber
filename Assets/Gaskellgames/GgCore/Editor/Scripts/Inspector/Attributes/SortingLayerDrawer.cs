#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>
    [CustomPropertyDrawer(typeof(SortingLayerAttribute), true)]
    public class SortingLayerDrawer : GgPropertyDrawer
    {
        #region GgPropertyHeight
        
        protected override float GgPropertyHeight(SerializedProperty property, float propertyHeight, float approxFieldWidth)
        {
            return propertyHeight;
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region OnGgGUI
        
        protected override void OnGgGUI(Rect position, SerializedProperty property, GUIContent label, GgGUIDefaults defaultCache)
        {
            if (property.propertyType != SerializedPropertyType.Integer && property.propertyType != SerializedPropertyType.String)
            {
                GgGUI.CustomPropertyField(position, property, label);
                return;
            }
            
            GgGUI.SortingLayerField(position, label, property, EditorStyles.popup, EditorStyles.label);
        }
        
        #endregion
        
    } // class end
}

#endif
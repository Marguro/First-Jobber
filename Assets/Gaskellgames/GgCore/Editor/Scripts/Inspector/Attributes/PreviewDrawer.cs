#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gaskellgames.EditorOnly
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>

    [CustomPropertyDrawer(typeof(PreviewAttribute), true)]
    public class PreviewDrawer : GgPropertyDrawer
    {
        #region GgPropertyHeight
        
        protected override float GgPropertyHeight(SerializedProperty property, float propertyHeight, float approxFieldWidth)
        {
            return !IsValidType(property, out _)
                ? propertyHeight
                : Mathf.Max(propertyHeight, (singleLineHeight * AttributeAsType<PreviewAttribute>().lines) + standardSpacing);
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region OnGgGUI

        protected override void OnGgGUI(Rect position, SerializedProperty property, GUIContent label, GgGUIDefaults defaultCache)
        {
            if (!IsValidType(property, out Type objectType))
            {
                GgGUI.CustomPropertyField(position, property, label);
                return;
            }
            
            // get rects
            float size = singleLineHeight * AttributeAsType<PreviewAttribute>().lines;
            float indentAdjustedSize = size + Mathf.Max(0, currentIndent);
            Rect labelPosition = GetLabelPosition(position, label);
            Rect fieldPosition = GetFieldPosition(position, label);
            Rect previewRect = new Rect(fieldPosition.xMax - indentAdjustedSize, fieldPosition.yMax - size, indentAdjustedSize, size);
            
            // draw label
            if (0 < labelPosition.width)
            {
                GgGUI.PrefixLabelField(labelPosition, label, property.hasMultipleDifferentValues);
            }
            
            // draw preview
            if (GgGUI.ObjectField(previewRect, GUIContent.none, property.objectReferenceValue, out Object outputValue, objectType, property.hasMultipleDifferentValues, false))
            {
                property.objectReferenceValue = outputValue;
            }
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Private Methods
        
        private bool IsValidType(SerializedProperty property, out Type objectType)
        {
            objectType = typeof(Object);
            if (property.propertyType != SerializedPropertyType.ObjectReference) { return false; }
            
            if (property.type.Contains(nameof(Texture2D)))
            {
                objectType = typeof(Texture2D);
                return true;
            }
            if (property.type.Contains(nameof(Sprite)))
            {
                objectType = typeof(Sprite);
                return true;
            }
            
            return false;
        }
        
        #endregion
        
    } // class end
}

#endif
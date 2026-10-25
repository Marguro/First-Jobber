#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>

    [CustomPropertyDrawer(typeof(ListLengthAttribute), true)]
    public class ListLengthDrawer : GgPropertyDrawer
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
            if (TryGetListOrArrayProperty(property, out SerializedProperty listProperty))
            {
                int capacity = AttributeAsType<ListLengthAttribute>().capacity;
                bool forceLength = AttributeAsType<ListLengthAttribute>().forceLength;
                if ((listProperty.arraySize < capacity && forceLength) || capacity < listProperty.arraySize)
                {
                    listProperty.arraySize = capacity;
                }
            }
            
            GgGUI.CustomPropertyField(position, property, label);
        }

        #endregion

    } // class end
}

#endif
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code updated by Gaskellgames
    /// Original code from 'EditorCools': https://github.com/datsfain/EditorCools
    /// </summary>

    public class ButtonDrawer
    {
        private readonly List<IGrouping<string, InspectorButton>> buttonGroups;
        
        public ButtonDrawer(object target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] methods = target.GetType().GetMethods(flags);
            List<InspectorButton> buttons = new List<InspectorButton>();
            int rowNumber = 0;
            
            foreach (MethodInfo method in methods)
            {
                ButtonAttribute buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute == null) { continue; }
                buttons.Add(new InspectorButton(method, buttonAttribute));
            }
            
            buttonGroups = buttons.GroupBy(button =>
            {
                ButtonAttribute attribute = button.buttonAttribute;
                return attribute.Row == string.Empty
                    ? $"__{rowNumber++}"
                    : attribute.Row;
            }).ToList();
        }
        
        /// <summary>
        /// Draw all buttons on the target objects.
        /// </summary>
        /// <param name="targets"></param>
        public void DrawButtons(IEnumerable<object> targets)
        {
            foreach (IGrouping<string, InspectorButton> buttonGroup in buttonGroups)
            {
                int count = buttonGroup.Count();
                EditorGUILayout.BeginHorizontal();
                foreach (InspectorButton button in buttonGroup)
                {
                    button.Draw(targets);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
    } // class end
} 
#endif
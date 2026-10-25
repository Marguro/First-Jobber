using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code updated by Gaskellgames
    /// Original code from 'EditorCools': https://github.com/datsfain/EditorCools
    /// </summary>
    
    public class InspectorButton
    {
        private readonly string displayName;
        private readonly MethodInfo method;
        internal readonly ButtonAttribute buttonAttribute;
        
        public InspectorButton(MethodInfo method, ButtonAttribute buttonAttribute)
        {
            this.buttonAttribute = buttonAttribute;
            displayName = method.Name.NicifyName();
            this.method = method;
        }
        
        internal void Draw(IEnumerable<object> targets)
        {
            // set visibility
            bool defaultEnabled = GUI.enabled;
            switch (buttonAttribute.Availability)
            {
                case ButtonAvailability.Playmode:
                    GUI.enabled = Application.isPlaying;
                    break;
                
                case ButtonAvailability.EditMode:
                    GUI.enabled = !Application.isPlaying;
                    break;
                
                default:
                case ButtonAvailability.Always:
                    GUI.enabled = true;
                    break;
            }
            
            // handle button press
            if (GUILayout.Button(displayName))
            {
                foreach (object target in targets)
                {
                    method?.Invoke(target, null);
                }
            }
            
            // reset visibility
            GUI.enabled = defaultEnabled;
        }
        
    } // class end
}
using System;

namespace Gaskellgames
{
    /// <summary>
    /// Code updated by Gaskellgames
    /// Original code from 'EditorCools': https://github.com/datsfain/EditorCools
    /// </summary>

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : Attribute
    {
        private readonly string row;
        private readonly ButtonAvailability availability;
        
        public ButtonAttribute()
        {
            this.row = string.Empty;
            this.availability = ButtonAvailability.Always;
        }
        
        public ButtonAttribute(string row)
        {
            this.row = row;
            this.availability = ButtonAvailability.Always;
        }
        
        public ButtonAttribute(ButtonAvailability availability)
        {
            this.row = string.Empty;
            this.availability = availability;
        }
        
        public ButtonAttribute(string row, ButtonAvailability availability)
        {
            this.row = row;
            this.availability = availability;
        }
        
        /// <summary>
        /// Get the row this button should be grouped by.
        /// </summary>
        public string Row => row;
        
        /// <summary>
        /// Get the visibility of this button.
        /// </summary>
        public ButtonAvailability Availability => availability;
        
    } // class end
}
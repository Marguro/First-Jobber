using UnityEngine;
using Object = UnityEngine.Object;

namespace Gaskellgames
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>
    
    public static class GgLogs
    {
        /// <summary>
        /// Logs a message to the Unity Console.
        /// </summary>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="logType">Type of log to show in the console.</param>
        /// <param name="format">String format of the log to be shown.</param>
        /// <param name="args">Arguments to be injected to the string format.</param>
        public static void Log(Object context, GgLogType logType, string format, params object[] args)
        {
            if (!GaskellgamesSettings_SO.Instance) { return; }
            if (!GaskellgamesSettings_SO.Instance.ShowLogs) { return; }
            if (!TryGetUnityLogType(logType, out LogType unityLogType)) { return; }
            
            string message = GetPrefix() + format;
            Debug.unityLogger.LogFormat(unityLogType, context, message, args);
        }
        
        /// <summary>
        /// Logs a coloured message to the Unity Console.
        /// </summary>
        /// <param name="messageColor">Color of the message to display in the console.</param>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="logType">Type of log to show in the console.</param>
        /// <param name="format">String format of the log to be shown.</param>
        /// <param name="args">Arguments to be injected to the string format.</param>
        public static void Log(Color32 messageColor, Object context, GgLogType logType, string format, params object[] args)
        {
            if (Application.isEditor) { Log(context, logType, format, args); }
            
            if (!GaskellgamesSettings_SO.Instance) { return; }
            if (!GaskellgamesSettings_SO.Instance.ShowLogs) { return; }
            if (!TryGetUnityLogType(logType, out LogType unityLogType)) { return; }
            
            string message = GetPrefix() + StringExtensions.AddColorRichTextTag(format, messageColor);
            Debug.unityLogger.LogFormat(unityLogType, context, message, args);
        }
        
        private static bool TryGetUnityLogType(GgLogType logType, out LogType unityLogType)
        {
            unityLogType = LogType.Log;
            switch (logType)
            {
                case GgLogType.Debug:
                    if (!GaskellgamesSettings_SO.Instance.ShowDebugLogs) { return false; }
                    unityLogType = LogType.Log;
                    return true;
                
                case GgLogType.Info:
                    if (!GaskellgamesSettings_SO.Instance.ShowInfoLogs) { return false; }
                    unityLogType = LogType.Log;
                    return true;
                
                case GgLogType.Warning:
                    if (!GaskellgamesSettings_SO.Instance.ShowWarningLogs) { return false; }
                    unityLogType = LogType.Warning;
                    return true;
                
                case GgLogType.Error:
                    if (!GaskellgamesSettings_SO.Instance.ShowErrorLogs) { return false; }
                    unityLogType = LogType.Error;
                    return true;
                
                case GgLogType.Assert:
                    if (!GaskellgamesSettings_SO.Instance.ShowErrorLogs) { return false; }
                    unityLogType = LogType.Assert;
                    return true;
                
                case GgLogType.Exception:
                    if (!GaskellgamesSettings_SO.Instance.ShowErrorLogs) { return false; }
                    unityLogType = LogType.Exception;
                    return true;
                
                default:
                    return false;
            }
        }
        
        private static string GetPrefix()
        {
            return Application.isEditor
                ? "[" + StringExtensions.AddColorRichTextTag("Gaskellgames", new Color32(000, 179, 223, 255)) + "] "
                : "[Gaskellgames] ";
        }
        
    } // class end
}

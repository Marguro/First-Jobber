using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    public static class MenuTools
    {
        [MenuItem("Tools/First Jobber/9. Convert Main Menu To Resume Style")]
        public static void ConvertMainMenu()
        {
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            if (menu == null) { Debug.LogError("[Menu] No MainMenuController in the open scene."); return; }

            var panel = (RectTransform)menu.transform;
            if (!EditorUtility.DisplayDialog("Convert Main Menu",
                    "This deletes everything inside '" + panel.name + "' and rebuilds it as a resume paper with Name, Gender and Submit. Custom menu art will be lost. Continue?",
                    "Convert", "Cancel")) return;

            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Convert Main Menu");
            for (int i = panel.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(panel.GetChild(i).gameObject);

            GameSceneBuilder.PopulateMainMenu(panel, menu);

            var resume = Object.FindFirstObjectByType<ResumeEditorController>();
            if (resume != null)
            {
                var so = new SerializedObject(resume);
                var prop = so.FindProperty("introStartFrom");
                if (prop != null) { prop.objectReferenceValue = menu.PaperRect; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            Debug.Log("[Menu] Main menu converted. Resume Editor now zooms out from the menu paper. Save the scene (Ctrl+S).");
        }
    }
}

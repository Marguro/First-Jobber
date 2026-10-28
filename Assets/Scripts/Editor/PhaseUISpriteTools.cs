using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>Explicit source-path aliases, no fuzzy asset matching. Safe to rerun; preserves existing nonempty artist mappings.</summary>
    public static class PhaseUISpriteTools
    {
        const string Root="Assets/Sprites/UI/", LibraryPath="Assets/Data/Atmosphere/PhaseUISprites.asset";
        static readonly HashSet<string> Answers=new HashSet<string>{"Anw Bog_Blue_W","Anw Bog_Green_W","Anw Bog_Orange_W","Anw Bog_Pink_W","Anw Bog_Yello_W"};
        static readonly HashSet<string> Jobs=new HashSet<string>{"Job Bog","Job bog2","Job Bog3","Job Bog4","Job Bog5"};
        [MenuItem("Tools/First Jobber/Atmosphere/Configure Production Phase UI Sprites")]
        static void Configure()
        {
            if (Application.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/Game.unity")
            { Debug.LogWarning("[Phase UI] Open the Production Game scene outside Play Mode first."); return; }
            var report=new StringBuilder();
            var library=AssetDatabase.LoadAssetAtPath<PhaseUISpriteLibrary>(LibraryPath);
            if (library==null) { library=ScriptableObject.CreateInstance<PhaseUISpriteLibrary>(); AssetDatabase.CreateAsset(library,LibraryPath); }
            Undo.RecordObject(library,"Configure phase UI sprites");
            var seen=new HashSet<Sprite>();
            foreach (var entry in library.entries)
                if (entry!=null && entry.original!=null && !seen.Add(entry.original))
                { Debug.LogError("[Phase UI] Duplicate original sprite in the library; resolve it in Inspector before automatic configuration.",library); return; }
            if (library.phase2==null) library.phase2=AssetDatabase.LoadAssetAtPath<AtmospherePhaseData>("Assets/Data/Atmosphere/Phase_02_Deterioration.asset");
            if (library.phase3==null) library.phase3=AssetDatabase.LoadAssetAtPath<AtmospherePhaseData>("Assets/Data/Atmosphere/Phase_03_Despair.asset");
            if (library.answerPhase3==null) library.answerPhase3=MainSprite(Root+"Phase_3/Anw Bog_Phase_3.png",report);
            if (library.companyPhase3==null) library.companyPhase3=MainSprite(Root+"Phase_3/Job Bog_Phase_3.png",report);
            foreach (string path in Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories))
            {
                string sourcePath=path.Replace('\\','/');
                if (sourcePath.Contains("/Phase_2/") || sourcePath.Contains("/Phase_3/") || sourcePath.Contains("/Mockup/")) continue;
                var original=MainSprite(sourcePath,report); if (original==null) continue;
                string stem=Path.GetFileNameWithoutExtension(sourcePath), stem2=stem;
                if (stem=="Anw Bog_Green_W") stem2="Anw Bog_Green_";
                if (stem=="Anw Bog_Pink_W") stem2="Anw Bog_Pink_";
                if (stem=="Anw Bog_Yello_W") stem2="Anw Bog_Yellow";
                string p2=Root+"Phase_2/"+stem2+"_Phase_2.png", p3=Root+"Phase_3/"+stem+"_Phase_3.png";
                if (sourcePath.StartsWith(Root+"Answer Box/",StringComparison.Ordinal) && Answers.Contains(stem)) p3=Root+"Phase_3/Anw Bog_Phase_3.png";
                if (sourcePath.StartsWith(Root+"Work Bord/",StringComparison.Ordinal) && Jobs.Contains(stem)) p3=Root+"Phase_3/Job Bog_Phase_3.png";
                if (sourcePath==Root+"Background/BG_Base.png") p3=Root+"Phase_3/BG_Phase_3.png";
                if (sourcePath==Root+"Dialog Box/Dialog Box_Line.png") p3=Root+"Phase_3/Dialog Box_Phase_3.png";
                if (sourcePath==Root+"Dialog Box/Dialog Box_Line.png") p2=Root+"Phase_2/Dialog Box_Phase_2.png";
                if (sourcePath==Root+"Work Bord/Woek Head.png") p3=Root+"Phase_3/Woek Head__Phase_3.png";
                var second=MainSprite(p2,report); var third=MainSprite(p3,report);
                if (second==null && third==null) continue;
                var entry=library.entries.Find(e=>e!=null && e.original==original);
                if (entry==null) { entry=new PhaseUISpriteLibrary.Entry{original=original}; library.entries.Add(entry); }
                if (sourcePath.StartsWith(Root+"Answer Box/",StringComparison.Ordinal) && Answers.Contains(stem)) entry.family=PhaseUIFamily.AnswerBackground;
                if (sourcePath.StartsWith(Root+"Work Bord/",StringComparison.Ordinal) && Jobs.Contains(stem)) entry.family=PhaseUIFamily.CompanyBackground;
                Fill(ref entry.phase2Sprite,second,sourcePath+" Phase2",report); Fill(ref entry.phase3Sprite,third,sourcePath+" Phase3",report);
                if (second==null || third==null) report.AppendLine("Missing variant: "+sourcePath+" [P2="+(second!=null)+", P3="+(third!=null)+"]");
            }
            EditorUtility.SetDirty(library);
            var managers=UnityEngine.Object.FindObjectsByType<AtmosphereManager>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if (managers.Length!=1) { Debug.LogError("[Phase UI] Expected exactly one Production AtmosphereManager."); return; }
            var overlay=new SerializedObject(managers[0]).FindProperty("overlay").objectReferenceValue as Image;
            if (overlay==null) { Debug.LogError("[Phase UI] Assign the existing atmosphere overlay."); return; }
            var canvas=overlay.canvas.rootCanvas;
            if (overlay.transform.parent!=canvas.transform) { Debug.LogError("[Phase UI] Overlay is no longer a direct Canvas child. Review render order before configuring coverage."); return; }
            var beforeOverlay=new HashSet<Graphic>();
            foreach (Transform child in canvas.transform)
            { if (child==overlay.transform) break; foreach (var graphic in child.GetComponentsInChildren<Graphic>(true)) beforeOverlay.Add(graphic); }
            var prefabs=new HashSet<string>();
            foreach (var list in canvas.GetComponentsInChildren<ChoiceListUI>(true))
            {
                var prefab=new SerializedObject(list).FindProperty("itemPrefab").objectReferenceValue;
                if (prefab!=null) prefabs.Add(AssetDatabase.GetAssetPath(prefab));
            }
            foreach (var final in canvas.GetComponentsInChildren<FinalDecisionController>(true))
            {
                var prefab=new SerializedObject(final).FindProperty("actionButtonPrefab").objectReferenceValue;
                if (prefab!=null) prefabs.Add(AssetDatabase.GetAssetPath(prefab));
            }
            ConfigureRoot(canvas.gameObject,library,beforeOverlay,report);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            foreach (var path in prefabs)
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try { ConfigureRoot(root,library,new HashSet<Graphic>(root.GetComponentsInChildren<Graphic>(true)),report); PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/PhaseUISpriteEditorAudit.txt",report.ToString());
            Debug.Log("[Phase UI] Configured existing Production scene + "+prefabs.Count+" referenced prefabs. Scene changes support Undo and remain unsaved. Audit: Temp/PhaseUISpriteEditorAudit.txt\n"+report);
        }
        static void Fill(ref Sprite current, Sprite found,string label,StringBuilder report)
        { if (current==null) current=found; else if (found!=null && current!=found) report.AppendLine("Preserved artist mapping (different from automatic match): "+label); }
        static Sprite MainSprite(string path,StringBuilder report)
        {
            if (!File.Exists(path)) return null;
            Sprite best=null; float area=-1; bool tie=false;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite)
            { float size=sprite.rect.width*sprite.rect.height; if (size>area) {best=sprite;area=size;tie=false;} else if (size==area) tie=true; }
            if (tie || best==null) { report.AppendLine("Ambiguous/missing imported main sprite: "+path); return null; }
            return best;
        }
        static void ConfigureRoot(GameObject root,PhaseUISpriteLibrary library,HashSet<Graphic> coverage,StringBuilder report)
        {
            var excluded=new HashSet<Image>();
            foreach (var hr in root.GetComponentsInChildren<HRReactionController>(true))
            { var image=new SerializedObject(hr).FindProperty("portrait").objectReferenceValue as Image; if (image!=null) excluded.Add(image); }
            foreach (var dialogue in root.GetComponentsInChildren<DialogueUI>(true))
            { var image=new SerializedObject(dialogue).FindProperty("portraitImage").objectReferenceValue as Image; if (image!=null) excluded.Add(image); }
            var bindings=new Dictionary<Image,AtmosphereSpriteBinding>();
            foreach (var binding in root.GetComponentsInChildren<AtmosphereSpriteBinding>(true))
            {
                var target=new SerializedObject(binding).FindProperty("image").objectReferenceValue as Image;
                if (target==null) continue;
                if (bindings.ContainsKey(target)) { report.AppendLine("Conflicting existing bindings: "+target.name); continue; }
                bindings.Add(target,binding);
            }
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (excluded.Contains(image)) continue;
                bindings.TryGetValue(image,out var binding);
                var original=binding!=null ? new SerializedObject(binding).FindProperty("baseSprite").objectReferenceValue as Sprite : null;
                if (original==null) original=image.sprite;
                bool mapped=library.entries.Exists(e=>e!=null && e.original==original);
                if (!mapped)
                { if (original!=null && AssetDatabase.GetAssetPath(original).StartsWith(Root,StringComparison.Ordinal)) report.AppendLine("Existing atmosphere fallback (no variants): "+root.name+" / "+image.name+" / "+AssetDatabase.GetAssetPath(original)); continue; }
                if (binding==null) { binding=Undo.AddComponent<AtmosphereSpriteBinding>(image.gameObject); Set(binding,"image",image); SetBool(binding,"spriteOnly",true); SetBool(binding,"applyUiTint",false); bindings[image]=binding; }
                Undo.RecordObject(binding,"Configure UI phase binding"); binding.enabled=true;
                Set(binding,"uiSprites",library); Set(binding,"baseSprite",original);
                var mask=coverage.Contains(image) ? Mask(image,binding) : null;
                Set(binding,"overlayMask",mask);
                report.AppendLine("Mapped: "+root.name+" / "+image.name+" -> "+AssetDatabase.GetAssetPath(original));
            }
            // Coverage only: no portrait/video/wallpaper sprite mappings are introduced here.
            foreach (var graphic in coverage)
            {
                if (graphic.material!=null && !graphic.material.HasProperty("_Stencil")) report.AppendLine("Shader needs manual overlay-mask support: "+graphic.name+" / "+graphic.material.shader.name);
                AtmosphereSpriteBinding binding=null; if (graphic is Image image) bindings.TryGetValue(image,out binding); Mask(graphic,binding);
            }
            foreach (var button in root.GetComponentsInChildren<ChoiceButtonUI>(true))
            {
                var image=new SerializedObject(button).FindProperty("background").objectReferenceValue as Image;
                if (image!=null && bindings.TryGetValue(image,out var binding)) Set(button,"backgroundAtmosphere",binding);
            }
        }
        static AtmosphereOverlayMask Mask(Graphic graphic,AtmosphereSpriteBinding binding)
        {
            var mask=graphic.GetComponent<AtmosphereOverlayMask>(); if (mask==null) mask=Undo.AddComponent<AtmosphereOverlayMask>(graphic.gameObject);
            Set(mask,"binding",binding);
            bool animated=graphic is RawImage && graphic.material!=null && graphic.material.shader.name=="FirstJobber/UI/MotifWallpaper";
            SetBool(mask,"syncAnimatedMaterial",animated); return mask;
        }
        static void Set(UnityEngine.Object target,string key,UnityEngine.Object value)
        { var so=new SerializedObject(target); so.FindProperty(key).objectReferenceValue=value; so.ApplyModifiedProperties(); }
        static void SetBool(UnityEngine.Object target,string key,bool value)
        { var so=new SerializedObject(target); so.FindProperty(key).boolValue=value; so.ApplyModifiedProperties(); }
    }
    [UnityEditor.CustomEditor(typeof(AtmosphereSpriteBinding))]
    public class AtmosphereUIBindingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck(); DrawDefaultInspector(); bool changed=EditorGUI.EndChangeCheck();
            var binding=(AtmosphereSpriteBinding)target;
            var library=serializedObject.FindProperty("uiSprites").objectReferenceValue as PhaseUISpriteLibrary;
            var original=Application.isPlaying ? binding.BaseSprite : serializedObject.FindProperty("baseSprite").objectReferenceValue as Sprite;
            if (library!=null && original!=null)
            {
                var data=new SerializedObject(library);var entries=data.FindProperty("entries");
                for (int i=0;i<entries.arraySize;i++)
                {
                    var entry=entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("original").objectReferenceValue!=original) continue;
                    EditorGUILayout.LabelField("Shared mapping for this original",EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("phase2Sprite"),new GUIContent("Phase 2 Sprite"));
                    int family=entry.FindPropertyRelative("family").enumValueIndex;
                    EditorGUILayout.PropertyField(family==1 ? data.FindProperty("answerPhase3") : family==2 ? data.FindProperty("companyPhase3") : entry.FindPropertyRelative("phase3Sprite"),new GUIContent(family==0 ? "Phase 3 Sprite" : "Phase 3 Sprite (shared family)"));
                    if (EditorGUI.EndChangeCheck()) { data.ApplyModifiedProperties(); if (Application.isPlaying && AtmosphereManager.Instance!=null) AtmosphereManager.Instance.RefreshUIBindings(); }
                    break;
                }
            }
            if (!Application.isPlaying) return;
            if (changed) binding.RefreshAuthoredMapping();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current phase",AtmosphereManager.Instance!=null && AtmosphereManager.Instance.AppliedPhase!=null ? AtmosphereManager.Instance.AppliedPhase.id : "Normal / none");
                EditorGUILayout.Toggle("Dedicated phase sprite",binding.DedicatedSpriteActive);
                EditorGUILayout.Toggle("Atmosphere fallback",binding.AtmosphereFallbackActive);
            }
        }
    }
    [UnityEditor.CustomEditor(typeof(PhaseUISpriteLibrary))]
    public class PhaseUISpriteLibraryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck(); DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck() && Application.isPlaying && AtmosphereManager.Instance!=null) AtmosphereManager.Instance.RefreshUIBindings();
        }
    }
    [CustomPropertyDrawer(typeof(PhaseUISpriteLibrary.Entry))]
    public class PhaseUISpriteEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,GUIContent label)=>4*(EditorGUIUtility.singleLineHeight+2);
        public override void OnGUI(Rect position,SerializedProperty property,GUIContent label)
        {
            float height=EditorGUIUtility.singleLineHeight;
            var row=new Rect(position.x,position.y,position.width,height);
            EditorGUI.PropertyField(row,property.FindPropertyRelative("original"));row.y+=height+2;
            EditorGUI.PropertyField(row,property.FindPropertyRelative("phase2Sprite"));row.y+=height+2;
            var family=property.FindPropertyRelative("family");EditorGUI.PropertyField(row,family);row.y+=height+2;
            var third=family.enumValueIndex==1 ? property.serializedObject.FindProperty("answerPhase3") : family.enumValueIndex==2 ? property.serializedObject.FindProperty("companyPhase3") : property.FindPropertyRelative("phase3Sprite");
            EditorGUI.PropertyField(row,third,new GUIContent(family.enumValueIndex==0 ? "Phase 3 Sprite" : "Phase 3 (shared family)"));
        }
    }
}

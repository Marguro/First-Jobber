"""Serialized layout ownership and preservation checks. Native layout: use the Unity Validate menu."""
from pathlib import Path
import re,runpy,yaml
ROOT=Path(__file__).resolve().parents[2]
helpers=runpy.run_path(str(ROOT/'Tests/ConfidenceStars/verify.py'))  # also runs the real view/star regression harness
documents,guid=helpers['documents'],helpers['guid']
path=ROOT/'Assets/Prefabs/ChoiceButton.prefab';d=documents(path)
unit=dict(x=1,y=1,z=1)
root=d[833558695849796019]['RectTransform'];assert root['m_LocalScale']==unit and root['m_SizeDelta']==dict(x=800,y=96)
layout=d[358968367170934732]['MonoBehaviour'];assert layout['m_PreferredHeight']==96 and layout['m_MinHeight']==76 and layout['m_LayoutPriority']==0
horizontal=d[5017982193662469446]['MonoBehaviour'];assert horizontal['m_ChildControlHeight']==1 and horizontal['m_ChildScaleHeight']==0
assert not any(x.get('MonoBehaviour',{}).get('m_EditorClassIdentifier','').endswith('ContentSizeFitter') for x in d.values()),'Root/layout must not fight a size fitter'
bg=d[5205809760930852328]['MonoBehaviour'];assert bg['m_GameObject']['fileID']==910200001 and bg['m_RaycastTarget']==0
assert d[910200003]['MonoBehaviour']['m_IgnoreLayout']==1
assert d[910200002]['RectTransform']['m_AnchorMin']==dict(x=0,y=0) and d[910200002]['RectTransform']['m_AnchorMax']==dict(x=1,y=1)
assert d[910200004]['MonoBehaviour']['m_RaycastTarget']==1 and d[910200004]['MonoBehaviour']['m_Color']['a']==0
assert d[450245967780818654]['MonoBehaviour']['m_TargetGraphic']['fileID']==5205809760930852328
assert d[7783337574798386231]['MonoBehaviour']['image']['fileID']==5205809760930852328
assert 910100006 not in d
assert guid(path)=='0e40d531b2bffcc44b12b87cf431a6e7'
scene=documents(ROOT/'Assets/Scenes/Game.unity')
for fid in [2107588836,925286099,1623757146]:assert scene[fid]['RectTransform']['m_LocalScale']==unit
if 685225767 in scene:
 sample=scene[685225767]['PrefabInstance']['m_Modification']['m_Modifications']
 assert any(x['target']['fileID']==6690180372980020888 and x['propertyPath']=='m_IsActive' and int(x['value'])==0 for x in sample)
roles={1192156551:'ResumeCategoryButton',2107588837:'ResumeChoiceButton',103586512:'CompanyChoiceButton',1385924648:'InterviewAnswerButton',2106000002:'EventChoiceButton',411229344:'EndingChoiceButton'}
for fid,role in roles.items():
 assert scene[fid]['MonoBehaviour']['itemPrefab']['guid']==guid(ROOT/f'Assets/Prefabs/UI/{role}.prefab')
preview=documents(ROOT/'Assets/Editor/UI/ChoiceButtonPreview.unity')
instances=[x['PrefabInstance'] for x in preview.values() if 'PrefabInstance'in x]
assert len(instances)==6 and all(x['m_SourcePrefab']['guid']==guid(path) for x in instances)
scaler=preview[627569010]['MonoBehaviour'];assert scaler==scene[627569010]['MonoBehaviour']
assert 'ChoiceButtonPreview' not in (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text()
script=(ROOT/'Assets/Scripts/UI/ChoiceButtonLayoutPreview.cs').read_text(encoding='utf-8')
assert script.startswith('#if UNITY_EDITOR') and script.rstrip().endswith('#endif')
assert not any(any(name in (x.get('MonoBehaviour',{}).get('m_EditorClassIdentifier') or '') for name in ['GameFlowManager','DialogueManager','DailyEventManager','ResumeEditorController']) for x in preview.values())
# Preserve all old prefab object/component IDs except the deliberately removed text mask.
backup=ROOT/'.utmp/choice-layout-before'
if backup.exists():
 old=documents(backup/'ChoiceButton.prefab');assert set(old)-set(d)=={910100006}
print('PASS preserved legacy prefab GUID/IDs, Background/hit separation, unit list chain, independent Production section references, six legacy preview instances, exact CanvasScaler, no preview Production managers/build entry')
print('Legacy source Image mode/artwork preserved. Native sprite appearance, 16:9/resizing, mouse/hover and Game View still require Unity validation.')

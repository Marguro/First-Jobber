// Actual UIManager, MainMenuController, CreateApplicantController and DialogueUI with API doubles.
// Native mouse raycasts/rendering and Unity lifecycle scheduling still require Play Mode.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
class StartupHarness
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    static void Main()
    {
        var ui=new UIManager();var groups=new List<CanvasGroup>();
        foreach(var field in new[]{"mainMenu","createApplicant","resumeEditor","company","interview","dailyEvent","finalDecision","ending","overlay"})
        {
            var group=new CanvasGroup();group.gameObject.SetActive(false);groups.Add(group);Set(ui,field,group);
        }
        Call(ui,"Awake");
        foreach(var group in groups)Check(group.gameObject.activeSelf && group.alpha==0 && !group.blocksRaycasts && !group.interactable,"Inactive authored panel must initialize without becoming visible/clickable");
        ui.ShowPanelFor(GamePhase.MainMenu,true);
        Check(groups[0].alpha==1 && groups[0].interactable,"Main Menu visible");
        ui.ShowPanelFor(GamePhase.DailyEvent,true);
        Check(groups[5].alpha==1 && groups[5].gameObject.activeSelf && !groups[0].interactable,"Daily Event panel activation");
        Console.WriteLine("PASS authored-inactive panel/transition roots activate before use, hidden panels do not block input");
        foreach(bool thai in new[]{false,true})
        foreach(int gender in new[]{1,2})
        {
            LocalizationManager.Thai=thai;
            var menu=new MainMenuController();var name=new TMP_InputField();var dropdown=new TMP_Dropdown();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Option A"));
            var error=new TMP_Text();var button=new Button();
            Set(menu,"nameInput",name);Set(menu,"genderDropdown",dropdown);Set(menu,"errorText",error);Set(menu,"submitButton",button);
            Call(menu,"Awake");Call(menu,"OnEnable");menu.Open(new GameDatabase());
            Check(dropdown.options.Count==3 && dropdown.options[0].text!="Option A","Replace Editor dropdown placeholders with prompt + database genders");
            int submitted=0;string savedName=null,savedGender=null;
            menu.SubmitRequested+=(n,g)=>{submitted++;savedName=n;savedGender=g;};
            button.onClick.Invoke();Check(submitted==0 && error.text.Length>0 && button.interactable,"Empty name validation must not freeze form");
            name.text=" Player ";button.onClick.Invoke();Check(submitted==0 && error.text.Length>0,"Gender prompt must not submit");
            dropdown.SetValueWithoutNotify(gender);
            LocalizationManager.Change();
            Check(dropdown.value==gender,"Language refresh must preserve selected gender index");
            button.onClick.Invoke();button.onClick.Invoke();
            Check(submitted==1 && savedName=="Player" && savedGender==(gender==1?"male":"female") && !button.interactable,"Submit sends trimmed name and stable gender exactly once");
            Call(menu,"OnDisable");
        }
        Console.WriteLine("PASS real Submit listener, male/female IDs, empty-name/prompt validation, EN/TH dropdown initialization, language switching, double-submit guard");
        var dialogue=new DialogueUI();var groupBox=new CanvasGroup();var click=new Button();
        Set(dialogue,"group",groupBox);Set(dialogue,"clickArea",click);
        dialogue.gameObject.SetActive(false);
        dialogue.gameObject.FirstActivation=()=>Call(dialogue,"Awake");
        int clicks=0;dialogue.Clicked+=()=>clicks++;
        dialogue.Show();click.onClick.Invoke();
        Check(dialogue.gameObject.activeSelf && groupBox.alpha==1 && groupBox.interactable && groupBox.blocksRaycasts && clicks==1,"First Show must activate the box, run Awake, THEN reveal it and wire Continue");
        dialogue.Hide();Check(groupBox.alpha==0 && !groupBox.blocksRaycasts,"Hidden dialogue must not intercept controls");
        Console.WriteLine("PASS authored-inactive DialogueBox activates before Show state; Continue callback is wired");
    }
}
namespace UnityEngine
{
    public class Object {public string name;}
    public class GameObject {public bool activeSelf=true;public Action FirstActivation;public void SetActive(bool value){activeSelf=value;if(value && FirstActivation!=null){var action=FirstActivation;FirstActivation=null;action();}}}
    public class MonoBehaviour:Object {public GameObject gameObject=new GameObject();}
    public class RectTransform:Object {}
    public class Sprite:Object {}
    public class CanvasGroup:Object {public GameObject gameObject=new GameObject();public float alpha;public bool interactable,blocksRaycasts;}
    public class SerializeField:Attribute {}
    public class HeaderAttribute:Attribute {public HeaderAttribute(string value){}}
    public class TooltipAttribute:Attribute {public TooltipAttribute(string value){}}
    public class MinAttribute:Attribute {public MinAttribute(float value){}}
    public static class Debug {public static void Log(string m,Object context=null){}public static void LogWarning(string m,Object context=null){}public static void LogError(string m,Object context=null){throw new Exception(m);}}
    public static class Time {public static float unscaledDeltaTime=.1f;}
    public class WaitForSecondsRealtime {public WaitForSecondsRealtime(float f){}}
    public static class Application {public static void Quit(){}}
    public static class Mathf {public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(n,b));public static int Max(int a,int b)=>Math.Max(a,b);public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.0001f;public static float MoveTowards(float a,float b,float step)=>a<b?Math.Min(a+step,b):Math.Max(a-step,b);}
}
namespace UnityEngine.UI
{
    public class ClickEvent {event Action handlers;public void AddListener(Action a){handlers+=a;}public void RemoveListener(Action a){handlers-=a;}public void Invoke(){handlers?.Invoke();}}
    public class Button:UnityEngine.Object {public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public bool interactable=true;public ClickEvent onClick=new ClickEvent();}
    public class Image:UnityEngine.Object {public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public UnityEngine.Sprite sprite;}
}
namespace UnityEngine.InputSystem {public class Key {public bool wasPressedThisFrame;}public class Keyboard {public static Keyboard current;public Key spaceKey=new Key(),enterKey=new Key();}}
namespace TMPro
{
    public class TMP_InputField {public string text="";}
    public class TMP_Text {public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public string text="";public int maxVisibleCharacters;public Info textInfo=new Info();public void ForceMeshUpdate(){textInfo.characterCount=text.Length;}public class Info {public int characterCount;}}
    public class TMP_Dropdown {public class OptionData {public string text;public OptionData(string s){text=s;}}public List<OptionData> options=new List<OptionData>();public int value;public void ClearOptions(){options.Clear();}public void AddOptions(List<OptionData> list){options.AddRange(list);}public void SetValueWithoutNotify(int v){value=v;}public void RefreshShownValue(){}}
}
namespace FirstJobber
{
    public static class Genders {public const string Unspecified="unspecified";}
    public class GenderOption {public string id,displayName;}
    public class GameDatabase {public string genderPrompt="Select gender...";public int maxNameLength=24;public const string GenderPromptKey="database/genderPrompt";public static string GenderKey(string id)=>"gender/"+id;public List<GenderOption> genders=new List<GenderOption>{new GenderOption{id="male",displayName="Male"},new GenderOption{id="female",displayName="Female"}};}
    public class GameSession {public static bool HasSave()=>false;}
    public static class LocalizationManager {public static bool Thai;public static event Action LanguageChanged;public static void Change(){Thai=!Thai;LanguageChanged?.Invoke();}public static string Get(string k,string en)=>Thai?"TH:"+en:en;}
    public class Label {public string Text="Validation error";public string Format(int n)=>Text;}
    public static class UiText {public static Label NameRequired=new Label(),NameTooLong=new Label(),GenderRequired=new Label();}
}

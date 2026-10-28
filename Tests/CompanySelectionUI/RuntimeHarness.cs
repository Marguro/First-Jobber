// Exercises the real Production controller with UI API doubles; not native Unity rendering/Play Mode.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
class CompanyHarness
{
    static void Check(bool v,string message){if(!v)throw new Exception(message);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static void Main()
    {
        var controller=new CompanySelectionController();var list=new ChoiceListUI();var details=new TMP_Text();var name=new TMP_Text();var heading=new TMP_Text();var apply=new Button();var back=new Button();var scroll=new ScrollRect();
        Set(controller,"companyList",list);Set(controller,"detailsText",details);Set(controller,"companyNameText",name);Set(controller,"headingText",heading);Set(controller,"applyButton",apply);Set(controller,"backButton",back);Set(controller,"detailsScroll",scroll);Set(controller,"detailsContent",new RectTransform());
        var sprites=new[]{new Sprite(),new Sprite(),new Sprite(),new Sprite(),new Sprite()};Set(controller,"backgroundVariants",sprites);Call(controller,"Awake");
        var ctx=new GameContext();for(int i=0;i<8;i++)ctx.Database.companies.Add(new CompanyData{id="c"+i,companyName="Company "+i,description="Job Description\n- Existing responsibility\n\nTerms\n- Existing condition",salary=i==1?"THB 15,000–20,000":""});
        controller.Open(ctx);Check(!apply.interactable,"Apply must require a company");
        var assigned=new Dictionary<string,Sprite>();var first=new HashSet<Sprite>();
        for(int i=0;i<list.Items.Count;i++){var item=list.Items[i];Check(item.id=="c"+i,"Visual shuffle changed company order");Check(item.subtitle==null && item.icon==null,"Card contains extra company content");Check(item.badge==(i==1?"THB 15,000–20,000":"Salary not specified"),"Authored salary or fallback incorrect");assigned[item.id]=item.backgroundSprite;if(i<5)Check(first.Add(item.backgroundSprite),"Repeated paper before all five were used");}
        int confirmed=0,backCount=0;controller.CompanyConfirmed+=id=>{Check(id=="c2","Wrong company applied");confirmed++;};controller.BackRequested+=()=>backCount++;
        list.Choose("c2");Check(name.text=="Company 2" && details.text==ctx.Database.companies[2].description && apply.interactable,"Selection did not bind existing company details");Check(scroll.verticalNormalizedPosition==1,"Selecting company did not reset description scroll");
        LocalizationManager.Thai=true;scroll.verticalNormalizedPosition=.2f;controller.RefreshLanguage();
        Check(name.text=="TH:Company 2" && details.text=="TH:"+ctx.Database.companies[2].description,"Details localization disconnected");Check(list.Items[0].badge=="ไม่ระบุเงินเดือน" && scroll.verticalNormalizedPosition==1,"Thai fallback or language scroll reset wrong");
        for(int n=0;n<20;n++){controller.RefreshLanguage();for(int i=0;i<list.Items.Count;i++)Check(list.Items[i].backgroundSprite==assigned[list.Items[i].id],"Language/selection rerolled paper");}
        ctx.Choices.Consumed.Add("c0");controller.RefreshLanguage();Check(list.Items[0].id=="c1" && list.Items[0].backgroundSprite==assigned["c1"],"Depletion shifted paper associations/order");
        apply.onClick.Invoke();apply.onClick.Invoke();list.Choose("c3");Check(confirmed==1 && !apply.interactable && !back.interactable,"Repeated Apply queued multiple interviews or bypassed lock");
        controller.Open(ctx);foreach(var item in list.Items)Check(item.backgroundSprite==assigned[item.id],"Reopening same session rerolled paper");back.onClick.Invoke();back.onClick.Invoke();Check(backCount==1,"Back callback duplicated");
        Set(controller,"backgroundVariants",new Sprite[0]);ctx.Session=new GameSession();controller.Open(ctx);foreach(var item in list.Items)Check(item.backgroundSprite==null,"New session did not clear visual assignments");
        Console.WriteLine("PASS actual company controller: shuffled five-paper distribution, >5 companies, stable ID/session mapping, company order/depletion, only name/salary, authored salary/fallback EN/TH, details/scroll reset, Apply/Back duplicate guards, no variants fallback");
    }
}
namespace UnityEngine
{
    public class Object { }
    public class Sprite:Object { }
    public class RectTransform:Object { }
    public class MonoBehaviour:Object { public T GetComponentInParent<T>()where T:class=>null; }
    public class SerializeField:Attribute { }
    public class HeaderAttribute:Attribute { public HeaderAttribute(string value){} }
    public static class Debug {public static void LogError(string message,Object context) {throw new Exception(message);} }
    public static class Canvas { public static void ForceUpdateCanvases(){} }
}
namespace TMPro {public class TMP_Text:MonoBehaviour {public string text;} }
namespace UnityEngine.UI
{
    public class ClickEvent {event Action listeners;public void AddListener(Action a)=>listeners+=a;public void RemoveListener(Action a)=>listeners-=a;public void Invoke()=>listeners?.Invoke();}
    public class Button {public bool interactable=true;public ClickEvent onClick=new ClickEvent();}
    public class ScrollRect {public RectTransform content;public float verticalNormalizedPosition;}
    public static class LayoutRebuilder {public static void ForceRebuildLayoutImmediate(RectTransform rect){} }
}
namespace FirstJobber
{
    public enum ChoiceState {Available,Selected,Consumed,Disabled}
    public struct ChoiceItem {public string id,title,subtitle,badge;public Sprite icon,backgroundSprite;public ChoiceState state;}
    public class ChoiceListUI {public List<ChoiceItem> Items;public event Action<string> ItemChosen;public void SetItems(List<ChoiceItem> items,string emptyMessage,bool resetScroll){Items=new List<ChoiceItem>(items);}public void SetInteractable(bool value){}public void Choose(string id)=>ItemChosen?.Invoke(id);}
    public class CompanyData {public string id,companyName,description,salary;public object playerReaction;}
    public class GameSession { }
    public class GameDatabase {public List<CompanyData> companies=new List<CompanyData>();public bool showRejectedCompanies;public CompanyData FindCompany(string id)=>companies.Find(c=>c.id==id);}
    public class ChoiceManager {public HashSet<string> Consumed=new HashSet<string>();public bool IsConsumed(CompanyData c)=>Consumed.Contains(c.id);}
    public class DialogueManager {public void Play(object lines,object callback,float time){} }
    public class GameContext {public GameDatabase Database=new GameDatabase();public GameSession Session=new GameSession();public ChoiceManager Choices=new ChoiceManager();public DialogueManager Dialogue=new DialogueManager();}
    public static class LocalizationManager
    {
        public static bool Thai;
        public static string Get(CompanyData c,string field,string english)=>Thai?"TH:"+english:english;
        public static string Get(string key,string english)=>Thai?(key=="ui/company/salary_not_specified"?"ไม่ระบุเงินเดือน":"TH:"+english):english;
    }
}

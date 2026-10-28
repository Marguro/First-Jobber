// Uses real LocalizationKeys, LocalizationManager, LocalizationTable and DialogueManager.
// Unity rendering/resources/preferences are doubles; this is not Unity Play Mode.
using System;
using System.Collections.Generic;
using System.Reflection;
using FirstJobber;
class ResumeLocalizationHarness
{
    static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    static void Drain(DialogueManager player)
    {
        for(int i=0;i<2000;i++)
        {
            var running=player.Pending;
            if(running==null)return;
            if(!running.MoveNext() && player.Pending==running)player.Pending=null;
        }
        throw new Exception("Dialogue did not finish");
    }
    static void Main()
    {
        var db=ResumeFixture.Create();
        var keys=new Dictionary<string,string>();
        LocalizationKeys.Visit(db,(k,en)=>keys.Add(k,en));
        Check(keys.Count==63,"Expected 63 active production resume keys");
        var table=ResumeFixture.Table;
        var multiQuestion=new InterviewQuestionData {id="test_shared",questionDialogue=new[]{new DialogueLine {speaker="HR",dialogueLines=new List<string>{"Question page one","Question page two"}}}};
        multiQuestion.answers=new List<InterviewAnswer>{new InterviewAnswer {id="test_answer",hrResponse=new[]{new DialogueLine {speaker="HR",emotion=HREmotion.Disappointed,dialogueLines=new List<string>{"Response page one","Response page two"}}}}};
        var questionHr=new HRCharacterData {hrName="Test HR"};questionHr.resumeLinkedQuestions.Add(multiQuestion);
        LocalizationKeys.VisitAsset(questionHr,(k,en)=>{if(k.StartsWith("question/"))table.entries.Add(new LocalizationTable.Entry {key=k,english=en,thai="TH:"+en});});
        Check(multiQuestion.questionDialogue[0].GetPageKey(1)=="question/test_shared/questionDialogue/0/lines/1","HR-owned shared question pages must receive startup keys");
        foreach(var pair in keys)
            Check(table.TryGet(pair.Key,out var th) && !string.IsNullOrWhiteSpace(th),"Missing Thai: "+pair.Key);
        var player=new DialogueManager();var ui=new DialogueUI();
        typeof(DialogueManager).GetField("ui",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,ui);
        int languageChanges=0;
        LocalizationManager.LanguageChanged+=()=>languageChanges++;
        foreach(var language in new[]{Language.English,Language.Thai})
        {
            LocalizationManager.SetLanguage(language);
            var questionPage=multiQuestion.questionDialogue[0];
            Check(LocalizationManager.Get(questionPage.GetPageKey(1),questionPage.GetPage(1))==(language==Language.Thai?"TH:Question page two":"Question page two"),"Question dialogue locale");
            var responsePage=multiQuestion.answers[0].hrResponse[0];
            Check(responsePage.GetPageKey(1)=="question/test_shared/answers/test_answer/hrResponse/0/lines/1" && LocalizationManager.Get(responsePage.GetPageKey(1),responsePage.GetPage(1))==(language==Language.Thai?"TH:Response page two":"Response page two"),"HR response dialogue locale");
            foreach(var pair in keys)
            {
                table.TryGet(pair.Key,out var thai);
                Check(LocalizationManager.Get(pair.Key,pair.Value)==(language==Language.Thai?thai:pair.Value),"Wrong locale "+pair.Key);
            }
            foreach(var choice in db.choices)
            {
                int completed=0;ui.Pages.Clear();
                player.Play(choice.playerReaction,()=>completed++,1f,DialoguePlaybackMode.Sequential);Drain(player);
                Check(completed==1 && ui.Pages.Count==choice.playerReaction.Length,"Lost reaction pages: "+choice.id);
                for(int b=0;b<choice.playerReaction.Length;b++)
                {
                    var line=choice.playerReaction[b];var key=line.GetPageKey(0);
                    Check(key=="choice/"+choice.id+"/playerReaction/"+b+"/lines/0","Wrong block/page key "+key);
                    Check(ui.Pages[b]==LocalizationManager.Get(key,line.GetPage(0)),"Dialogue did not use localized page");
                }
                // Preserve authored RandomOne: one block each time, not every authored page.
                ui.Pages.Clear();player.Play(choice.playerReaction,null,1f,DialoguePlaybackMode.RandomOne);Drain(player);
                Check(ui.Pages.Count==1,"RandomOne changed");
            }
        }
        Check(languageChanges==1,"Language event should fire once for a real change");
        Check(LocalizationManager.Get("missing/key","English fallback")=="English fallback","Missing key fallback");
        Check(LocalizationManager.Get("test/empty","English fallback")=="English fallback","Empty Thai fallback");
        Check(LocalizationManager.Get("test/whitespace","English fallback")=="English fallback","Whitespace Thai fallback");
        var legacy=new DialogueLine("Player","Legacy source") {locKey="test/legacy/text"};
        Check(legacy.GetPageKey(0)=="test/legacy/text" && LocalizationManager.Get(legacy.GetPageKey(0),legacy.GetPage(0))=="Legacy Thai","Legacy text key");
        var multi=new DialogueLine {dialogueLines=new List<string>{"One","Two"},locKey="test/multi/0/text"};
        Check(multi.GetPageKey(0)=="test/multi/0/lines/0" && multi.GetPageKey(1)=="test/multi/0/lines/1","Multipage keys");
        Console.WriteLine("PASS real C# keys and EN/TH lookup for all 63 fields; all 42 localized reaction pages; RandomOne; legacy/multipage keys; missing/empty/whitespace fallback; language event");
    }
}

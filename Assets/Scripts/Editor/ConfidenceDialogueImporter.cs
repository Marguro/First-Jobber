using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Creates Assets/Data/Production/ConfidenceDialogue_Production.asset (day-based Apply reaction, 5 days, 24 candidates),
    /// attaches it to GameDatabase_Production and writes the Thai text into LocalizationTable_TH.
    /// Safe to run again: days / candidates are matched by id, existing English text and Thai translations are never overwritten.
    /// The demo database is not touched (it keeps the legacy pools).
    /// </summary>
    public static class ConfidenceDialogueImporter
    {
        const string AssetPath = SampleDataGenerator.ProductionRoot + "/ConfidenceDialogue_Production.asset";
        const string ProductionDbPath = SampleDataGenerator.ProductionRoot + "/GameDatabase_Production.asset";
        const string TablePath = "Assets/Resources/Localization/LocalizationTable_TH.asset";

        struct C
        {
            public string en, th;
            public C(string en, string th) { this.en = en; this.th = th; }
        }

        // day -> (high, low); every entry is ONE independent random candidate
        static readonly C[][] Day1High = {
            new[] { new C("Wow! Look at my profile! How could a game company possibly pass up someone as talented as me?!", "โอ้โห! ดูโปรไฟล์ของฉันสิ! แบบนี้บริษัทเกมจะพลาดคนเก่งอย่างฉันไปได้ยังไงกันล่ะ?!") },
            new[] { new C("All right, this is the one! My first job is going to be great!", "เอาล่ะ บริษัทนี้แหละ! งานแรกของฉันต้องดีแน่นอน!") },
            new[] { new C("Just send it already!!", "ส่งเลยแล้วกันนนน!!") },
        };
        static readonly C[][] Day1Low = {
            new[] { new C("Uh... come to think of it, my profile doesn't really stand out that much...", "เอ่อ... จะว่าไป โปรไฟล์ของเราก็ยังดูไม่ได้โดดเด่นขนาดนั้นแฮะ...") },
            new[] { new C("Would this company even be interested in me? Or will they open my resume and close it right away...", "บริษัทนี้จะสนใจเราจริง ๆ เหรอนะ? หรือเขาจะเปิด Resume มาแล้วก็ปิดทิ้งเลย...") },
            new[] { new C("But you never know until you try!", "แต่ถ้าไม่ลองก็ไม่รู้!") },
            new[] { new C("I may not be fully confident, but I really do want to work in games!", "ถึงจะยังไม่มั่นใจเต็มร้อย แต่ฉันก็อยากทำงานเกมจริง ๆ นี่นา!") },
            new[] { new C("Let's go! At least I have to give myself a chance first!", "ลุยเลย! อย่างน้อยก็ต้องให้โอกาสตัวเองก่อน!") },
        };
        static readonly C[][] Day2High = {
            new[] { new C("All right! Maybe the last company just wasn't the right place for me! My skills aren't bad at all. This company must have something good for me!", "เอาล่ะ! บริษัทที่แล้วอาจแค่ไม่ใช่ที่ของเราก็ได้! ฝีมือเราก็ไม่ได้แย่สักหน่อย บริษัทนี้ต้องมีอะไรดี ๆ บ้างแหละ!") },
        };
        static readonly C[][] Day2Low = {
            new[] { new C("Ah... I have to send it again, huh. I hope I don't get rejected too quickly this time...", "อ่า... ต้องส่งอีกแล้วสินะ หวังว่าคราวนี้จะไม่โดนปฏิเสธเร็วเกินไปนะ...") },
            new[] { new C("Oh well, let's give it a try.", "เอาเถอะ ลองดูก็ได้") },
        };
        static readonly C[][] Day3High = {
            new[] { new C("My work isn't bad, and I have the skills. So what's still missing?", "ผลงานเราก็ไม่ได้แย่ เราก็มีความสามารถ แล้วมันยังขาดอะไรอีกล่ะ?") },
            new[] { new C("Whatever, just send it for now. Soon I'll find out what excuse they come up with this time to turn me down.", "ช่างเถอะ ส่งไปก่อนละกัน เดี๋ยวค่อยรู้ว่าคราวนี้เขาจะหาเหตุผลอะไรมาไม่รับเรา") },
        };
        static readonly C[][] Day3Low = {
            new[] { new C("Same resume, same portfolio, same questions... It feels like I'm doing everything over and over. The only differences are the company name and the rejection I'm about to get...", "เรซูเม่เดิม ผลงานเดิม คำถามเดิม... เหมือนทำทุกอย่างซ้ำไปซ้ำมาเลยแฮะ ต่างกันแค่ชื่อบริษัทกับคำปฏิเสธที่กำลังจะได้รับ...") },
        };
        static readonly C[][] Day4High = {
            new[] { new C("Yeah, I'm good, and I know it... So what? In the end I still have to sit here applying for jobs like always. Ha ha...", "เออ ฝีมือเราก็ดี เราก็รู้ตัวดี... แล้วไงต่อล่ะ? สุดท้ายก็ต้องมานั่งสมัครงานเหมือนเดิมอยู่ดีนี่นา ฮะ ๆ...") },
            new[] { new C("Fine, just send it so the day can be over.", "เอาเถอะ ส่ง ๆ ไป จะได้จบอีกวัน") },
        };
        static readonly C[][] Day4Low = {
            new[] { new C("...I don't even want to look at my resume anymore.", "...ไม่อยากเปิดดูเรซูเม่แล้วด้วยซ้ำ") },
            new[] { new C("But if I don't send it, there's no job, no money, and I'll keep worrying about the same things... I'm so tired.", "แต่ถ้าไม่ส่ง ก็ไม่มีงาน ไม่มีเงิน แล้วก็ต้องนั่งกังวลเรื่องเดิม ๆ ต่อไป... เหนื่อยจังวะ") },
        };
        static readonly C[][] Day5High = {
            new[] { new C("I've done well all along... I studied hard, built my portfolio, trained myself for so many years.", "เราก็ทำได้ดีมาตลอดนี่นา... ตั้งใจเรียน ทำพอร์ต ฝึกฝนตัวเองมาตั้งกี่ปี") },
            new[] { new C("So why is it so hard just to find a job somewhere?", "แล้วทำไมแค่จะหางานทำสักที่ มันถึงยากขนาดนี้กันนะ?") },
            new[] { new C("...Yeah, forget it. Just send it, so I finally find out what reason they'll reject me for this time.", "...เออ ช่างมัน ส่งไปเถอะ จะได้รู้กันสักทีว่าคราวนี้จะโดนปฏิเสธด้วยเหตุผลอะไร") },
        };
        static readonly C[][] Day5Low = {
            new[] { new C("Do I have to do this again?... Pick a company, attach my portfolio, write my resume, and then sit and wait...", "ต้องทำอีกแล้วเหรอ... เลือกบริษัท แนบพอร์ต เขียนเรซูเม่ แล้วก็นั่งรอ...") },
            new[] { new C("I don't even know if anyone will open it once I send it.", "ไม่รู้ด้วยซ้ำว่าถ้าส่งไปแล้วจะมีใครเปิดดูไหม") },
            new[] { new C("...But if I don't send it, I'll just have to come back and do this again tomorrow.", "...แต่ถ้าไม่ส่ง พรุ่งนี้ก็คงต้องกลับมานั่งทำแบบนี้อยู่ดี") },
        };

        [MenuItem("Tools/First Jobber/23. Import Production Confidence Dialogue")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Confidence] Leave Play mode first."); return; }
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProductionDbPath);
            if (db == null) { Debug.LogError("[Confidence] Run '15. Import Production Content' first (no " + ProductionDbPath + ")."); return; }
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table == null) { Debug.LogError("[Confidence] Thai table not found."); return; }

            var asset = AssetDatabase.LoadAssetAtPath<ConfidenceDialogueDatabase>(AssetPath);
            if (asset == null)
            {
                if (File.Exists(AssetPath))
                {
                    Debug.LogError("[Confidence] The existing asset cannot be loaded. Repair its MonoScript reference or compilation errors before importing; serialized content was left untouched.");
                    return;
                }
                asset = ScriptableObject.CreateInstance<ConfidenceDialogueDatabase>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            var thai = new Dictionary<string, string>();   // candidate dialogue -> Thai, filled below
            var pools = new[] { (1, Day1High, Day1Low), (2, Day2High, Day2Low), (3, Day3High, Day3Low), (4, Day4High, Day4Low), (5, Day5High, Day5Low) };
            foreach (var (day, high, low) in pools)
            {
                var cfg = asset.days.Find(d => d != null && d.day == day);
                if (cfg == null) { cfg = new ConfidenceDayConfig { day = day }; asset.days.Add(cfg); }
                Fill(cfg.high, high, thai, day, "high");
                Fill(cfg.low, low, thai, day, "low");
            }
            asset.days.Sort((a, b) => a.day.CompareTo(b.day));
            EditorUtility.SetDirty(asset);

            db.confidenceDialogue = asset;
            db.requireDayBasedConfidence = true;
            EditorUtility.SetDirty(db);

            // Register Thai under the exact keys the game looks up.
            LocalizationKeys.VisitAsset(asset, null);
            int added = 0, kept = 0;
            foreach (var cfg in asset.days)
            {
                foreach (var list in new[] { cfg.high, cfg.low })
                    foreach (var cand in list)
                    {
                        if (!thai.TryGetValue(cand.id + "|" + cfg.day + "|" + (list == cfg.high ? "high" : "low"), out var th)) continue;
                        string key = cand.dialogue.GetPageKey(0);
                        var e = table.entries.Find(x => x != null && x.key == key);
                        if (e == null) { table.entries.Add(new LocalizationTable.Entry { key = key, english = cand.dialogue.GetPage(0), thai = th }); added++; }
                        else if (string.IsNullOrWhiteSpace(e.thai)) { e.thai = th; e.english = cand.dialogue.GetPage(0); added++; }
                        else kept++;
                    }
            }
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            int total = 0;
            foreach (var cfg in asset.days) total += cfg.high.Count + cfg.low.Count;
            Debug.Log("[Confidence] " + asset.days.Count + " days, " + total + " candidates (expected 24). Thai: " + added + " added, " + kept + " kept. Attached to " + db.name + ".");
            LocalizationTools.Export();
        }

        static void Fill(List<ConfidenceCandidate> target, C[][] source, Dictionary<string, string> thai, int day, string pool)
        {
            for (int i = 0; i < source.Length; i++)
            {
                string id = "candidate_" + i.ToString("00");
                var cand = target.Find(c => c != null && c.id == id);
                if (cand == null)
                {
                    cand = new ConfidenceCandidate
                    {
                        id = id,
                        // Player speaker, one page in the legacy text field => key .../<id>/text. Multi-page can be added later in the Inspector.
                        dialogue = new DialogueLine { speaker = Speakers.Player, text = source[i][0].en }
                    };
                    target.Add(cand);
                }
                thai[id + "|" + day + "|" + pool] = source[i][0].th;
            }
        }
    }
}

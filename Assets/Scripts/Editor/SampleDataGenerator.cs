using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Tools > First Jobber > Generate Sample Data.
    /// Creates placeholder content under Assets/Data. Existing assets are NEVER overwritten.
    /// </summary>
    public static class SampleDataGenerator
    {
        const string Root = "Assets/Data";

        [MenuItem("Tools/First Jobber/1. Generate Sample Data")]
        public static void Generate()
        {
            EnsureFolder(Root);
            foreach (var f in new[] { "Categories", "Choices", "Companies", "Questions", "Events", "Endings" })
                EnsureFolder(Root + "/" + f);

            var db = GetOrCreate<GameDatabase>(Root + "/GameDatabase.asset", out bool dbIsNew);

            var edu = Category("Cat_01_Education", "education", "Education / University", "No universities left to list.");
            var dream = Category("Cat_02_Dream", "dream", "Dream", "There are no dreams left to write down.");
            var passion = Category("Cat_03_Passion", "passion", "Passion", "I can't think of anything I still love.");

            // ---------- choices ----------
            var bigCity = Choice(edu, "Edu_01_BigCity", "edu_big_city", "Big City University", "B.Sc. Computer Science",
                P("The name looks good on paper. I hope it's enough."),
                H("Big City University. Everyone has that on their resume.", HREmotion.Neutral));
            var smallTown = Choice(edu, "Edu_02_SmallTown", "edu_small_town", "Small Town University", "B.A. Liberal Arts",
                P("It's not famous, but I worked hard there."),
                H("I see you graduated from Small Town University.", HREmotion.Disappointed));
            var artCollege = Choice(edu, "Edu_03_ArtCollege", "edu_art_college", "Riverside Art College", "B.F.A. Digital Media",
                P("I learned more there than anywhere else."),
                H("An art college... that's an unusual path for this position.", HREmotion.Uncomfortable));
            var online = Choice(edu, "Edu_04_Online", "edu_online", "Online Degree Program", "Self-paced, remote",
                P("Studied at night. It counts, right?"),
                H("An online degree. Hm.", HREmotion.Uncomfortable));

            var gameDev = Choice(dream, "Dream_01_GameDev", "dream_game_dev", "Game Developer", "Build worlds people can play",
                P("I've wanted this since I was a kid."),
                H("So you want to make games for a living?", HREmotion.Uncomfortable));
            var artist = Choice(dream, "Dream_02_Artist", "dream_artist", "Artist", "Make things people feel",
                P("Drawing is the only time I stop thinking."),
                H("Artist. And you're applying here because...?", HREmotion.Disappointed));
            var programmer = Choice(dream, "Dream_03_Programmer", "dream_programmer", "Programmer", "Solve problems with code",
                P("Practical. Safe. Maybe too safe."),
                H("A programmer. That's a good answer. A common one.", HREmotion.Interested));
            var writer = Choice(dream, "Dream_04_Writer", "dream_writer", "Writer", "Tell stories",
                P("Nobody pays you to write, they say."),
                H("A writer? Do you have anything published?", HREmotion.Disappointed));

            var gameDesign = Choice(passion, "Passion_01_GameDesign", "passion_game_design", "Game Design", "Systems, rules, play",
                P("I could talk about game design for hours."),
                H("Game design. That's a hobby, isn't it?", HREmotion.Uncomfortable));
            var tech = Choice(passion, "Passion_02_Tech", "passion_technology", "Technology", "New tools, new ideas",
                P("I like knowing how things work."),
                H("Technology. Very broad.", HREmotion.Neutral));
            var music = Choice(passion, "Passion_03_Music", "passion_music", "Music", "Playing and composing",
                P("It's what I do when I can't sleep."),
                H("Music. Do you play professionally?", HREmotion.Disappointed));
            var story = Choice(passion, "Passion_04_Storytelling", "passion_storytelling", "Storytelling", "Characters and worlds",
                P("Everyone has a story. I just like telling them."),
                H("Storytelling... I'm not sure how that applies here.", HREmotion.Uncomfortable));

            // ---------- companies ----------
            Company("Company_01_PixelPioneers", "company_pixel_pioneers", "Pixel Pioneers", "Junior Game Designer", "$2,400 / month", "Game Studio",
                "A small studio with big plans and no budget.",
                P("A game studio... this could actually be it."),
                H("Welcome to Pixel Pioneers. Please sit.", HREmotion.Neutral));
            Company("Company_02_Brightline", "company_brightline", "Brightline Corp", "Assistant Account Executive", "$3,100 / month", "Marketing",
                "Fast-paced. Lots of meetings.",
                P("Marketing. I don't know anything about marketing."),
                H("Thank you for coming to Brightline. Let's begin.", HREmotion.Neutral));
            Company("Company_03_GrayTower", "company_gray_tower", "Gray Tower Bank", "Data Entry Clerk", "$2,800 / month", "Finance",
                "Stable hours. Stable everything.",
                P("A bank. The building looks like it never sleeps."),
                H("Gray Tower Bank. You're late by two minutes.", HREmotion.Disappointed));
            Company("Company_04_NovaSoft", "company_nova_soft", "Nova Soft", "QA Tester", "$2,600 / month", "Software",
                "Find bugs before the customers do.",
                P("Testing games is basically playing them, right?"),
                H("Nova Soft. Let's get this over with.", HREmotion.Neutral));

            // ---------- questions: choice-linked ----------
            Question("Q_Edu_BigCity", "q_edu_big_city", "Many applicants come from Big City University. What makes you different?", HREmotion.Neutral, bigCity, null,
                A("a1", "I worked on projects outside class.", "I hope that counts for something.", "Projects. Everyone says that.", HREmotion.Neutral),
                A("a2", "Honestly, nothing yet.", "That came out wrong.", "At least you're honest.", HREmotion.Disappointed),
                A("a3", "I learn fast.", "Please believe me.", "We'll see.", HREmotion.Neutral));
            Question("Q_Edu_SmallTown", "q_edu_small_town", "Why did you choose Small Town University?", HREmotion.Disappointed, smallTown, null,
                A("a1", "It was close to home.", "Not the most inspiring answer.", "Convenient. I suppose.", HREmotion.Disappointed),
                A("a2", "It taught me to work with what I have.", "I almost believe that myself.", "That's a nice way to say it.", HREmotion.Interested),
                A("a3", "I couldn't afford anywhere else.", "Why did I say that out loud?", "Oh. I see.", HREmotion.Uncomfortable));
            Question("Q_Dream_GameDev", "q_dream_game_dev", "Why do you want to become a game developer?", HREmotion.Uncomfortable, gameDev, null,
                A("a1", "I love creating experiences.", "Games have always meant something to me.", "Passion alone doesn't make you qualified.", HREmotion.Disappointed),
                A("a2", "I want to make people feel something.", "That sounded better in my head.", "This is a business, not an art project.", HREmotion.Uncomfortable),
                A("a3", "It's stable enough these days.", "That's not even true.", "That is... not true at all.", HREmotion.Disappointed));
            Question("Q_Dream_Artist", "q_dream_artist", "Artists rarely last in this industry. Why should we invest in you?", HREmotion.Disappointed, artist, null,
                A("a1", "I never stop practicing.", "I practice every night.", "Practice isn't a deliverable.", HREmotion.Disappointed),
                A("a2", "I can learn other skills.", "Another thing to give up.", "Flexible. Good.", HREmotion.Interested),
                A("a3", "Maybe you shouldn't.", "I can't believe I said that.", "...", HREmotion.Uncomfortable));
            Question("Q_Passion_GameDesign", "q_passion_game_design", "Game design is a hobby for most people. Why is it more for you?", HREmotion.Neutral, gameDesign, null,
                A("a1", "I've finished three prototypes.", "None of them are good. But they exist.", "Prototypes. On your own time.", HREmotion.Neutral),
                A("a2", "It's how I understand people.", "That's the truest thing I've said today.", "Interesting. Unusual.", HREmotion.Interested),
                A("a3", "I don't know.", "I really don't.", "Then why list it?", HREmotion.Disappointed));
            Question("Q_Passion_Music", "q_passion_music", "Music is on your resume. How does it help you here?", HREmotion.Neutral, music, null,
                A("a1", "It taught me discipline.", "Hours of scales, nobody listening.", "Discipline. We value that.", HREmotion.Interested),
                A("a2", "It doesn't, really.", "I should have lied.", "Then why is it here?", HREmotion.Disappointed),
                A("a3", "I can keep a team in rhythm.", "That's a stretch.", "That is a stretch.", HREmotion.Uncomfortable));

            // ---------- questions: category-linked ----------
            Question("Q_Cat_Education_1", "q_cat_education_1", "Tell me about your time at university.", HREmotion.Neutral, null, edu,
                A("a1", "I mostly studied alone.", "It was a lonely four years.", "Self-motivated, then.", HREmotion.Neutral),
                A("a2", "I joined every club I could.", "And finished none of them.", "Involved. Good.", HREmotion.Interested),
                A("a3", "I'd rather talk about the future.", "Please change the subject.", "Avoiding the question.", HREmotion.Disappointed));
            Question("Q_Cat_Education_2", "q_cat_education_2", "What was your biggest academic failure?", HREmotion.Neutral, null, edu,
                A("a1", "A group project that fell apart.", "I carried that team. For nothing.", "Teamwork matters here.", HREmotion.Neutral),
                A("a2", "I haven't failed.", "A lie. A confident one.", "Everyone has failed.", HREmotion.Disappointed),
                A("a3", "I failed an exam and learned from it.", "A rehearsed answer.", "A standard answer.", HREmotion.Neutral));
            Question("Q_Cat_Dream_1", "q_cat_dream_1", "Where do you see yourself in five years?", HREmotion.Neutral, null, dream,
                A("a1", "Doing what I dreamed of.", "If only it were that simple.", "Dreams are not a plan.", HREmotion.Disappointed),
                A("a2", "In a position of responsibility.", "I have no idea.", "Ambitious. Good.", HREmotion.Interested),
                A("a3", "Somewhere stable.", "Somewhere. Anywhere.", "Stable. Sure.", HREmotion.Neutral));
            Question("Q_Cat_Dream_2", "q_cat_dream_2", "Would you give up your dream for this job?", HREmotion.Uncomfortable, null, dream,
                A("a1", "Yes, if I had to.", "Did I just say that?", "A realistic answer.", HREmotion.Interested),
                A("a2", "No.", "It's the only thing I have.", "Then why are you here?", HREmotion.Disappointed),
                A("a3", "I'd find a way to do both.", "Both is exhausting.", "Everyone says that.", HREmotion.Neutral));
            Question("Q_Cat_Passion_1", "q_cat_passion_1", "What do you do when you're not working?", HREmotion.Neutral, null, passion,
                A("a1", "I work on my passion projects.", "That's all I do.", "No work-life balance, then.", HREmotion.Uncomfortable),
                A("a2", "I rest.", "I stare at the ceiling.", "Rest is important.", HREmotion.Neutral),
                A("a3", "I look for jobs.", "True. Painfully true.", "...I see.", HREmotion.Uncomfortable));
            Question("Q_Cat_Passion_2", "q_cat_passion_2", "Would your passion ever get in the way of your work?", HREmotion.Neutral, null, passion,
                A("a1", "Never.", "Liar.", "That's what they all say.", HREmotion.Neutral),
                A("a2", "It would make my work better.", "Please let that be true.", "We prefer focus.", HREmotion.Disappointed),
                A("a3", "I can keep them separate.", "I've been separating them for years.", "Good.", HREmotion.Interested));

            // ---------- questions: generic fallback ----------
            Question("Q_Gen_Yourself", "q_gen_yourself", "Tell me about yourself.", HREmotion.Neutral, null, null,
                A("a1", "I'm a recent graduate who works hard.", "Same words as yesterday.", "Hm.", HREmotion.Neutral),
                A("a2", "I'm still figuring that out.", "Too honest again.", "Not exactly confidence-inspiring.", HREmotion.Disappointed),
                A("a3", "I'm persistent.", "That's the word. Persistent.", "Persistence is good.", HREmotion.Interested));
            Question("Q_Gen_Weakness", "q_gen_weakness", "What is your greatest weakness?", HREmotion.Neutral, null, null,
                A("a1", "I care too much.", "Everyone says that.", "A cliche.", HREmotion.Disappointed),
                A("a2", "I overthink everything.", "Like this answer.", "At least that's real.", HREmotion.Neutral),
                A("a3", "I don't know when to stop.", "I should stop.", "Noted.", HREmotion.Uncomfortable));
            Question("Q_Gen_Why", "q_gen_why", "Why should we hire you?", HREmotion.Neutral, null, null,
                A("a1", "Because I'm motivated.", "Because I need to eat.", "So are the others.", HREmotion.Neutral),
                A("a2", "I'm not sure you should.", "Why did I say that?", "Interesting.", HREmotion.Uncomfortable),
                A("a3", "Because I won't give up.", "I won't. I can't.", "We'll be in touch.", HREmotion.Neutral));

            // ---------- events ----------
            Event("Event_01_FatherMessage", "event_father_message", EventTrigger.OnDayStart, 2, 0, 10, "Day 2", null,
                L("Narrator", "Another morning. Another message on my phone."),
                L("Father", "\"How is the job search going? Call me when you can.\""),
                S("I'll call him back later. Maybe."));
            Event("Event_02_ResumeDistorted", "event_resume_distorted", EventTrigger.BeforeResume, 0, 3, 10, "", new[] { "resume_distorted" },
                S("The words on my resume... they look wrong."),
                S("Did I write it like this? I can't remember."));
            Event("Event_03_HRStrange", "event_hr_strange", EventTrigger.BeforeInterview, 0, 4, 10, "", new[] { "hr_strange" },
                L("Narrator", "The room feels smaller than yesterday."),
                S("Why does the fan sound so loud?"));

            // ---------- endings ----------
            var end1 = Ending("Ending_01_AJobIsAJob", "ending_job_is_a_job", "A Job Is a Job", "Accept father's offer",
                "Take the office job. No more rejections.", new Color(0.12f, 0.14f, 0.18f),
                "ENDING 1 - A JOB IS A JOB\nNo more interviews. The mornings just look the same.",
                L("Narrator", "Monday. The office smells like old coffee."),
                S("I escaped the interviews. I didn't escape the routine."),
                L("Narrator", "Every desk looks the same. So does every day."),
                S("It's a job. A job is a job."));
            var end2 = Ending("Ending_02_AnotherMorning", "ending_another_morning", "Another Morning", "Refuse father's offer",
                "Go back to applying. Maybe tomorrow.", new Color(0.18f, 0.12f, 0.1f),
                "ENDING 2 - ANOTHER MORNING\nThe cycle begins again.",
                L("Narrator", "The alarm goes off. The laptop is still open."),
                S("I told him no. I'll find something on my own."),
                L("Narrator", "A new resume. A new list of companies."),
                S("Maybe today is different."));
            var end3 = Ending("Ending_03_MyOwnTerms", "ending_my_own_terms", "My Own Terms", "Follow your own interests",
                "Reject the expected path. Open that old project.", new Color(0.1f, 0.16f, 0.14f),
                "ENDING 3 - MY OWN TERMS\nNothing is guaranteed. But the choice is mine.",
                L("Narrator", "An old folder. An unfinished project."),
                S("I forgot I made this."),
                L("Narrator", "The cursor blinks. The first line is typed."),
                S("It won't be perfect. It will be mine."));

            // ---------- database ----------
            if (dbIsNew)
            {
                db.resumeIntroLines = new[] { S("This is me, on paper."), S("Let's make it better.") };
                db.defaultHrIntro = new[] { HL("Good morning, {name}. Please have a seat.", HREmotion.Neutral) };
                db.hrReviewLines = new[] { HL("Let me look at your resume.", HREmotion.Neutral), HL("{name}, {age} years old. A recent graduate.", HREmotion.Neutral) };
                db.rejectionHR = new[]
                {
                    HL("Thank you for your time, {name}.", HREmotion.Neutral),
                    HL("We've decided to move forward with other candidates.", HREmotion.Disappointed)
                };
                db.rejectionPlayer = new[] { S("...Again."), S("Time to go home.") };
                db.fatherOfferLines = new[]
                {
                    L("Narrator", "The phone is ringing. It's Dad."),
                    L("Father", "{name}, I've been thinking. There's an opening at my friend's company."),
                    L("Father", "Steady pay. Regular hours. You don't have to keep doing this."),
                    S("I... I don't have anything left to apply to."),
                    L("Father", "Then take it. What do you say?")
                };
            }

            AtmosphereTools.GenerateSampleAssets();

            // Fill database lists from everything in Assets/Data (names define the order)
            CollectInto(db);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log("[First Jobber] Sample data ready at " + Root + ". Database lists refreshed.");
        }

        [MenuItem("Tools/First Jobber/Refresh Database Lists")]
        public static void RefreshDatabaseLists()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/GameDatabase.asset");
            if (db == null) { Debug.LogError("No database at " + Root + "/GameDatabase.asset. Run 'Generate Sample Data' first."); return; }
            CollectInto(db);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[First Jobber] Database lists refreshed from " + Root + " (ordered by asset name).");
        }

        public static void CollectInto(GameDatabase db)
        {
            db.categories = Load<ResumeCategoryData>();
            db.choices = Load<ResumeChoiceData>();
            db.companies = Load<CompanyData>();
            db.questions = Load<InterviewQuestionData>();
            db.events = Load<DailyEventData>();
            db.endings = Load<EndingData>();
            db.InvalidateLookups();
        }

        static List<T> Load<T>() where T : Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => System.IO.Path.GetFileNameWithoutExtension(p), System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(a => a != null).ToList();
        }

        // ------------------------------------------------------------ helpers

        static DialogueLine L(string speaker, string text, HREmotion e = HREmotion.Neutral, float auto = 0f)
            => new DialogueLine(speaker, text, e, auto);
        static DialogueLine S(string text) => L(Speakers.Player, text);
        static DialogueLine[] P(string text) => new[] { L(Speakers.Player, text) };
        static DialogueLine HL(string text, HREmotion e) => L(Speakers.HR, text, e);
        static DialogueLine[] H(string text, HREmotion e) => new[] { L(Speakers.HR, text, e) };

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static T GetOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { created = false; return existing; }
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        static ResumeCategoryData Category(string file, string id, string display, string empty)
        {
            var a = GetOrCreate<ResumeCategoryData>(Root + "/Categories/" + file + ".asset", out bool created);
            if (created) { a.id = id; a.displayName = display; a.emptyMessage = empty; EditorUtility.SetDirty(a); }
            return a;
        }

        static ResumeChoiceData Choice(ResumeCategoryData cat, string file, string id, string text, string desc,
            DialogueLine[] player, DialogueLine[] hr)
        {
            var a = GetOrCreate<ResumeChoiceData>(Root + "/Choices/" + file + ".asset", out bool created);
            if (created)
            {
                a.id = id; a.category = cat; a.displayText = text; a.description = desc;
                a.playerReaction = player; a.hrReactions = hr;
                EditorUtility.SetDirty(a);
            }
            return a;
        }

        static CompanyData Company(string file, string id, string name, string job, string salary, string industry, string desc,
            DialogueLine[] player, DialogueLine[] intro)
        {
            var a = GetOrCreate<CompanyData>(Root + "/Companies/" + file + ".asset", out bool created);
            if (created)
            {
                a.id = id; a.companyName = name; a.jobTitle = job; a.salary = salary; a.industry = industry; a.description = desc;
                a.playerReaction = player; a.hrIntro = intro;
                EditorUtility.SetDirty(a);
            }
            return a;
        }

        // answer tuple: id, text, player reaction, HR response, emotion
        static (string id, string text, string player, string hr, HREmotion e) A(string id, string text, string player, string hr, HREmotion e)
            => (id, text, player, hr, e);

        static InterviewQuestionData Question(string file, string id, string text, HREmotion e, ResumeChoiceData choice, ResumeCategoryData cat,
            params (string id, string text, string player, string hr, HREmotion e)[] answers)
        {
            var q = GetOrCreate<InterviewQuestionData>(Root + "/Questions/" + file + ".asset", out bool created);
            if (!created) return q;
            q.id = id; q.questionText = text; q.hrEmotion = e; q.linkedChoice = choice; q.linkedCategory = cat;
            q.answers = answers.Select(a => new InterviewAnswer
            {
                id = a.id,
                text = a.text,
                playerReaction = P(a.player),
                hrResponse = H(a.hr, a.e)
            }).ToList();
            EditorUtility.SetDirty(q);
            return q;
        }

        static DailyEventData Event(string file, string id, EventTrigger trigger, int exactDay, int minDay, int priority,
            string caption, string[] flags, params DialogueLine[] lines)
        {
            var a = GetOrCreate<DailyEventData>(Root + "/Events/" + file + ".asset", out bool created);
            if (created)
            {
                a.id = id; a.trigger = trigger; a.exactDay = exactDay; a.minDay = minDay; a.priority = priority;
                a.caption = caption; a.setFlags = flags; a.lines = lines;
                EditorUtility.SetDirty(a);
            }
            return a;
        }

        static EndingData Ending(string file, string id, string title, string label, string desc, Color bg, string finalText,
            params DialogueLine[] lines)
        {
            var a = GetOrCreate<EndingData>(Root + "/Endings/" + file + ".asset", out bool created);
            if (created)
            {
                a.id = id; a.title = title; a.optionLabel = label; a.optionDescription = desc;
                a.backgroundColor = bg; a.finalText = finalText; a.lines = lines;
                EditorUtility.SetDirty(a);
            }
            return a;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Tools > First Jobber > 12. Generate Sample HR Content.
    /// Creates one HR character per company (own dialogue, own question assets) under Assets/Data/HR and assigns it
    /// to the company ONLY if the company has no HR yet. Existing assets are never overwritten.
    /// All text is placeholder - rewrite it in the Inspector.
    /// </summary>
    public static class SampleHrGenerator
    {
        const string Root = "Assets/Data";

        class Spec
        {
            public string file, id, name, companyFile;
            public Color tint;
            public bool introRandom, rejectionRandom, genderedRejection;
            public string greetM, greetF, greetAny, welcome1, welcome2, review;
            public string[][] rejections;                 // each = lines of one block
            public string[] fixedQ;                        // 3 fixed questions
            public string eduQ, dreamQ, passionQ, gameDevQ, fallbackQ;
            public string good, meh, bad;                  // HR voice per answer quality
            public bool fixedThenLinked = true;           // false: Linked, Fixed
        }

        static readonly Spec[] Specs =
        {
            new Spec
            {
                file = "HR_01_Iris", id = "hr_iris", name = "Iris Park", companyFile = "Company_01_PixelPioneers", tint = new Color(0.55f, 0.6f, 0.85f),
                introRandom = true, rejectionRandom = true,
                greetM = "Good morning, Mr. {name}.", greetF = "Good morning, Ms. {name}.", greetAny = "Good morning, {name}.",
                welcome1 = "Welcome to Pixel Pioneers.", welcome2 = "Please sit. We're a small team and we move fast.",
                review = "I've had a look at your resume.",
                rejections = new[]
                {
                    new[] { "Thank you for coming in.", "We loved the energy, but we need someone who is already shipping." },
                    new[] { "We'll be moving forward with another candidate.", "Keep making things, {name}." },
                },
                fixedQ = new[] { "Tell me about yourself.", "Why Pixel Pioneers?", "What can you ship in three months?" },
                eduQ = "Your education stands out. What did you actually build there?", dreamQ = "Is this dream a plan, or a wish?",
                passionQ = "Everybody has a passion. What have you finished?", gameDevQ = "You want to make games. Show me you understand the cost.",
                fallbackQ = "Is there anything on your resume you want to explain?",
                good = "That's the kind of answer we like to hear.", meh = "Hm. Fair enough.", bad = "We can't work with that."
            },
            new Spec
            {
                file = "HR_02_Gregor", id = "hr_gregor", name = "Gregor Hale", companyFile = "Company_02_Brightline", tint = new Color(0.8f, 0.55f, 0.4f),
                introRandom = false, rejectionRandom = true,
                greetM = "Mr. {name}. Right on time.", greetF = "Ms. {name}. Right on time.", greetAny = "{name}. Right on time.",
                welcome1 = "Brightline Corp has no room for guesswork.", welcome2 = "Let's keep this efficient.",
                review = "Let me see what you brought.",
                rejections = new[]
                {
                    new[] { "That will be all.", "HR will not be contacting you." },
                    new[] { "Not a fit.", "Close the door on your way out." },
                    new[] { "We've seen stronger candidates this week.", "Thank you." },
                },
                fixedQ = new[] { "Summarize your career in one sentence.", "What is your biggest weakness?", "Where are you in five years?" },
                eduQ = "Walk me through your degree, quickly.", dreamQ = "Dreams don't pay invoices. Convince me.",
                passionQ = "A passion is not a qualification. Explain.", gameDevQ = "Games are a luxury market. Why should we care?",
                fallbackQ = "Give me one concrete result you produced.",
                good = "Acceptable. Concise.", meh = "Noted.", bad = "That was a waste of my time.", fixedThenLinked = false
            },
            new Spec
            {
                file = "HR_03_Helena", id = "hr_helena", name = "Helena Gray", companyFile = "Company_03_GrayTower", tint = new Color(0.5f, 0.5f, 0.55f),
                introRandom = true, rejectionRandom = false, genderedRejection = true,
                greetM = "Good afternoon, Mr. {name}.", greetF = "Good afternoon, Ms. {name}.", greetAny = "Good afternoon, {name}.",
                welcome1 = "Gray Tower Bank values stability above all.", welcome2 = "Please take a seat. This will be brief.",
                review = "Your file is... interesting.",
                rejections = new[] { new[] { "Our decision is final.", "We wish you every success elsewhere." } },
                fixedQ = new[] { "Why a bank?", "How do you handle pressure?", "Describe a time you followed a rule you disagreed with." },
                eduQ = "Your academic history is unconventional.", dreamQ = "This dream does not match our culture.",
                passionQ = "Is this passion compatible with a career in finance?", gameDevQ = "Games. In a bank. Help me understand.",
                fallbackQ = "What do you think we do here?",
                good = "That is a reasonable position.", meh = "I see.", bad = "That is not what we are looking for."
            },
            new Spec
            {
                file = "HR_04_Noel", id = "hr_noel", name = "Noel Archer", companyFile = "Company_04_NovaSoft", tint = new Color(0.4f, 0.75f, 0.6f),
                introRandom = false, rejectionRandom = true,
                greetM = "Hey Mr. {name}! Great to meet you.", greetF = "Hey Ms. {name}! Great to meet you.", greetAny = "Hey {name}! Great to meet you.",
                welcome1 = "Welcome to Nova Soft!", welcome2 = "Grab a coffee. We're casual here.",
                review = "Cool, cool. Let's see the resume.",
                rejections = new[]
                {
                    new[] { "Really enjoyed this!", "Sadly it's a no for now." },
                    new[] { "You were great, honestly.", "We just went another way." },
                },
                fixedQ = new[] { "So, who are you?", "What do you do for fun?", "What would your friends say about you?" },
                eduQ = "Tell me about school. Any good stories?", dreamQ = "Love a big dream. How big are we talking?",
                passionQ = "What's the thing you could talk about all night?", gameDevQ = "Making games, huh? What's your favorite one?",
                fallbackQ = "Anything you want to add that isn't on the page?",
                good = "Love that!", meh = "Yeah, okay.", bad = "Hmm. That's not really landing."
            },
        };

        [MenuItem("Tools/First Jobber/12. Generate Sample HR Content")]
        public static void Generate()
        {
            EnsureFolder(Root + "/HR");
            EnsureFolder(Root + "/HR/Questions");

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/GameDatabase.asset");
            var edu = FindById<ResumeCategoryData>("education");
            var dream = FindById<ResumeCategoryData>("dream");
            var passion = FindById<ResumeCategoryData>("passion");
            var gameDev = FindById<ResumeChoiceData>("dream_game_dev");

            foreach (var s in Specs) Build(s, edu, dream, passion, gameDev);

            AssetDatabase.SaveAssets();
            if (db != null)
            {
                SampleDataGenerator.CollectInto(db);
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("[First Jobber] Sample HR content ready in " + Root + "/HR. Run 'Validate Content' next.");
        }

        static void Build(Spec s, ResumeCategoryData edu, ResumeCategoryData dream, ResumeCategoryData passion, ResumeChoiceData gameDev)
        {
            var hr = GetOrCreate<HRCharacterData>(Root + "/HR/" + s.file + ".asset", out bool created);
            var company = AssetDatabase.LoadAssetAtPath<CompanyData>(Root + "/Companies/" + s.companyFile + ".asset");

            if (created)
            {
                string q = s.id.Replace("hr_", "");
                hr.id = s.id; hr.hrName = s.name; hr.company = company; hr.description = "Placeholder HR for " + s.companyFile + ".";
                hr.emotionVisuals = new List<HREmotionVisual>
                {
                    new HREmotionVisual { emotion = HREmotion.Neutral, tint = s.tint },
                    new HREmotionVisual { emotion = HREmotion.Interested, tint = Color.Lerp(s.tint, Color.green, 0.25f) },
                    new HREmotionVisual { emotion = HREmotion.Disappointed, tint = Color.Lerp(s.tint, Color.red, 0.3f) },
                    new HREmotionVisual { emotion = HREmotion.Uncomfortable, tint = Color.Lerp(s.tint, Color.yellow, 0.3f) },
                };

                // ---- dialogue
                hr.introMode = s.introRandom ? DialoguePlaybackMode.RandomOne : DialoguePlaybackMode.Sequential;
                var intro = new List<DialogueLine>();
                if (s.introRandom)
                {
                    AddGenderGroup(intro, "intro_a", s.greetM, s.greetF, s.greetAny, s.welcome1);
                    AddGenderGroup(intro, "intro_b", s.greetM, s.greetF, s.greetAny, s.welcome2);
                }
                else
                {
                    AddGenderGroup(intro, "greeting", s.greetM, s.greetF, s.greetAny, null);
                    intro.Add(Block(Speakers.HR, HREmotion.Neutral, s.welcome1, s.welcome2));
                }
                hr.introduction = intro.ToArray();
                hr.resumeReviewMode = DialoguePlaybackMode.Sequential;
                hr.resumeReview = new[] { Block(Speakers.HR, HREmotion.Neutral, s.review) };

                hr.rejectionMode = s.rejectionRandom ? DialoguePlaybackMode.RandomOne : DialoguePlaybackMode.Sequential;
                var rej = new List<DialogueLine>();
                if (s.genderedRejection)
                {
                    AddGenderGroup(rej, "rej_title", "Our regrets, Mr. {name}.", "Our regrets, Ms. {name}.", "Our regrets, {name}.", null);
                }
                foreach (var lines in s.rejections) rej.Add(Block(Speakers.HR, HREmotion.Disappointed, lines));
                hr.rejection = rej.ToArray();

                // ---- questions (each HR owns its assets)
                var fixedQs = new List<InterviewQuestionData>();
                for (int i = 0; i < s.fixedQ.Length; i++)
                    fixedQs.Add(Question(s, q + "_fixed_" + (i + 1), s.fixedQ[i], null, null));
                hr.fixedQuestions = fixedQs;

                hr.resumeLinkedQuestions = new List<InterviewQuestionData>
                {
                    Question(s, q + "_edu", s.eduQ, null, edu),
                    Question(s, q + "_dream", s.dreamQ, null, dream),
                    Question(s, q + "_passion", s.passionQ, null, passion),
                };
                if (gameDev != null)
                {
                    var exact = Question(s, q + "_dream_gamedev", s.gameDevQ, gameDev, null);
                    exact.weight = 2; // not needed for exact matches (they always win) - kept as an example of priority
                    EditorUtility.SetDirty(exact);
                    hr.resumeLinkedQuestions.Insert(0, exact);
                }
                hr.fallbackQuestion = Question(s, q + "_fallback", s.fallbackQ, null, null);

                hr.sequence = new List<InterviewSlot>
                {
                    new InterviewSlot { type = s.fixedThenLinked ? InterviewSlotType.Fixed : InterviewSlotType.ResumeLinked },
                    new InterviewSlot { type = s.fixedThenLinked ? InterviewSlotType.ResumeLinked : InterviewSlotType.Fixed },
                };
                EditorUtility.SetDirty(hr);
            }

            if (company != null && company.hr == null)
            {
                company.hr = hr;
                EditorUtility.SetDirty(company);
            }
        }

        // ------------------------------------------------------------ helpers

        // Three alternatives in one variantGroup: male / female / everyone else (fallback).
        static void AddGenderGroup(List<DialogueLine> list, string group, string male, string female, string any, string extraLine)
        {
            DialogueLine V(string text, params string[] genders)
            {
                var b = Block(Speakers.HR, HREmotion.Neutral, extraLine == null ? new[] { text } : new[] { text, extraLine });
                b.variantGroup = group;
                b.genderIds = genders;
                return b;
            }
            list.Add(V(male, "male"));
            list.Add(V(female, "female"));
            list.Add(V(any));
        }

        static DialogueLine Block(string speaker, HREmotion emotion, params string[] lines)
        {
            return new DialogueLine { speaker = speaker, emotion = emotion, dialogueLines = new List<string>(lines), genderIds = new string[0] };
        }

        static InterviewQuestionData Question(Spec s, string idSuffix, string text, ResumeChoiceData choice, ResumeCategoryData category)
        {
            var q = GetOrCreate<InterviewQuestionData>(Root + "/HR/Questions/Q_" + idSuffix + ".asset", out bool created);
            if (!created) return q;
            q.id = "q_" + idSuffix; q.questionText = text; q.hrEmotion = HREmotion.Neutral;
            q.linkedChoice = choice; q.linkedCategory = category;
            q.answers = new List<InterviewAnswer>
            {
                Answer("a1", "I'll be honest with you.", "Honesty is all I have left.", s.meh, HREmotion.Neutral),
                Answer("a2", "I've prepared for exactly this.", "Rehearsed, but it should hold.", s.good, HREmotion.Interested),
                Answer("a3", "I'm not sure what to say.", "My mind went blank.", s.bad, HREmotion.Disappointed),
            };
            EditorUtility.SetDirty(q);
            return q;
        }

        static InterviewAnswer Answer(string id, string text, string player, string hrLine, HREmotion e)
        {
            return new InterviewAnswer
            {
                id = id, text = text,
                playerReaction = new[] { new DialogueLine(Speakers.Player, player) },
                hrResponse = new[] { Block(Speakers.HR, e, hrLine, "Let's move on.") }
            };
        }

        static T FindById<T>(string id) where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root }))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a == null) continue;
                var so = new SerializedObject(a);
                var p = so.FindProperty("id");
                if (p != null && p.stringValue == id) return a;
            }
            return null;
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

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}

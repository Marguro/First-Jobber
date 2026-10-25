# First Jobber — Content & Tuning Guide

How to turn the sample content into a real game without touching C#.
Everything below is edited in the Unity Inspector. Companion docs: `GAME_SETUP_TH.md` (architecture, Thai).

---

## 0. The 60-second mental model

```
GameDatabase  (one asset: Assets/Data/GameDatabase.asset)
 ├─ categories   Education, Dream, Passion ...        (what the resume has)
 ├─ choices      each belongs to ONE category          (the options per category)
 ├─ companies                                           (where you can apply)
 ├─ questions    each may link to a choice/category    (interview bank)
 ├─ events                                              (daily events)
 ├─ endings                                             (final decision options)
 └─ shared dialogue + settings
```

Rules of the loop that decide how long your game is:

- Every rejection consumes **1 company** and **1 choice in each category that still has choices**.
- The game goes to the father's offer when **no company is left** OR **every category is empty**.
- Therefore: **number of applications = min(number of companies, size of the biggest category)**.
  - 4 companies, categories of 4/4/4 → 4 applications → father's offer on day 5.
  - 6 companies, categories of 4/4/4 → still 4 applications (choices run out first). Add choices *and* companies to lengthen the game.
  - A category smaller than the others simply drops out when empty; the player is never forced to pick from it.
- Nothing in the code says "4 days". Content decides.

Golden rules:

1. **IDs are permanent.** Every `id` must be unique and never change after you start testing (saves, history and links use it).
2. **The database only knows what is in its lists.** A new asset does nothing until it is in `GameDatabase` (see 1.2).
3. **Run `Tools > First Jobber > 3. Validate Content`** after every editing session. It reports duplicate ids, missing references, empty dialogue and bad links.

---

## 1. Everyday workflow

### 1.1 Create an asset
Project window → right-click inside `Assets/Data/<Type>/` → `Create > First Jobber > ...`

| Menu item | Folder (convention) |
|---|---|
| Resume Category | `Assets/Data/Categories` |
| Resume Choice | `Assets/Data/Choices` |
| Company | `Assets/Data/Companies` |
| Interview Question | `Assets/Data/Questions` |
| Daily Event | `Assets/Data/Events` |
| Ending | `Assets/Data/Endings` |
| Atmosphere Phase / Configuration | `Assets/Data/Atmosphere` |

### 1.2 Register it in the database (two ways)
- **Automatic:** `Tools > First Jobber > Refresh Database Lists`. It collects everything under `Assets/Data` and sorts each list by **asset file name**. So name files with a number prefix: `Edu_05_MyUniversity`, `Q_Dream_Writer_01`. File name order = display order and tie-break order.
- **Manual:** select `GameDatabase.asset` and drag the asset into the matching list. List order = display order.

> Atmosphere phases are NOT in the database; they go in `AtmosphereConfiguration.phases`.

### 1.3 Safe editing rules
- Edit assets freely; they are never modified while playing.
- Deleting a choice/company that a saved game has already consumed is harmless (stale ids are ignored), but **delete `first_jobber_save.json`** (inside `Application.persistentDataPath`; in the Editor use *Window > Analysis > ... / Show Persistent Data Path*, or search for the file) whenever you rename ids, so old saves cannot confuse testing.
- **Do not re-run `2. Build Game Scene` after you restyle the UI** — it replaces `Game.unity`. Duplicate the scene first, or only use it once. Use `4. Add Atmosphere To Open Scene` for atmosphere on custom scenes.

---

## 2. Resume content

### 2.1 Add a choice to an existing category
1. Create `Resume Choice`, name it with the right prefix, e.g. `Edu_05_StateCollege`.
2. Fill in:
   - `id` — e.g. `edu_state_college` (unique, lowercase, no spaces).
   - `category` — drag the category asset. **If this is empty the choice is invisible** and the validator warns.
   - `displayText` — the button title.
   - `description` — the smaller line under the title (optional).
   - `tags` — optional keywords, used by `requiredTag` on questions (e.g. `prestigious`, `creative`).
   - `playerReaction` — what the player thinks when clicking it (shown in the bottom box, auto-advances after ~2 s).
   - `hrReactions` — what HR says about it in the interview (speaker **HR**, pick an emotion).
3. `Refresh Database Lists`.

Each choice should have both `playerReaction` and `hrReactions`. Without `hrReactions` HR says nothing about that pick; without `playerReaction` clicking is silent.

### 2.2 Add a new category (e.g. Skills)
1. `Create > First Jobber > Resume Category`: `Cat_04_Skills`, `id = skills`, `displayName = Skills`, `emptyMessage` (shown when nothing is left).
2. Create at least 2–4 choices pointing at it (2.1).
3. Refresh lists. The resume preview and editor show it automatically; the confirm button now requires a pick from it while it still has options.
4. Add questions for it (section 4).

Category order on screen = order in `database.categories` (file-name order after a refresh).
The preview list scrolls, so 5–6 categories fit fine.

### 2.3 Remove / hide a choice or category
Remove it from the database list (the asset can stay in the project). A category with no choices is skipped automatically.

### 2.4 Settings that change how choices look
`GameDatabase`:
- `showConsumedChoices` — OFF (default): used choices vanish (the horror effect: options disappear). ON: they stay as greyed, struck-through entries.
- `showRejectedCompanies` — ON (default): rejected companies stay visible as `REJECTED`. OFF: they vanish.

### 2.5 Pacing advice (the core of the horror)
- The fewer options remain, the more each one should hurt. Write **hrReactions that get meaner or sadder** for the choices likely to be last.
- Give the *last remaining* choice of each category a distinct playerReaction ("This is all I have left.").
- Different category sizes (e.g. 5/4/3) make categories vanish one by one — a cheap, strong effect.

---

## 3. Companies

`Create > First Jobber > Company`:

| Field | Notes |
|---|---|
| `id`, `companyName`, `jobTitle`, `salary`, `industry`, `description` | Shown in the list row (title / job / salary \| industry) and in the details card |
| `logo` | Sprite shown as the row icon (optional; row works without it) |
| `tags` | Used by `requiredTag` on questions |
| `playerReaction` | Player thought when clicking the company |
| `hrIntro` | Optional. HR's opening for this company. Empty → `GameDatabase.defaultHrIntro` |
| `rejection` | Optional. This company's rejection speech. Empty → `GameDatabase.rejectionHR` |

Tips:
- Use `hrIntro`/`rejection` to give each company a personality (cold bank, chaotic startup).
- Company-specific questions: set `requiredCompany` on the question (section 4.4).
- The number of companies caps the game length (section 0).

---

## 4. Interview questions

### 4.1 How questions are picked (exactly)
At the start of every interview the game collects the player's selected choices and the company, then:

1. Throws away questions whose `requiredCompany` / `requiredTag` fail.
2. Builds three groups, in this priority order:
   - **Exact:** `linkedChoice` is one of the picked choices.
   - **Category:** `linkedCategory` set, no `linkedChoice`, and the player picked any choice of that category.
   - **Generic:** both links empty.
3. Inside each group sort by `weight` (high first), ties by database order (file name) → **deterministic**.
4. Picks the first `questionsPerInterview` (default 2) that were **not asked in earlier interviews**; if there aren't enough fresh ones it allows repeats; if the bank is empty a built-in emergency question is used (never softlocks).

Consequences you must plan for:
- Exact questions always beat category questions, which always beat generic ones. With 2 questions per interview and three picked choices that each have an exact question, only the **two highest-weight** ones are asked. Use `weight` to decide which matters most.
- Keep **at least `questionsPerInterview` generic questions** as a safety net (validator checks this).
- You can raise `questionsPerInterview` (e.g. 3) for longer interviews.

### 4.2 Create a question linked to one choice
1. `Create > First Jobber > Interview Question`, name `Q_Dream_Writer_01`.
2. `id = q_dream_writer_01`, `questionText` ("Writers rarely get paid. Why should we hire you?").
3. `hrEmotion` — HR's face while asking.
4. `linkedChoice` — drag the choice asset (e.g. Writer). Leave `linkedCategory` empty (or set to the same category; a mismatch is flagged by the validator).
5. `weight` — higher = asked first. Default 1.
6. Add 2–4 **answers** (see 4.5).

### 4.3 Link to a whole category / make it generic
- Category question: set only `linkedCategory`. Asked when the player picked anything in that category.
- Generic: leave both empty. Asked as fallback, any resume.

### 4.4 Conditions (optional)
- `requiredCompany` — only for that company.
- `requiredTag` — needs the tag on **a selected choice OR the company**. Example: tag `creative` on Artist/Writer/Music, then one question with `requiredTag = creative` covers all of them.

### 4.5 Answers
Each answer has:
- `id` — unique **within the question** (`a1`, `a2`, ...).
- `text` — the button text.
- `playerReaction` — player's thought after choosing.
- `hrResponse` — HR's reply. **Set `speaker = HR` and an `emotion`** on every line; that is what changes HR's face.

Notes:
- Answers have no gameplay effect (the interview always ends in rejection) — their job is tone. Make them read like real options with different personalities, and let HR's reply react specifically.
- 3 answers per question is a good size.
- Answer ids are what the game stores; keep them stable.

### 4.6 How many questions do I need for a "real" game?
For a 4-loop game with 3 categories × 4 choices:
- 1 exact question per choice (12) — minimum, 2 for the important ones.
- 2 category questions per category (6).
- 4–6 generic questions.
- Check variety: across 4 interviews the game asks 8 questions; players should rarely see a repeat.

### 4.7 HR resume review (what HR says before questions)
Order in each interview: company `hrIntro` (or default) → `database.hrReviewLines` → each selected choice's `hrReactions` (category order) → questions → rejection.
Edit `hrReviewLines` for the "let me read your resume" lines. Tokens work (4.9).

---

## 5. Dialogue authoring (applies everywhere)

A **Dialogue Line** has:

| Field | Meaning |
|---|---|
| `speaker` | `Player` (shown as the applicant's name), `HR`, `Father`, `Narrator`, or any text |
| `text` | The line. Supports tokens |
| `emotion` | Only used when speaker is `HR` |
| `autoAdvanceDelay` | `>0`: moves on by itself after N seconds. `0`: waits for click / Space / Enter |
| `delayBefore` | Pause before the line appears — use for dread |
| `eventId` | Advanced: raised through `DialogueManager.LineEvent` (hook for custom triggers) |
| `portrait` | Optional small image in the dialogue box |

### 4.9 Tokens (typed inside text)
`{name}` `{age}` `{day}` `{company}` `{job}`
(`{company}` and `{job}` only make sense after a company is selected. Unknown tokens are left as-is.)

### Behaviour you should know
- A click while text is typing completes it; the next click advances (with a 0.2 s guard against double-clicks).
- Starting a new dialogue replaces the old one — rapid clicking on choices never stacks reactions.
- Resume-choice and company reactions auto-advance after ~2 s even if the line has no delay. Interview/event/ending lines wait for a click unless you set `autoAdvanceDelay`.
- Typing speed: `DialogueManager.charactersPerSecond` (scene → GameSystems).

### Writing tips
- 1–2 short lines per reaction. Players click through ~100+ lines per run.
- Use `delayBefore` 0.5–1.2 s before the cruelest HR line.
- Let `Player` lines *repeat* motifs ("Again.") — the loop is the horror.

---

## 6. Shared story beats (GameDatabase)

| Field | When it plays |
|---|---|
| `resumeIntroLines` | Right after creating the applicant, once resume slides left |
| `defaultHrIntro` | Start of each interview (unless the company has its own) |
| `hrReviewLines` | HR starts reading the resume |
| `rejectionHR` | Rejection (unless the company has its own) |
| `rejectionPlayer` | Player's reaction after the rejection |
| `fatherOfferLines` | The father's phone call at the end |

Idea for a "real" feel: rejections are the same every day on purpose; use **company `rejection`** and **Daily Events** to vary them, or add later-day wording through events (section 7).

---

## 7. Daily Events

Events are data. The game checks for eligible events at fixed moments called **triggers**.

### 7.1 Trigger timing (when exactly)

| Trigger | Moment | Shown on | Day number |
|---|---|---|---|
| `OnDayStart` | After the sleep blackout, before the resume editor. Not on day 1 | Dedicated Daily Event screen (caption + background) | The **new** day |
| `BeforeResume` | Resume editor just opened (after the intro on day 1), list locked until it ends | Resume screen | Current day |
| `AfterResumeConfirmed` | Player pressed Confirm Resume, before the company list | Resume screen | Current day |
| `AfterCompanySelected` | After Apply, before the interview | Company screen | Current day |
| `BeforeInterview` | Immediately before the interview starts | Company screen | Current day |
| `AfterInterview` | Last answer done, before the rejection speech | Interview screen | Current day |
| `AfterRejection` | Rejection finished and **today's choices are already consumed** | Interview screen | Still today (old day) |
| `BeforeSleep` | Just before the screen fades to black | Interview screen | Still today (old day) |

So "day 3" content: use `OnDayStart` + `exactDay = 3` to trigger when day 3 *begins*. Use `AfterRejection`/`BeforeSleep` + `exactDay = 2` for something at the *end* of day 2.

### 7.2 Create an event
`Create > First Jobber > Daily Event`:

| Field | Meaning |
|---|---|
| `id` | Unique |
| `trigger` | From the table above |
| `exactDay` | `0` = ignore; otherwise only on exactly this day |
| `minDay` | `0` = ignore; otherwise on this day **or later** |
| `repeatable` | OFF: plays once per playthrough. ON: plays every time conditions match |
| `priority` | Higher first when several events qualify at the same trigger |
| `requiredFlag` / `forbiddenFlag` | Needs / must not have this flag |
| `setFlags` | Flags raised when it runs |
| `caption`, `background` | Only for `OnDayStart` (the title card / backdrop) |
| `flickerSeconds` | Temporary screen flicker (does not change atmosphere) |
| `lines` | The dialogue |

Add it to `database.events` (Refresh).

### 7.3 Flags (the glue)
A flag is just a string stored in the playthrough. Use them to chain events or to switch effects.

Built-in flags with visual effects:
- `resume_distorted` — resume header text glitches and jitters.
- `hr_strange` — HR sways and jitters, fan spins 3× faster, screen flickers more often.

Flags stay on for the rest of the playthrough (a new game clears them).

Chaining example — "mom calls only if the player ignored dad":
- Event A: `setFlags = [ignored_dad]` on day 2.
- Event B: `requiredFlag = ignored_dad`, day 4.

### 7.4 Recipes
- **Father texts on day 2:** `trigger=OnDayStart, exactDay=2, caption="Day 2"`, lines from `Narrator`/`Father`.
- **Resume distorts from day 3:** `trigger=BeforeResume, minDay=3, setFlags=[resume_distorted]`.
- **HR gets strange from day 4:** `trigger=BeforeInterview, minDay=4, setFlags=[hr_strange]`.
- **A nightmare after the 2nd rejection:** `trigger=BeforeSleep, exactDay=2, flickerSeconds=1.5`, lines from `Narrator`.
- **Repeating dread line each interview from day 3:** `trigger=BeforeInterview, minDay=3, repeatable=ON`.

Atmosphere (permanent look by day) is separate: see section 9.

### 7.5 Things events do NOT do
- They cannot change which choices/companies exist.
- They cannot branch the story on past picks (no condition on specific choices yet — use flags set by events as a proxy).
- They do not run on day 1 for `OnDayStart` (the game starts straight in the editor).

---

## 8. Endings

### 8.1 What the player sees
After the father's call (`fatherOfferLines`), a list of **all endings in `database.endings`** appears. All are always selectable. Order = list order.

### 8.2 Edit an ending (`Ending` asset)
| Field | Meaning |
|---|---|
| `id` | Unique |
| `title` | Big title on the ending screen |
| `optionLabel` / `optionDescription` | Button text on the final decision list |
| `lines` | Ending narration/dialogue (speaker `Narrator`, `Player`...) |
| `background` | Sprite (fallback: `backgroundColor`) |
| `finalText` | Large text shown after the dialogue, with Restart / Main Menu |
| `sound` | One-shot sound at the start |

### 8.3 Add or remove an ending
Create a 4th `Ending`, add it to `database.endings` → it appears automatically (no code). The validator warns if the count isn't 3 only as a reminder of the design.
Removing = take it out of the list.

### 8.4 The father's offer
Edit `database.fatherOfferLines`. Typical shape: narrator sets the scene → father speaks → player admits there's nothing left → father asks for an answer. The last line should lead naturally into the three buttons.

### 8.5 Limits
- Endings do not currently change text based on history (e.g. which companies were rejected). If you need that, set flags in events and ask for a small code extension.
- Restart → new applicant creation. Main Menu → returns to the menu; "Continue" is hidden after an ending because the save is deleted.

---

## 9. Atmosphere (look & sound by day)

Full details in `GAME_SETUP_TH.md` §10. Quick version:

1. Each `Atmosphere Phase` has `startDay`; the active one is the **highest startDay ≤ today**.
2. Add the phase to `AtmosphereConfiguration.phases`.
3. Colour/darkness: `overlayColor` + `overlayOpacity`; `uiTint`; `cameraColor`; `hrTint`.
4. Sprites per phase: put an **Atmosphere Sprite Binding** (with a Target Id) on any Image, then in the phase add `spriteOverrides` with the same Target Id.
5. HR portrait per phase: `hrSprites` (emotion → sprite).
6. Audio: `ambientClip`, `ambientVolume`, `audioCrossfade`.
7. Preview in Play Mode: select `GameSystems` → AtmosphereManager → Preview buttons.

Suggested progression for 4 loops: Normal (day 1–2) → Deterioration (day 3) → Despair (day 4+). Make the change start **one day before** the story gets worst so the player feels it before they understand it.

---

## 10. Turning the mockup into a real game

### 10.1 Art checklist (all optional, nothing breaks without them)

| Asset | Where to assign |
|---|---|
| HR portraits (4 emotions) | Scene → `InterviewPanel/HRArea` → HRReactionController → `visuals` (sprite per emotion) |
| HR variants per atmosphere | Phase asset → `hrSprites` |
| Ceiling fan, ambient props | Replace the Images inside `InterviewPanel/HRArea/CeilingFan` (keep the `CeilingFan` object as the rotating root) |
| Company logos | Company → `logo` |
| Event title-card backgrounds | Daily Event → `background` |
| Ending backgrounds | Ending → `background` |
| Backgrounds / paper / panels | Scene Images (`Background`, `ResumePreview`, `DetailsCard`, `DialogueBox`...). Keep their component references |
| Choice button look | Edit `Assets/Prefabs/ChoiceButton.prefab` (keep the `ChoiceButtonUI` references: button, background, title, subtitle, icon). Used by every list |
| Buttons | Replace sprites on the Button Images |
| Per-phase backgrounds | Atmosphere Sprite Binding + phase `spriteOverrides` |

Rules when restyling: keep the GameObject names and component references, change sprites/colors/fonts/sizes freely. Use Anchors, not fixed positions.

### 10.2 Fonts (important for Thai or stylised text)
- The default TMP font (Liberation Sans) has **no Thai glyphs**. For Thai text: *Window > TextMeshPro > Font Asset Creator*, build a font asset from a Thai font (e.g. Sarabun), set **Atlas Population Mode = Dynamic**, then assign it as the default in *Project Settings > TextMeshPro > Settings > Default Font Asset* (or per text object). Test every panel after switching.
- Keep dialogue legible: size ≥ 32 at 1920×1080.

### 10.3 Audio (what exists vs what you add)
- Exists: per-phase ambience (crossfade) and `Ending.sound`.
- Not included yet: UI click SFX, typewriter blips, HR voice, music. They are simple additions (an AudioSource triggered from button clicks / `DialogueManager.LineShown`); ask when you want them.
- The project already contains a `Casual Game Sounds U6` pack you can use for clicks.

### 10.4 Game length target (10–15 min)
Rough budget per loop: resume 1.5 min, company 0.5, interview 2–2.5, rejection+sleep 0.5. Four loops ≈ 10–14 min plus intro/ending. If playtests run short, raise `questionsPerInterview` to 3 or add events rather than more loops.

### 10.5 Language
Everything the player reads comes from assets plus a few strings in code (button labels like "Confirm Resume", "Start Game", error messages, "Day N is over..."). Those are in `GameSceneBuilder.cs` (scene text — editable directly in the scene after building) and `CreateApplicantController.cs`/`GameFlowManager.cs` (validation messages, sleep caption). Scene button labels can be changed in the Hierarchy.

### 10.6 Balancing & difficulty
There is no "failing" — tone is the whole game. What to tune:
- How fast options disappear (more categories = faster feeling of loss).
- How cruel HR is per choice (hrReactions).
- When events/atmosphere kick in (startDay / minDay).

---

## 11. Pre-release checklist

1. `Tools > First Jobber > 3. Validate Content` → no warnings (or only ones you accept).
2. Play a full run and a **Continue** run (quit mid-day 3, relaunch, Continue).
3. Play with each category order: pick the same choices every day, then pick randomly.
4. Check each of the 3 endings, then Restart → applicant screen, then play day 1 again (atmosphere back to Normal, no leftover flags).
5. Console clean in a full run (no `[ChoiceListUI]`, `[GameFlow]`, `[HR]` warnings).
6. Test at 16:9 plus 16:10 and a narrow window (the layout uses anchors).
7. Build: *File > Build Profiles*, scene `Game` is in the list (the builder adds it).

---

## 12. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| New choice doesn't show | Not in `database.choices`, or `category` empty/not in `database.categories`, or its id duplicates another → Validate + Refresh |
| Game ends earlier than expected | A pool ran out: companies or the biggest category. See §0 |
| Game won't end / extra loops | You added companies *and* choices; length = min of both |
| Same question every interview | Not enough questions; add more exact/category/generic ones (§4.6) |
| Linked question never appears | Link points to a different choice than expected; or higher-weight exact questions fill both slots; or `requiredCompany/Tag` fail |
| HR face doesn't change | HR line has `speaker` other than exactly `HR`; or no visual for that emotion in HRReactionController |
| Event never fires | Wrong trigger/day (see timing table), `repeatable` off and already used, flag condition not met, not in `database.events`, or it is `OnDayStart` for day 1 |
| Event fires too early | `minDay` vs `exactDay`; remember `AfterRejection`/`BeforeSleep` still see the old day number |
| Text shows `{xyz}` | Unknown token (valid: name, age, day, company, job) |
| Empty/odd boxes after restyling | A reference in a controller was cleared; select the object and check missing fields (GameFlowManager logs the missing ones at start) |
| Scene lost custom edits | You re-ran *Build Game Scene*. Restore from version control / the duplicated scene |
| Thai text shows squares | Missing Thai glyphs in the TMP font (§10.2) |
| Old save behaves oddly | Delete `first_jobber_save.json` in `Application.persistentDataPath` |

# First Jobber — How To Change Content (Quick Cookbook)

Task-based. Find what you want to change, follow the steps. Everything is done in the Unity Inspector, no C# needed.
Deeper reference: `CONTENT_GUIDE.md` (older, general) and `GAME_SETUP_TH.md` (Thai, architecture + new systems).

> After every editing session run **Tools > First Jobber > 3. Validate Content** and read the Console.
> Press **Ctrl+S** to save assets. Never rename an `id` once you have started testing.

---

## 0. Where things live

| What | Where |
|---|---|
| Master list of everything | `Assets/Data/GameDatabase.asset` |
| Resume categories / choices | `Assets/Data/Categories`, `Assets/Data/Choices` |
| Companies | `Assets/Data/Companies` |
| HR characters + their questions | `Assets/Data/HR`, `Assets/Data/HR/Questions` |
| Old shared question bank | `Assets/Data/Questions` |
| Daily events / endings | `Assets/Data/Events`, `Assets/Data/Endings` |
| Atmosphere phases | `Assets/Data/Atmosphere` |

Create an asset: right-click in the folder > `Create > First Jobber > ...`.
Then make sure it is in the database: **Tools > First Jobber > Refresh Database Lists** (collects everything under `Assets/Data`).

---

## 1. Writing dialogue (applies to every dialogue field)

Every dialogue field is a list of **blocks**. One block = one speaker saying one or more pages.

```
Element 0   (a block)
  Speaker, Emotion, Portrait, Auto Advance Delay, Delay Before, Event Id
  Gender Ids, Variant Group          <- optional, see section 2
  Dialogue Lines [3]                 <- each line = one page, player clicks between them
      Line 0: "That's the best I can make it."
      Line 1: "I've sent so many applications already."
      Line 2: "Now, who will take me?"
```

- **Add a page:** open the block > **Dialogue Lines** > **+ Add Line**. Use the arrows to reorder and **X** to delete.
- **Add another speaker / emotion:** add a new block (a new list element).
- **Speaker:** `Player` (shows the applicant name), `HR` (shows the HR name of the company), or any text (`Father`, `Narrator`).
- **Emotion** only matters when speaker is `HR`: it changes the HR expression.
- **Auto Advance Delay:** `0` = wait for a click. `> 0` = continue by itself after that many seconds.
- **Legacy Text:** old content has a single `Text` field. It still works while Dialogue Lines is empty. Use the **Move into Dialogue Lines** button to convert it.
- **Tokens** you can type inside text: `{name}` or `{PlayerName}`, `{gender}`, `{company}`, `{job}`, `{day}`, `{hr}`.

### Random one block instead of all
Fields in `GameDatabase` that have a **Mode** next to them (Resume Intro, Resume Confirmed, Company Confirmed, Default Hr Intro, Hr Review, Rejection HR, Rejection Player, Father Offer) and HR dialogue fields can be:

- **Sequential**: play every block in order.
- **RandomOne**: pick **one** block at random each time, play all its lines in order. Repeats are allowed.

Set it by changing the **Mode** dropdown. Change it back to Sequential any time. Fields without a Mode (events, endings, choice reactions) are always sequential.

---

## 2. Different dialogue for male / female / other

Use this for things like "Mr." / "Ms.".

1. Make three blocks in the same dialogue field.
2. Give all three the same **Variant Group**, e.g. `greeting`.
3. Block 1: **Gender Ids** = `male` → "Good morning, Mr. {name}."
4. Block 2: **Gender Ids** = `female` → "Good morning, Ms. {name}."
5. Block 3: leave **Gender Ids** empty → "Good morning, {name}." (this is the fallback for everyone else)

Rules:
- A block with **no Gender Ids and no Variant Group** is played for everybody. You never have to duplicate shared text.
- A block with Gender Ids but **no Variant Group** is simply skipped for other genders.
- Gender only changes wording. It never changes confidence or the rejection.
- Works with RandomOne: gender filtering happens first, then one block is picked.

### Add or rename gender options
`GameDatabase.asset` > **Genders**. Each has an `id` (lowercase, never change it later, e.g. `male`) and a `displayName` (shown in the dropdown). Keep one with id `unspecified`. Order in the list = order in the dropdown. **Gender Prompt** is the first, unselectable dropdown text.

---

## 3. Resume content

**Add a choice**
1. `Create > First Jobber > Resume Choice` in `Assets/Data/Choices`.
2. Fill: `Id`, `Category`, `Display Text`, `Description`.
3. **Confidence Percentage** (0–100): how strong this choice is. This feeds the confidence bar.
4. **Player Reaction**: what the player says when picking it.
5. **Hr Reactions**: default HR reaction. (An HR can override it, see section 5.)
6. Refresh Database Lists.

**Add a category** (e.g. Skills): `Create > First Jobber > Resume Category`, give it an `Id` and a name, add at least one choice, refresh the database.

**Remove a choice:** delete it from `GameDatabase > Choices` (or delete the asset). Check Validate Content for questions that still link to it.

**Game length** comes from content: number of applications = `min(number of companies, size of the biggest category)`. Add companies **and** choices to lengthen the game.

---

## 4. Companies and HR characters

### Add a company
1. `Create > First Jobber > Company`. Fill `Id`, `Company Name`, `Job Title`, `Salary`, `Industry`, `Description`.
2. **Player Reaction**: what the player says when applying.
3. Assign an **Hr** (next step). Refresh Database Lists.

### Create an HR character
1. `Create > First Jobber > HR Character` in `Assets/Data/HR`.
2. Fill `Id` (unique) and `Hr Name`.
3. Put it in `GameDatabase > Hr Characters` (Refresh Database Lists does it).
4. Open the company asset and drag the HR into **Hr**.

When the player picks that company, **that** HR's dialogue, questions, sprites and rejection are used. Nothing from other HR characters.

> No HR on a company and no `Fallback Hr` in the database = error in the Console and the old shared interview is used. Always assign one.

### Quick start with example HR
`Tools > First Jobber > 12. Generate Sample HR Content` creates 4 example HR characters (placeholder text) and assigns them to companies that have none. Existing assets are never overwritten.

---

## 5. Everything an HR says (open the `HR_...` asset)

| Field | What it is |
|---|---|
| **Introduction** (+ mode) | First thing HR says. |
| **Resume Review** (+ mode) | Said before reacting to each resume choice. |
| **Choice Reactions** | This HR's own reaction to a specific choice. No entry = the choice's shared `Hr Reactions`. |
| **Rejection** (+ mode) | Said after the interview, before the day ends. |
| **Default Emotion** | Face when nothing else is set. |

If a field is empty the game falls back to the company's own dialogue, then the shared one in `GameDatabase`, so nothing breaks.

Changing one HR never changes another HR.

---

## 6. Interview questions

Each HR has its own question assets (create under `Assets/Data/HR/Questions`, `Create > First Jobber > Interview Question`).
**Do not use the same question asset for two HR characters.**

### Fixed question (always asked, in order)
1. Create a question. Leave **Linked Choice** and **Linked Category** empty.
2. Drag it into the HR's **Fixed Questions**. List order = asking order.

### Resume-linked question (depends on what the player picked)
1. Create a question. Set **Linked Choice** (exact match, strongest) **or** **Linked Category** (any choice of that category).
2. Drag it into the HR's **Resume Linked Questions**.
3. Matching order: exact choice first, then category. Inside the same tier: not-yet-asked first, higher **Weight** first, then list order. No randomness.

### Fallback question
Set **Fallback Question** on the HR. It is asked when no resume-linked question matches. Always set one.

### Interview order (Sequence)
HR asset > **Sequence**. One element per question asked:
- `Fixed` (leave `Fixed Question` empty to take the next unused fixed question, or drag a specific one)
- `ResumeLinked`

Examples: `Fixed, ResumeLinked` (default when empty) · `Fixed, Fixed, ResumeLinked` · `ResumeLinked, Fixed`.

### Question fields
- **Question Text**: shown on screen and spoken by HR.
- **Question Dialogue** (optional): if filled, HR says these blocks instead (multi-line, gender variants). Text on screen uses its first matching page.
- **Hr Emotion**: face while asking.
- **Answers**: each has `Id` (unique inside the question), `Text`, **Player Reaction**, **Hr Response** (with Mode each). Put `Speaker = HR` and an emotion on the HR response lines so the face reacts.

---

## 7. HR pictures and animation

HR asset > **Visuals**.
- **Emotion Visuals**: one entry per emotion. `Sprite` empty = a coloured placeholder with the HR name. `Tint` colours it.
- **Default Portrait**: used when an emotion has no sprite.
- **Atmosphere Variants**: add a phase (`Phase_02_Deterioration` etc.) and a sprite per emotion for that phase. Missing = the normal emotion sprite is shown.
- **Animator Controller** (optional): also drag an `Animator` into the HR portrait's `HRReactionController > Animator` in the scene.

Art can be swapped in later without touching code.

---

## 8. Daily events

`Create > First Jobber > Daily Event`. Key fields:
- **Trigger**: when it runs (`OnDayStart`, `BeforeResume`, `AfterResumeConfirmed`, `AfterCompanySelected`, `BeforeInterview`, `AfterInterview`, `AfterRejection`, `BeforeSleep`).
- **Exact Day** / **Min Day**: only on that day / from that day.
- **Repeatable**, **Priority**, **Required Flag / Forbidden Flag**, **Set Flags**.
- **Lines**: the dialogue (multi-line blocks work).
- Built-in flags: `resume_distorted`, `hr_strange` (visual horror effects).

---

## 9. Endings and the father's offer

- Father's phone call: `GameDatabase > Father Offer Lines` (+ Mode).
- Endings: edit the `Ending_...` assets: `Title`, `Option Label`, `Option Description`, `Lines`, `Background / Background Color`, `Final Text`, `Sound`.
- The design expects exactly 3 endings.

---

## 10. Atmosphere (look and sound by day)

`Assets/Data/Atmosphere`: each `Phase_...` has a `Start Day`, colours, overlay, flicker, audio and sprite overrides. The phase is chosen by the current day. Order them in `AtmosphereConfiguration.phases`.

---

## 11. Other things you may want to change

| I want to... | Do this |
|---|---|
| Change how many questions the **old shared** interview asks | `GameDatabase > Questions Per Interview` (HR characters use their own Sequence instead) |
| Show used-up choices greyed out | `GameDatabase > Show Consumed Choices` |
| Show rejected companies greyed out | `GameDatabase > Show Rejected Companies` |
| Max name length | `GameDatabase > Max Name Length` |
| Change the resume confidence of a choice | the choice asset > `Confidence Percentage` |
| Rebuild the main menu with the gender dropdown | `Tools > First Jobber > 9. Convert Main Menu To Resume Style` |
| Jump to an ending to test it | the debug console in Play mode |

---

## 12. Checklist after editing

1. **Refresh Database Lists** (new assets are in the database).
2. **Validate Content**: fix every warning that matters (missing HR, empty intro, question used by two HRs, missing answers...).
3. Play from the main menu with **two different genders** and **two different companies**; check the HR name, wording and questions differ.
4. Ctrl+S, then commit.

## Troubleshooting

| Problem | Likely cause |
|---|---|
| HR says nothing in an interview stage | that HR field is empty **and** the fallbacks are empty too |
| Wrong/no HR shows | company has no `Hr` assigned |
| New asset has no effect | not in `GameDatabase` (Refresh Database Lists) |
| A line is skipped | its Gender Ids do not match and it has no fallback block in the same Variant Group |
| Always the same random block | the field is `Sequential`, or only one block is non-empty |
| `{age}` shows in text | age was removed; use `{name}` / `{gender}` |
| Gender dropdown missing / game says "unspecified" | run `9. Convert Main Menu To Resume Style` or assign `Gender Dropdown` on `MainMenuController` |
| Question never asked | resume-linked question without Linked Choice/Category, or the player did not pick that choice |

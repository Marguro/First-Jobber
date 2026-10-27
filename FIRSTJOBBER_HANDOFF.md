# First-Jobber — Project Handoff from ChatGPT Design Conversation

**Purpose:** Transfer design decisions and authored-content requirements from the ChatGPT planning conversation into Codex CLI. This document is a **specification/handoff**, not proof that every feature is implemented. Inspect the live Unity repository, scene/prefab bindings, ScriptableObject assets, localization tables, `CLAUDE.md`, `NEXT_STEPS.md`, and Git state to distinguish implemented vs. planned.

## 1. Project overview

- Working title: **First-Jobber**.
- Unity-based 2D psychological/loop horror game jam project (team context: 2 developers, 3 artists; seven-day jam).
- Main narrative cycle: Main Menu → applicant name/gender creation (new game only) → Resume Editor → Company Selection → Interview with that company's own HR → rejection → sleep/next day → revised Resume with previously consumed choices removed → repeat → flexible endgame choice(s).
- Game revolves around the exhausting new-graduate job search. Rejection is part of the authored story; don't invent hiring success rules.
- Separate test/demo data from production data; do not overwrite tests just to import final writing.
- Use existing project architecture, not a wholesale rewrite. Existing Unity/editor version, assets, scene names, component types, and exact paths must be verified from the repo.

## 2. Resume workflow and production choices

- Resume offers multiple categories. Latest production content supplied: **Faculty/Major** (5 choices), **University** (5), **Personal Motto** (5): 15 production choices total.
- A production Resume Choice can carry Player Choice Reaction dialogue and a corresponding HR Resume-Linked question (with answer choices).
- Previous authored production data includes 15 linked questions and 42 answer choices (per ChatGPT planning notes); verify the data files, import status, IDs and actual counts in Unity.
- User can switch among choices before confirming Resume. After rejection, the confirmed selected choices become consumed and unavailable in later loops. Clicking a choice does not itself consume it.
- **Player Choice Reaction is once per unique Choice ID per playthrough**: if A was already reacted to, choosing B then A again updates selection/preview/confidence but does not repeat A's reaction. Preserve tracking through scene/UI refresh and day changes; reset for New Game. Avoid duplicate queueing with rapid clicks. Persist with save data if saving is implemented.

## 3. Resume Confidence

- Each ResumeChoice has a configurable Confidence percentage 0–100.
- Overall Confidence is arithmetic mean of currently selected categories (ignore unselected/empty; zero if none); updates in Resume Editor in real time.
- Vertical bottom-to-top Confidence Bar with sprite-ready Background/Fill/Frame. Do not introduce DOTween solely for it.
- Capture finalized confidence when confirming Resume; **do not** play confidence dialogue at Resume confirmation.
- Current intended confidence reaction trigger: **after selecting a company and pressing Apply, while Company Selection is visible, before transitioning to Interview and HR Introduction**. Merely highlighting a company does not trigger it. No overlapping dialogue and no repeated Apply.
- High if **> 50%**, Low if **<= 50%**; exactly 50 is Low. Threshold configurable without changing calculation formula.
- Day-specific confidence reaction for **days 1–5**. Each day has separate High/Low random candidate pools; **RandomOne plays exactly one candidate**. Each textual utterance was intended as an independent candidate, NOT all candidates in sequence. A selected candidate may itself have multiple pages.
- Candidate counts (High/Low): Day 1 = 3/5; Day 2 = 1/2; Day 3 = 2/1; Day 4 = 2/2; Day 5 = 3/3. Total 24 candidates. Full Thai lines are also transcribed in `DAY_CONFIDENCE_DIALOGUE_TH.md`. Verify them against imported assets and avoid duplicate imports.
- Must be data-driven with editable days and safe missing-day/empty-pool behavior. Day 5 content should NOT force altering actual loop length or exhaustion/endings.

## 4. Company and HR structure

- Five production company/HR pairs discussed (names in Thai/English should be verified against actual `CompanyData` asset IDs): Focus Frame Interactive, Redline Studio, Idea Harvest Interactive, Appfair Digital, Control Group.
- Each CompanyData links to its dedicated HRCharacterData. Prefer CompanyData as the authoritative direction to avoid redundant, inconsistent bidirectional links.
- Each HR owns independent Introduction, Resume Review, fixed questions, response/rejection dialogue, portraits/emotions/atmosphere options; do not attach all questions to all HRs indiscriminately.
- **Interview = 4 questions by default: 2 distinct random Fixed Questions drawn from that HR's own fixed pool + 2 distinct Resume-Linked Questions selected from questions valid for the confirmed Resume**. Fixed and linked pools are separate. Keep question count/order inspector-editable if architecture supports. Prevent duplicates within an interview; report insufficient valid linked questions rather than inventing unrelated questions or softlocking.
- Five HR characters have three authored fixed questions each (15 fixed questions total); a previous correction supplied Control Group question #2 answer options: `สามารถยอมปรับตามได้ทุกอย่าง` / `อาจจะมีการพูดคุย` / `คิดว่าได้นะ`; question #3: `ยินดี` / `จะพยายามเรียนรู้` / `คิดว่าได้นะ`. Confirm assets contain them.
- Gender-dependent HR dialogue (notably Appfair HR): use the applicant's **initially selected gender ID** to choose the corresponding dialogue block, never randomize gender. Filter gender variants before random selection. If a locale/gender variant is not authored, do not silently use the wrong-gender variant; report gaps and use an explicitly authored neutral fallback if present.
- HR Intro and Rejection may have distinct gender variants; these are separate from Company Choice Reactions and Confidence Reactions.

## 5. Dialogue data and playback

- Existing `FirstJobber.DialogueLine` should be a SINGLE authoritative class definition, not duplicate files. There was a prior conflict: newer code added `dialogueLines`, `genderIds`, `variantGroup`, `DialoguePlaybackMode` but older version carried runtime `[NonSerialized] string locKey`; merge without losing localization or serialized data. Check actual source before editing.
- A dialogue block has speaker, legacy text fallback, list of pages, emotion, portrait, eventId, delayBefore, autoAdvanceDelay, optional gender conditions, and runtime localization linkage where needed.
- `Sequential`: play each block in order. `RandomOne`: choose exactly **one block**, then play **all pages** within it sequentially.
- Preserve DialogueManager queue, completion callback, speaker name token substitution, auto-advance and manual-advance behavior. Prevent duplicate transitions/re-entrancy.
- Fixed HR questions, resume-linked HR questions, Resume choice reactions, Company choice reactions, HR intro/rejection, confirmation confidence reactions, daily events and endings are separate concerns: changing one should not globally change all.

## 6. Localization (English source, Thai only as translation)

- English is authoritative source text within ScriptableObjects and UI code; Thai translations are stored in `Assets/Resources/Localization/LocalizationTable_TH.asset` with `key`, `english`, `thai` fields.
- Runtime `LocalizationManager.Get(key, english)` returns English when English selected, entry absent, or Thai translation missing (never empty text).
- Stable IDs derive keys: examples `choice/c1/displayText`, `company/acme/jobTitle`, `choice/c1/playerReaction/0/text`, per-page `choice/c1/playerReaction/0/lines/1`, `speaker/Father`, and `ui/label/<hierarchy-path>`.
- `LocalizationManager.SetLanguage` stores the language in PlayerPrefs and updates UI (including localized labels and dialogue). Main Menu should offer English/Thai switching.
- Project localization export action identified as `Tools > First Jobber > 12. Export Translation Template`, writing `Assets/Localization/translation_template.csv` (UTF-8) and updating table while preserving authored Thai. Inspect exact menus/scripts before assumptions.
- Do not create a second localization system or destabilize existing keys. On content changes, review/update corresponding Thai translation and ensure multi-page dialogue keys survive reorder/migration.

## 7. Company Description writing FORMAT (not rewriting company tone)

- `Company_P_prod_company_appfair` is the **formatting** reference asset, not a writing-style/tone reference.
- Every production company description, in both English and Thai, must consist of two localized section headings and `- ` hyphen bullet lines, with one blank line between sections:

English:
```
Job Description
- First responsibility.
- Second responsibility.

Terms
- First condition.
- Second condition.
```

Thai:
```
รายละเอียดงาน
- หน้าที่ข้อแรก
- หน้าที่ข้อที่สอง

เงื่อนไขการทำงาน
- เงื่อนไขข้อแรก
- เงื่อนไขข้อที่สอง
```

- Preserve each company's facts, tone and number of points; do not invent job terms or responsibilities. Use real newline characters, no forced string literals of `\\n` and no bullet symbols other than `- `.
- Company Selection description viewport should remain a bounded-height scrollable TextMeshPro + Unity uGUI ScrollRect with masked viewport, word wrap, variable content height, vertical only; reset scroll to top on company/language change and recalculate layout. Do not enlarge full selection screen or interfere with parent company-list scrolling.

## 8. Atmosphere and endings

- Day/phase driven atmosphere progression may include Normal / Deterioration / Despair and sprite/background/UI/HR appearance variants; configurable ScriptableObjects; keep independent of one-off Daily Events.
- Endgame outcomes previously discussed as provisional (father's contact job, refuse/repeat, choose another path), not finalized. Do not hardcode an ending from an old plan.

## 9. Migration safety and Codex priorities

1. **Do not assume these features are implemented**; first inspect Git status, root `CLAUDE.md`, `NEXT_STEPS.md`, `AGENTS.md` (if any), scripts, Scenes/Prefabs, ScriptableObjects and localization.
2. If using Codex CLI, consider interactive `/import` → Claude Code for recent local Claude sessions. This ChatGPT conversation itself is not imported by that feature; this document carries its design decisions.
3. Protect `.meta` GUIDs, existing ScriptableObject assets/IDs, HR references, localization table entries, test/demo content, and Git branch boundaries. Avoid reimporting content packages and creating duplicate production assets without inspection.
4. Build an **implemented / incomplete / unverified** checklist from actual repository evidence before changing code. Ask for explicit task scope or work on the user's next concrete request.
5. Do not modify the entire game when asked for a targeted fix. Explain affected files and inspector setup in Thai, including what was and was not tested in the Unity Editor.
6. Commit a checkpoint or recommend one before substantial changes; never overwrite teammate changes or uncommitted work.

## 10. Source caveat

The notes above compile the design decisions from the ChatGPT conversation as of 27 Oct 2026. The locally checked-in code and serialized assets are the authoritative evidence of **current implementation**. Authored content references are included separately under `reference_packages/` (previously generated import packages), and day confidence text in `DAY_CONFIDENCE_DIALOGUE_TH.md`. These are **staging references, not proof of actual Unity import**; inspect the repository before modifying or importing.

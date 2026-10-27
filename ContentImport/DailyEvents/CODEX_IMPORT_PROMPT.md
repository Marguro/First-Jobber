# FIRST-JOBBER — IMPORT PRODUCTION DAILY EVENTS (DAY 1–6)

You are working in the existing Unity First-Jobber project. Implement the events in the attached `ContentImport/DailyEvents/day_events_production_th.json` into the **active Production game**, not just into unused demo data. The JSON is source material, not a request to rewrite the runtime event architecture.

## 0. Read and audit BEFORE changing data

1. Read `ContentImport/DailyEvents/day_events_production_th.json` in full. Read `FIRSTJOBBER_HANDOFF.md`, `CLAUDE.md`, `NEXT_STEPS.md` if present.
2. Inspect actual Game Scene, active production `GameDatabase_Production`, existing Day/Loop state, current DailyEvent ScriptableObjects, DialogueManager and its Sequential/Multi-Line implementation, dialogue UI, choice popup/interview-answer UI, LocalizationManager, `LocalizationTable_TH.asset`, Ending/Timeline assets and the atmosphere progression integration.
3. Identify all existing Production day events, IDs, localization keys, and trigger points before creating anything. Reuse, repair or extend existing serialized assets and code. Do not duplicate imported events. Do not modify Demo/Test assets. Preserve asset GUIDs, all authored data and pending Git changes.
4. Inspect the `m_Script` reference for any existing broken ScriptableObject before modifying its data. Never delete and recreate a production asset to hide a missing MonoScript problem.

## 1. Required event trigger map

- Day 1 before Resume: opening story (`event_day01_opening`), **then** show Resume Editor.
- Day 1 on entering Resume: Resume tutorial (`event_day01_resume_intro`), once per New Game, and after opening intro. Allow interaction only after it completes.
- Day 1 on entering Company Selection: company tutorial (`event_day01_company_intro`), once per New Game, before normal company interactions.
- Day 1 after HR rejection: (`event_day01_after_rejection`), after rejection dialogue finishes but before sleeping / incrementing day.
- Day 2 before Resume: (`event_day02_before_resume`), morning lines → incoming phone call from Friend → player response choice popup → common call outro → hangup → final player line → Resume.
- Day 2 after rejection: (`event_day02_after_rejection`).
- Day 3 before Resume and after rejection: (`event_day03_before_resume`, `event_day03_after_rejection`).
- Day 4 before Resume: (`event_day04_before_resume`), morning lines → incoming call from Father → player response choice popup → common call outro → hangup → final player line → Resume.
- Day 4 after rejection: (`event_day04_after_rejection`).
- Day 5 before Resume and after rejection: (`event_day05_before_resume`, `event_day05_after_rejection`).
- Day 6: (`event_day06_final_story`), **STORY AND FINAL DECISION ONLY**. Do NOT show Resume Editor / Company Selection / Interview on Day 6.

Use the existing story-day counter. Do not hardcode time delays instead of scene/event completion callbacks. Events must play exactly once at their intended trigger per applicable cycle (do not replay simply because a panel refreshes or is reopened).

## 2. Dialogue behavior: SEQUENTIAL, NOT RANDOM

**Very important:** every item in `lines_th` is an individual dialogue PAGE. In Daily Events, pages play in the exact order authored. Advance through the existing DialogueManager with the normal click/auto-advance behavior; do not randomly pick one event line.

This is **different** from the Day+Confidence Reaction feature, which uses `RandomOne`. Do not change the Confidence candidate randomization.

For the phone conversations, set speaker according to `speaker` on the segment: `Player`, `Friend`, or `Father`. Replace the token `{name}` with the current applicant name at runtime after localization, reusing existing token substitution. Avoid gender assumptions in English or Thai. Speaker names should localize with the existing `speaker/*` conventions.

Do not skip the authored `...` pauses; they are intentional dialogue pages.

## 3. Phone call choice popups (Day 2 and Day 4)

At `choice_popup` segments, STOP normal dialogue advancement and show an actual interactive choice UI with the authored options.

Day 2 Friend answer:
- `...`
- `เดี๋ยวค่อยว่ากัน`

Day 4 Father answer:
- `...`
- `ขอลองเดินทางที่ตัวเองเลือกก่อนนะพ่อ`

Keep options' stable IDs. Localize choice labels. Record the choice in the story/session if the existing event system supports it, but there are **no authored different response branches** yet. Selecting either answer proceeds to the same next scripted segment. Do not invent branching outcomes.

Use the existing modal/choice component if possible. Avoid a second dialogue manager. Lock only conflicting interactions and avoid deadlocks or duplicate submission. Incoming ring/hangup are optional configurable `eventId`/audio cues: no authored AudioClips were supplied, so no fabricated sound assets. At minimum retain those beats as simple text/stage events if sound references are empty.

## 4. Day 6 final decision — TWO-STAGE CONFIRMATION

Only after Day 6's entire opening monologue finishes, show the three-ending Choice Popup:

1. `ไปสอบข้าราชการ` (civil-service exam, Timeline ending)
2. `ไปทำงานกับพ่อ` (father's job, Timeline ending)
3. `สมัครงานต่อไป` (loop to Day 1 Resume Editor)

**On initial choice click:**
- Store the option as a temporary selection, not final.
- Play the option's own `preview_dialogue_th` as one or more Sequential Dialogue pages.
- DO NOT trigger the ending, change scenes, or reset state yet.
- After the preview finishes, show an explicit `ยืนยันการตัดสินใจ` button and a `ย้อนกลับ` button.
- Confirm begins the selected ending exactly once; Back returns to the three-choice menu with no side effect. Selecting a different option plays that option's preview and requires confirmation again.
- Disable the confirm button until the matching preview sequence completes; prevent double clicks and duplicate Timelines / scene transitions.

Connect options 1 and 2 to the **existing** Ending ScriptableObject/Timeline references, if they exist. Keep their serialized references and GUIDs. If Timeline bindings do not exist, provide Inspector-assignable hooks, do not invent cutscene footage, and clearly report what the artist/designer needs to assign. Option 3 returns directly to Day 1 Resume Editor with no Main Menu and no applicant creation. Use or extend the existing loop reset: restore choice/company availability enough to allow a playable new cycle, reset story Day and background atmosphere appropriately, but retain the applicant's name/gender. Avoid replaying introductory Day 1 tutorials on looping, unless already explicitly configured otherwise. Preserve intended long-term meta-state; document any reset behavior.

**AUTHOR CONTENT CONFLICT — DO NOT SILENTLY CHANGE:** The preview line authored for ending 1 mentions Father accepting the protagonist, while the line authored for ending 2 discusses stable employment. These seem semantically swapped relative to their option labels. **Do not silently swap the option IDs, Timeline bindings, or dialogue lines.** Implement the exact option-to-preview mapping from JSON for now, clearly flag the discrepancy in your Thai report for the author to decide.

## 5. English source + Thai localization

The provided source file contains *authored Thai lines*, not already localized Unity assets. English is the runtime source language; Thai is a localization-table translation.

For **every event page, speaker name, popup option, ending name, preview line and confirmation/back label**:

- Write a faithful, natural English source. Preserve tone, hesitation, ellipses, humor and narrative intent.
- Preserve the authored Thai in `Assets/Resources/Localization/LocalizationTable_TH.asset` using the project's current import/export workflow.
- Use stable IDs based on explicit Event IDs, Segment IDs, Option IDs and page IDs/indices, consistent with existing `LocalizationKeys`; do not key on visible text or ephemeral hierarchy object names.
- Do not overwrite unrelated translations or generate duplicate keys. Check English fallback shows nonempty text if a translation is missing.
- Preserve all original intentional pauses and line segmentation. Any obvious typo normalization in the JSON is described in README; do not creatively rewrite or shorten dialogue.
- Do not accidentally introduce two competing localization pipelines. If the localization importer supports multi-line DialogueLine pages, use its existing per-page keys.

## 6. Integration with other existing systems

Ensure ordering does not conflict with these existing features:

- Resume Choice Reactions (shown once per choice as authored)
- Resume Confidence calculation/bar and per-choice stars
- Company Apply → Day-based Confidence Reaction (RandomOne) → correct HR Introduction
- HR Fixed Questions / Resume-Linked Questions (2 + 2)
- HR Rejection
- After-Rejection daily event → advance day
- Day/Atmosphere/Background progression, especially Day 6 and reset to Day 1
- Existing save/load and scene management

Do not insert daily events in the middle of HR rejection pages, and do not start the next day until the post-rejection event is complete. Day 6 is not another application day. Do not change hiring/rejection logic because narrative text mentions confidence or odds.

## 7. Production asset validation and implementation

Actually IMPLEMENT missing ScriptableObjects, entries, serialized references and UI wiring in the active Production Game Scene. If the current architecture uses a central DayEvent manager or event lists in GameDatabase, populate that structure in place; don't introduce parallel systems. Prefer Editor scripts only for safe asset authoring; no UnityEditor API in builds. Do not edit Unity YAML blindly without checking the script's serialized fields and GUIDs.

Verify:
- 13 day-event trigger groups: Day 1 x4; Days 2–5 x2 each; Day 6 x1.
- 54 authored event dialogue pages (plus 4 final-choice preview pages).
- Day 2 popup with 2 choices and Day 4 popup with 2 choices.
- Day 6 final choice popup with 3 options and a two-stage preview → confirm/back flow.
- All localization keys resolve to correct English/Thai.
- Each event triggers at the correct time and does not replay due to UI refresh.
- Day 6 has no Resume/Company/Interview gameplay.
- Ending 3 loops to a playable Day 1 Resume without Main Menu.
- No double firing or dialogue queue overlap, no deadlocks.
- Existing production references are preserved and no demo data is substituted.

Execute static validation/tests available on this machine. Do not claim Unity Play Mode verification unless actually performed. If Editor cannot be automated, list exact manual tests for me.

## 8. Output (Thai)

Report concise but complete:
1. Which event ScriptableObjects and scripts were updated/created.
2. Which exact Production GameDatabase/Scene references now invoke them.
3. Which popups and confirmation controls were wired.
4. Whether all authored pages, phone options and ending previews were imported.
5. What was added to English/Thai localization.
6. How Ending 3 resets the loop.
7. Any missing Timeline references, SFX or other Inspector work.
8. The Day 6 ending 1/2 dialogue mismatch without changing it silently.
9. Which tests were performed vs which require Unity Play Mode.

**Proceed with the import/implementation. Do not stop at an architecture proposal, do not overwrite existing author-authored Production content, and do not turn the daily story into random dialogue.**

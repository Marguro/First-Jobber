# FIRST-JOBBER — IMPORT AUTHORED RESUME CONTENT + HR-LINKED QUESTIONS + MAIN-MENU LANGUAGE CONTROL

You are working inside my existing **First-Jobber Unity 6** project. This is a **content integration and localization update**, not a request to rewrite the gameplay. Explain the completed work and Inspector instructions in **Thai**.

## Input

I have supplied `production_content_bilingual.json` alongside this prompt. **Read the complete JSON file.** It is a structured **authoring handoff**, **not a Unity-native data format**. Adapt its fields into the project's **existing ScriptableObject schemas** and localization pipeline. Do not replace existing managers or introduce a parallel runtime content system.

The JSON contains exactly:
- 3 Resume categories: `faculty` (คณะ), `university` (มหาวิทยาลัย), `motto` (คติประจำใจ).
- 5 Resume choices per category = 15.
- 42 player-reaction dialogue pages across those choices.
- 15 Resume-linked HR interview questions (one per choice).
- 42 player-selectable interview answers (the `/` delimiters in the supplied Thai text have already been split into options).
- All content as **English source text + Thai translation**.

**Treat the supplied Thai content as authored source material.** Do not paraphrase, rewrite, omit, merge, or fabricate story dialogue. English in the JSON is a newly drafted translation and may be polished later; stable IDs and Thai lines must be retained. Where minor formatting ambiguities are noted in JSON, flag them for writing review rather than silently inventing replacement content.

## 1. Inspect existing systems first

Before editing, inspect at least:
- `GameDatabase`, all current Resume/Choice ScriptableObject types and IDs.
- Company and `HRCharacterData`, HR question definitions, links, interview selection logic.
- `DialogueLine`, Multi-Line dialogue blocks, Sequential/RandomOne playback, reaction-once tracking.
- `LocalizationManager`, `LocalizationKeys`, `LocalizedLabel`, localization table definitions, CSV export/import tools and any localization editor scripts.
- Main Menu scene/prefab and its UI controller, `TMP_Dropdown`/language selection flow.
- GameSession, Choice Depletion and Resume Confidence.

Check working scenes and assets; preserve asset GUIDs and authored data. Do not delete or rename existing dummy content or assets as part of import.

## 2. Separate production content from old test content

Create an organized **Production** content folder and clearly separate it from **Test / Demo** data. Follow the project's actual data path conventions; suggested structure:

```text
Assets/Data/Production/
    Resume/Categories/
    Resume/Choices/
    Interview/ResumeLinkedQuestions/
    Localization/
    GameDatabase_Production.asset   (only if this matches project architecture)
```

Use the existing data types rather than making duplicate types. Generate uniquely named assets with stable IDs from the JSON; keep existing demo data untouched. If a root GameDatabase is used by the running scene, create a configurable way to assign the production dataset **in the Inspector**, preferably without replacing the demo source or adding a second manager. Do not silently change the current scene to use production data; provide a clearly explained demo/production switch or separate test scene if appropriate to the existing architecture.

Category display names (English original / Thai translation):
- `faculty`: Faculty / คณะ
- `university`: University / มหาวิทยาลัย
- `motto`: Personal Motto / คติประจำใจ

Do not assume the existing Dream or Passion categories should be deleted: these are the new authored categories. Keep older test categories isolated and not loaded in production content unless expressly requested.

## 3. Import Resume choices and player reactions

For each JSON choice:
- Create/update the relevant **production** Resume Choice asset with its stable `id`.
- Set the asset's **English** display text from `display.en`.
- Add **all** `playerReaction.blocks[].lines[].en` as pages within its existing Multi-Line Dialogue block (Speaker = Player). Preserve order.
- Preserve existing support for **reaction only once per Choice per playthrough**; choosing the same option again must still update the Resume and Confidence, but must not replay the reaction.
- Do not use RandomOne to randomly discard authored lines within a single block. The complete authored reaction must play line-by-line.
- Do not consume choices when clicked; only after a completed rejection.
- Keep the existing deterministic Confidence system. **The author did not specify percentages.** Leave production confidence as explicitly identified TODO/default values (e.g. the current valid default) rather than inventing supposedly authored percentages; document exactly which Inspector fields require the author to set them.

## 4. Import HR Resume-linked questions and selectable answers

For every `linkedQuestion` from the JSON:
- Create a production Interview Question asset (or existing equivalent) with the given stable Question ID.
- Set **question type = ResumeLinked** and link **exactly** to its parent Choice ID / Category ID using the current supported linking schema. An exact Choice ID match is preferred over broad category match.
- Set the question's English source text from `linkedQuestion.question.en`.
- Create every answer in original order from `linkedQuestion.answers`; each answer must be a separate selectable choice. Never combine `/` alternatives into one displayed answer.
- Use answer IDs from JSON. Localize question and answer text through the same keys as the project's existing question/answer localization logic.
- Do **not** invent HR answer reactions or emotions: those were not supplied. Use a transparent empty/no-op default if valid in the current runtime; if the interview requires a fallback response, use clearly labeled TEST PLACEHOLDER content outside final authored dialogue and document how to replace it.
- Preserve existing interview answer selection and callback behavior and prevent softlocks if optional reactions are empty.

### Critical: HR ownership

The project uses **company-specific HR characters**. The author has **not specified which HR owns these 15 new questions**. Do not attach all questions to every existing HR automatically, and do not mix two HR characters' personal intro/rejection dialogue.

Import the 15 questions as a clearly marked **unassigned authoring question pool** or reusable source templates, then provide Inspector assignment instructions or a safe editor utility to copy/assign them into an explicitly selected HR's `ResumeLinkedQuestions` list. Use the project's actual HR architecture. Existing fixed interview questions should remain unchanged. Never randomize Fixed Questions. Do not silently assign ownership to `HR_01_Iris`, `HR_02_Gregor`, etc.

## 5. Correct localization behavior (English is source)

The game already follows an **English-as-source / Thai-only translation table** workflow. Do not replace it with Unity Localization Package or a new table system.

Existing table:
`Assets/Resources/Localization/LocalizationTable_TH.asset`

Existing export menu (verify actual implementation):
`Tools > First Jobber > 12. Export Translation Template`

Existing CSV location (verify):
`Assets/Localization/translation_template.csv`

Expected behavior:
- English selected: show the English text stored on assets/in code.
- Thai selected: `LocalizationManager.Get(key, english)` returns translated Thai if present, otherwise **falls back to English**, never a blank string.
- Preserve existing `LocalizationKeys` conventions, including keys such as `choice/{id}/displayText`, `choice/{id}/playerReaction/0/lines/{i}`. For question and answer keys, **inspect actual generator and use its real stable key pattern**, do not invent one.
- Translation must be applied to each Multi-Line Dialogue PAGE before formatting tokens (such as `{name}`).
- Keep the distinction between legacy single-page text keys and multi-page dialogue keys, preserving backwards compatibility.

### Translation import protocol

The supplied JSON stores English/Thai pairs as authoring data, but in Unity runtime:
1. Put **only English** in the source text fields of production ScriptableObjects.
2. Use the project's established exporter/importer or a safe id-based editor import method to put **only Thai translations** into the Thai localization table.
3. Never overwrite old translations or existing demo keys.
4. Do not manually edit serialized Unity `.asset` YAML when a project editor API/utility can create/update assets safely.
5. After import, rerun/export the translation template using the existing tool and verify key coverage. If the export tool protects pending CSV changes, preserve the CSV and follow its expected import workflow rather than overwrite it.
6. Idempotent imports: running the import again must not duplicate assets, key rows, questions, answers, or dialogue lines.
7. Validate uniqueness, missing translations, and any changed English source text.

The import should produce 15 choice display strings, 42 reaction pages, 15 question strings, 42 answer strings, plus 3 category titles (subject to the project's exporter key coverage). These counts are acceptance checks, not instructions to use fabricated key paths.

## 6. Add language selection to MAIN MENU

Add visible language selection on the **Main Menu only** (but switching language must update the entire game). A small `TMP_Dropdown` or two buttons is acceptable; prioritize fitting the current Main Menu artwork.

Options:
- English
- ไทย

Requirements:
- Use the existing `LocalizationManager.SetLanguage` and project language identifiers; do not introduce a parallel locale state.
- Load the saved language from PlayerPrefs on launch; make selected option reflect that preference.
- Switching languages updates main menu labels, profile screen, choice button text, dialogue (including all pages), question/answer text, and dynamic UI labels using the existing localization mechanism.
- Avoid duplicate onValueChanged listeners / recursive dropdown changes.
- If a scene label already uses `LocalizedLabel` or scene auto-binding, reuse it and ensure change notifications refresh it.
- Preserve PlayerName token replacement and HR gender-aware dialogue logic.
- Missing Thai translation must display the existing English source, not blank.
- Language switching must not reset choice history, confidence, applicant gender, company selection, atmosphere, game progress, or active scene.
- Do not create final art. The language control must remain easy to reskin using art-team sprites later.

## 7. Validation and tests

Test at minimum:
1. Production and Demo datasets are isolated and selectable; old test assets survive unchanged.
2. Exactly 3 production categories, 15 distinct production choices and 15 exact-choice-linked production questions exist.
3. Exactly 42 authored Player reaction pages and 42 distinct answer choices were imported.
4. First selection plays complete Player reaction; revisiting same choice does not replay it.
5. Choosing a resume option makes **only its linked question** eligible in the question matching logic (subject to selected HR ownership); unrelated question does not match.
6. A question's alternatives are each displayed as distinct options.
7. English mode displays correct English sources. Thai mode displays supplied Thai. Missing Thai falls back to English.
8. A three-page reaction localizes each page separately in correct order.
9. Changing language in Main Menu persists across restart and refreshes UI without resetting playthrough.
10. Question binding never silently attaches templates to unrelated HRs.
11. Existing fixed questions, rejection loop, confidence bar, atmosphere variants, gender-specific HR dialogue and endings remain functional.
12. No id collisions, duplicate localization keys, missing required references or new compilation errors.
13. Verify Unity Editor Play Mode where available; otherwise report precisely what remains unverified.

## 8. Deliverables

- Production ScriptableObject assets and all necessary references.
- Thai localization entries imported into the **existing** table using current toolchain.
- Visible main-menu language selector wired to current LocalizationManager.
- A short Thai `PRODUCTION_CONTENT_SETUP_TH.md` containing paths and Inspector steps, and a list of unresolved items:
  - HR owner assignment for these linked questions.
  - Production confidence percentages not yet authored.
  - HR responses/emotions after interview answers not yet authored.
  - Any writer review flags from the source JSON.
- Thai summary of files created/changed, exact totals, validation results, and Play Mode tests remaining.

**Start by inspecting existing project APIs and serialized content, then import conservatively. Do not overwrite, merge, or delete existing test content.**

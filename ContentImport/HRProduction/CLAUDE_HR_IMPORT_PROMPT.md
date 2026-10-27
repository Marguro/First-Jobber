# FIRST-JOBBER — IMPORT COMPANY-SPECIFIC HR PRODUCTION CONTENT

You are continuing work inside my **existing Unity 6 First-Jobber project**. Inspect the current project first. Read this entire document and `ContentImport/HRProduction/hr_production_bilingual.json` before modifying files. The source artwork boards are included in `ContentImport/HRProduction/references/` for checking the author's exact wording; do **not** automatically import the embedded reference images as in-game character portraits.

## WHAT THIS DATA MEANS (CRITICAL)

The images define **5 production companies** and **one distinct HR per company**. Each HR owns their:
- Introduction dialogue, including optional applicant-gender variants.
- **FIXED QUESTION POOL**: the section labelled `คำถามที่จะเอาไปสุ่มได้` in the boards. These questions are **not linked to Resume choices**; **randomly select** from that specific HR's authored fixed-question list. "Fixed" here means *standalone, non-resume-linked authored questions*, **NOT fixed order**. This explicitly supersedes the previous rule saying Fixed Questions must never be randomized.
- Rejection dialogue, including optional gender variants.
- Independent artwork references and emotion/atmosphere variants, assignable later.

**RESUME-LINKED QUESTION POOL is completely separate.** Reuse the 15 Resume-Linked question definitions previously imported from the Faculty, University, and Personal Motto production content. They match confirmed Resume choice IDs. Do not move the new fixed questions to Resume-Linked, and do not duplicate old Resume-Linked questions into the Fixed pool. Do not automatically assign all shared Resume-Linked questions to every HR; only use explicitly configured/assigned references and preserve each HR's ownership/selection configuration.

Initial interview configuration for **EVERY HR: FOUR QUESTIONS PER INTERVIEW**:
1. Random Fixed Question #1 from the selected HR's own fixed pool.
2. Random Fixed Question #2 from that same HR pool, WITHOUT replacement.
3. Random Resume-Linked Question #1 from the selected HR's explicitly assigned linked bank, matching the confirmed Resume choices.
4. Random Resume-Linked Question #2 from the same eligible matched bank, WITHOUT replacement.

**Fixed = not linked to Resume, not "fixed order"**. Randomize both question pools independently when creating the interview plan; do not re-roll when a question or UI refreshes. Keep the displayed sequence **Fixed → Fixed → Resume-Linked → Resume-Linked** by default, and make that order configurable. The count is editable per HR, but production default is 2 + 2. Validate each HR has two distinct eligible matching Resume-Linked questions for the intended Resume combinations. If not, report incomplete configuration and do not invent question dialogue; handle gracefully without a softlock.

## PRODUCTION ASSETS MUST BE SEPARATE FROM DEMOS

1. Preserve all existing Test/Demo content and **all existing authored assets**. No renaming or destructive updates of demo IDs.
2. Import as a NEW production group e.g. `Assets/FirstJobber/Data/Production/Companies` and `.../Production/HR` (adjust to actual project structure).
3. Prefix identifiers with `prod_` and keep them stable; JSON already supplies IDs.
4. Do not overwrite a user-authored asset when an ID already exists. Make the importer **idempotent**: re-importing updates only the intended production assets after checking collisions, preserving GUIDs and unchanged references.
5. Use the existing CompanyData, HRCharacterData, InterviewQuestionData, dialogue blocks, ScriptableObjects, localization and game database. Do **not** build competing managers or incompatible content models.
6. Use a separate **Production GameDatabase/configuration** or explicitly selectable production-content configuration; don't silently replace the currently selected demo database in the scene. Provide an easy, documented switch and validate before switching.
7. Keep current applicant name/gender, Resume confidence, loop logic, choices, atmosphere and endings unchanged.

## AUTHORING AND TEXT FORMAT

The JSON is **staging data** with `{english,thai}` objects. It is NOT intended as a new runtime content database. For every imported field, copy `english` into the matching ScriptableObject source-text field. English is an authored translation of the Thai user-provided boards; the Thai text is the source of truth and should not be paraphrased or polished without request.

Link the existing `LocalizationKeys` generator and `LocalizationManager.Get(key, english)` workflow. Generate real keys from the actual asset IDs and runtime field paths; DO NOT invent an incompatible key scheme. Populate/update `Assets/Resources/Localization/LocalizationTable_TH.asset` with the matching Thai translations, preserving prior entries.

Use the existing `Tools > First Jobber > 12. Export Translation Template` behavior when appropriate, and preserve any CSV pending edits: **never overwrite unimported Thai CSV work**. If necessary create a separate staging CSV rather than destroying content. Localized menu labels remain editable through the existing localization system. English must appear as fallback if a Thai key is missing.

Existing Multi-Line and RandomOne dialogue support must still function. For intros/rejections, preserve each HR's independent dialogue collections; use the current dialogue modes or explicitly configure them. Do not turn a series of authored lines in one block into randomly selected individual lines.

## COMPANY / HR DATA RULES

Read every `companies[]` record from the JSON. Import:
- Company ID, bilingual name, `jobDescription[]`, `workingConditions[]` / terms, and HR reference. **Salary is not in the source**: leave blank/unset, not invented.
- HR ID and name, introduction variants, rejection variants, fixed questions and answer options.
- Each fixed question must be a separate question asset or existing project-compatible serializable entry owned by that HR. Its answer options are individual answer choices (the slash `/` in the source meant different selectable answers, never text to show as one combined answer).
- The authored answer choices have **no HR follow-up response** specified. Do not invent production HR answer reactions; retain empty/default values and mark TODOs for writers.
- The images are creative visual reference boards, not finished game sprites. Leave production portraits and sprite/Animator bindings as replaceable placeholder fields.

### Gender-specific dialogue
`prod_hr_appfair` has separate introduction and rejection variants for `male` and `female`. These are authored as written, even if intentionally inappropriate or discriminatory in the fictional story. Do not randomly play the wrong variant. Filter by applicant gender **before** any RandomOne logic. This is **NOT** a request to author a new backup line for AppFair. For AppFair (`prod_hr_appfair`), choose the existing Intro and Rejection authored variants by the **gender selected at initial applicant registration**, `ApplicantProfile.genderId`:
- `male` plays ONLY the `male` variant.
- `female` plays ONLY the `female` variant.
Filter matching blocks BEFORE RandomOne. Do not pick or mix genders at random. For other gender IDs, if a separate authored neutral/default line already exists, use that; otherwise warn and safely skip the missing optional line instead of inventing text or playing a wrong-gender line. This HR remains assigned to the same company regardless of gender. Preserve original texts without editorial changes.

For the other four HRs, use gender-neutral selection conditions (the original text may still contain Thai polite particles specific to the speaker).

### Random Fixed Question behavior
- In each interview, resolve the chosen Company's assigned HR first.
- Filter to that HR's eligible, completed Fixed Questions.
- Randomly select the configured number **without replacement within the same interview**.
- Preserve the **original order of all answer choices** inside the selected question; do not randomize its answers unless explicitly configured elsewhere.
- Do not re-roll when a dialogue page advances or when a panel refreshes.
- Allow new selection on a later day/interview.
- If fewer eligible questions exist than requested, use only available entries and report a validation warning. Do not crash, reuse the same question within one interview, or invent fake content.
- Fixed questions do not check Resume category/choice IDs. Resume-Linked questions are resolved **only from the current HR's assigned linked question bank** with the existing matched ID rules; keep both pools logically distinct.

### Unfinished authored choices
The user has now authored the missing answer options for two Control Group questions. They MUST be imported as separate clickable choices in this order:

`prod_hr_controlgroup_fixed_02` (repeated client revisions):
1. `สามารถยอมปรับตามได้ทุกอย่าง`
2. `อาจจะมีการพูดคุย`
3. `คิดว่าได้นะ`

`prod_hr_controlgroup_fixed_03` (assigned work unrelated to interests):
1. `ยินดี`
2. `จะพยายามเรียนรู้`
3. `คิดว่าได้นะ`

Use the matching `{english,thai}` data from JSON. Both are production-ready and eligible for Fixed Random selection. Do not treat these as missing or invent HR answer follow-up lines. Another HR/question (`prod_hr_ideaharvest_fixed_03`) has exactly one authored answer (`...`)—preserve that, even though it is unusual; mark for editorial review but do not expand it without permission.

## IMPLEMENTATION CHECKLIST

1. Inspect actual current types and editor creation tools. Document field mapping and collisions.
2. Import company metadata and descriptions into new production-only assets.
3. Import HR assets and assign each HR to exactly its company through existing `CompanyData -> HRCharacterData` reference. Avoid contradictory two-way Source of Truth.
4. Import per-HR Intro, Rejection, and Fixed Question Pool; preserve all authored Thai/English text and gender conditions.
5. Do **not** silently replace all HR Resume-Linked banks: expose independent HR configuration. Reuse prior production Resume Questions by reference/allowed assignment as intended.
6. Update/fix existing interview selection logic where necessary so Fixed pools support **random without replacement**, while Resume-Linked remains Resume-matched.
   - Resume-Linked selection MUST ALSO be random without replacement among eligible matching questions from the selected HR bank.
   - Populate/validate explicit HR linked-bank references using prior production Resume questions. Do not silently use all global questions from unrelated HRs or create question duplicates without deliberate ownership.
   - If an interview currently only exposes two questions, update the per-HR slots/count to **2 Fixed + 2 Resume-Linked (4 total)**.
7. Populate localization through the existing key generator; ensure English fallback and preserve existing translated content.
8. Keep production and test/demo content separately selectable. Create a simple inspector/editor setup or safe import command; document it.
9. Validate IDs, company-to-HR links, gender matching, all question answers, 2+2 interview sequence (including insufficient matching candidates), and localization key coverage.
10. Update Thai README and provide a table of what remains unwritten (e.g., HR follow-up reactions, HR-specific Resume-Linked assignments, optional AppFair neutral gender variant).

## PLAYTEST ACCEPTANCE

- Selecting each company loads its unique HR, own intro and rejection, and own Fixed Question pool; no leakage across companies.
- Each interview uses **two randomly selected Fixed questions from that HR**, plus **two randomly selected Resume-matching questions** from that HR's assigned Linked bank (4 total when configured correctly).
- Two Fixed questions in the same interview are never identical, and the TWO eligible Resume-Linked questions are distinct and matched to the confirmed Resume.
- Gender-specific AppFair intro/rejection resolves correctly; all other gender values safely fallback.
- Answer options are separately clickable, in authored order, with no invented answers or responses.
- Existing Multi-Line dialogue displays all pages of selected blocks; RandomOne remains per dialogue collection, not per line.
- Importing does not delete previous Resume production content or demo data.
- English is used as ScriptableObject source text. Thai comes from `LocalizationTable_TH.asset`; missing translations fallback to English, never blank.
- Empty answer data does not softlock the interview.
- Inspector can assign real sprites/HR emotions/Atmosphere visuals in the future without code changes.

## DELIVERY

Implement in the actual Unity project. Report in Thai:
- Which files/assets were created or modified.
- Which company is assigned to which HR.
- How to swap the demo configuration for production safely.
- Where to assign each HR's own Resume-Linked Question references.
- How to add/edit Fixed Questions and turn on random selection.
- How to export/import Thai Localization safely.
- Exact manual Inspector/Scene assignments and Play tests needed.
- Any compilation/editor tests not run.

Start by inspecting and showing the mapping to the current project data types. Then implement as an additive, safe, minimally invasive production import. Do NOT overwrite existing artist work or sample content.

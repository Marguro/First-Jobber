# FIRST-JOBBER — HR CONTENT V2 PATCH (USE IF V1 WAS ALREADY IMPORTED)

You are working in the existing Unity First-Jobber project. Read `ContentImport/HRProduction/hr_production_bilingual.json`, this patch, and the full `CLAUDE_HR_IMPORT_PROMPT.md`. Inspect current CompanyData, HRCharacterData, InterviewQuestionData, InterviewManager, Choice handling and Localization before editing.

This is an IN-PLACE CONTENT UPDATE, NOT a request to regenerate all Company/HR assets. Preserve existing ScriptableObject GUIDs, IDs, references, authored lines, translations, artist images, and demo assets. Do not create duplicate content if production IDs already exist.

## CHANGES TO APPLY

1. **AppFair gender binding**: For `prod_hr_appfair`, Intro and Rejection variants are selected using `ApplicantProfile.genderId` as chosen at registration. `male` uses male lines; `female` uses female lines. Filter by gender before the existing RandomOne chunk selection. Do not create a second HR for each gender. Do not randomly mix male/female lines. No new "backup" line is requested. For any other gender, only use an existing authored neutral/default fallback if available, otherwise emit validation warning and skip missing optional content safely (no invented dialogue).

2. **Control Group authored answers**: Update existing `prod_hr_controlgroup_fixed_02` answer choices, in order:
   - สามารถยอมปรับตามได้ทุกอย่าง
   - อาจจะมีการพูดคุย
   - คิดว่าได้นะ
   Update existing `prod_hr_controlgroup_fixed_03` answer choices, in order:
   - ยินดี
   - จะพยายามเรียนรู้
   - คิดว่าได้นะ
   Use English `english` values from the JSON for ScriptableObject source text and Thai `thai` values for the existing LocalizationTable_TH keys. Enable both questions as production-ready. Do not invent HR answer follow-up reactions.

3. **Every HR interview uses 4 questions**: Create/use four configurable interview sequence slots:
   - Slot 1: Fixed Random
   - Slot 2: Fixed Random
   - Slot 3: Resume-Linked Random (matches current confirmed Resume)
   - Slot 4: Resume-Linked Random (matches current confirmed Resume)
   The first two are sampled from **that HR's own Fixed Question bank**, without replacement per interview. The latter two are sampled independently from **that HR's explicitly assigned Resume-Linked bank**, matching confirmed Resume Choice ID/category; sample without replacement per interview. These two systems are separate; do not treat Fixed as Resume-linked or randomize answer option order. Keep question order Fixed, Fixed, Linked, Linked by default; Inspector-configurable. Do not reroll on UI/dialogue advance.

4. **HR bank configuration**: Prior production Resume-linked question assets exist. Do NOT automatically use the entire global question library for each HR at runtime. Establish deliberate per-HR assigned references or explicitly configured owned copies, preserve HR independence, and validate each HR can produce TWO distinct matching linked questions for a valid Resume. Source material does not specify new unique HR-specific resume questions; do not fabricate them. If fewer than two matches, log a clear authoring/validation warning and use an existing explicitly configured HR-specific fallback if any; otherwise gracefully play fewer questions rather than softlock. Report which HRs are not yet guaranteed to reach 4 questions.

5. Preserve existing RandomOne dialogue (random Chunk, all Pages sequentially), localization, gender filtering, Choice Depletion, Confidence, Atmosphere, Events, and guaranteed Rejection.

## VERIFY AND REPORT IN THAI
- Each of 5 HRs has 3 eligible authored Fixed Questions (15 total), and Control Group Q2/Q3 each has 3 clickable answers.
- Interview plan is 2 random distinct Fixed + 2 random distinct eligible Resume-linked = 4 total when bank is populated.
- Company-to-HR assignments do not cross.
- AppFair male/female Intro and Rejection match registration gender.
- English fallback/Thai Localization works, especially six newly authored answers.
- No duplicate IDs, content deletions, or ScriptableObject GUID churn.
- List any missing explicit per-HR Linked assignments or missing neutral fallback data.
- Clearly separate verified compile tests from tests that still need Unity Play Mode.

Implement this patch incrementally after inspecting the project. Do not overwrite production assets wholesale.

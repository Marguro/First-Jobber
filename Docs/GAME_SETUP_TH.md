# First Jobber — คู่มือระบบเกม (ภาษาไทย)

เกม 2D Psychological Horror แนว Loop: สมัครงาน → โดนปฏิเสธ → ตัวเลือกหายไปเรื่อย ๆ → พ่อเสนองาน → เลือก 1 ใน 3 ฉากจบ

## 1. เริ่มใช้งานเร็วที่สุด (3 ขั้นตอน)

1. เปิด Unity 6 → ถ้ามี Dialog ให้ Import **TMP Essential Resources** ให้กด Import (ครั้งแรกครั้งเดียว)
   หรือเมนู `Window > TextMeshPro > Import TMP Essential Resources`
2. เมนู `Tools > First Jobber > 1. Generate Sample Data` — สร้างข้อมูลตัวอย่างใน `Assets/Data` (ไม่ทับไฟล์เดิม)
3. เมนู `Tools > First Jobber > 2. Build Game Scene` — สร้าง `Assets/Scenes/Game.unity` พร้อม UI, Prefab และเชื่อม Reference ทั้งหมดให้อัตโนมัติ แล้วกด **Play**

เมนูเสริม: `3. Validate Content` (ตรวจ ID ซ้ำ / reference หาย / คำถามที่ link ผิด / บทพูดว่าง) และ `Refresh Database Lists`

> สถานะการทดสอบ: โค้ด runtime คอมไพล์ผ่านแล้ว แต่ **ยังไม่เคยเล่นใน Editor** ควรทดสอบตามหัวข้อ 8 และแจ้งบั๊กที่พบ

## 2. สถาปัตยกรรม

```
GameFlowManager (State Machine)  <- จุดเดียวที่เปลี่ยน Phase / เปิด-ปิด Panel / ต่อ Controller เข้าด้วยกัน
 ├─ GameSession      ข้อมูลความคืบหน้าทั้งหมด (ชื่อ, อายุ, วัน, choice ที่ถูกใช้ไปแล้ว, flags, history) -> บันทึกเป็น JSON
 ├─ ChoiceManager    คำนวณ Available / Selected / Consumed จาก Database + Session
 ├─ LoopManager      BeginApplication / ApplyRejection (กันซ้ำ) / AdvanceDay / IsFinalDay
 ├─ DialogueManager  เล่นบทพูดทีละ sequence (เริ่มอันใหม่ = ยกเลิกอันเก่าโดยไม่เรียก callback เก่า)
 ├─ UIManager        Fade Panel + จอดำตอนนอน
 ├─ DailyEventManager เลือก/เล่น Event ตาม Trigger, วัน, flag, priority
 └─ Controllers      MainMenu, CreateApplicant, ResumeEditor, CompanySelection, Interview, FinalDecision, Ending
```

- **ScriptableObject ไม่ถูกแก้ตอนเล่น** — ความคืบหน้าอยู่ใน `GameSession` เท่านั้น
- ทุก Controller ส่ง **event** ให้ GameFlowManager (ไม่เรียกกันเอง)
- `GameFlowManager.Transitions` คือตารางที่กำหนดว่า Phase ไหนไป Phase ไหนได้ (อยากเพิ่ม Phase ให้เพิ่มที่นี่)

### Flow
MainMenu → CreateApplicant → ResumeEditor → CompanySelection → Interview (Introduction → ResumeReview → Question ⇄ Reaction) → Rejection → Sleep → DayTransition → (DailyEvent) → ResumeEditor … จนไม่เหลือใบสมัครที่ใช้ได้ → FinalDecision → Ending

- Main Menu / Create Applicant ขึ้นแค่ตอนเริ่มเกมใหม่ ไม่ขึ้นอีกในลูป
- Choice ถูก "consume" **เฉพาะหลังโดนปฏิเสธ** (ครั้งเดียวต่อใบสมัคร) ไม่ใช่ตอนกด
- เกมจบเมื่อ: ไม่เหลือบริษัท **หรือ** ทุกหมวด Resume ไม่เหลือตัวเลือก (หมวดที่หมดก่อนจะไม่บังคับให้เลือก) — จำนวนวันไม่ได้ hardcode

## 3. โครงสร้างไฟล์

```
Assets/Scripts/
  Core/       GameFlowManager, GameSession, GameContext, GamePhase, ChoiceManager, LoopManager
  Data/       GameDatabase + ScriptableObject ทุกชนิด (Category, Choice, Company, Question, Event, Ending), DialogueLine
  Dialogue/   DialogueManager, DialogueUI
  UI/         UIManager, ChoiceButtonUI, ChoiceListUI, ResumePreviewUI, MainMenu/CreateApplicant/DailyEventPanel
  Resume/     ResumeEditorController
  Companies/  CompanySelectionController
  Interview/  InterviewController, InterviewQuestionSelector, HRReactionController
  Events/     DailyEventManager
  Endings/    FinalDecisionController, EndingController
  Editor/     SampleDataGenerator, GameSceneBuilder, ContentValidatorMenu
Assets/Data/  ข้อมูลตัวอย่าง (Categories, Choices, Companies, Questions, Events, Endings, GameDatabase.asset)
Assets/Prefabs/ChoiceButton.prefab
```

## 4. Hierarchy ของ Scene (สร้างให้อัตโนมัติ)

```
Main Camera / EventSystem (InputSystemUIInputModule)
GameSystems          <- DialogueManager, UIManager, DailyEventManager, GameFlowManager
GameCanvas (1920x1080, Scale With Screen Size)
 ├ Background
 ├ MainMenuPanel           [MainMenuController]
 ├ CreateApplicantPanel    [CreateApplicantController]
 ├ ResumeEditorPanel       [ResumeEditorController]
 │   ├ ResumePreview (ซ้าย)  [ResumePreviewUI]  -> CategoryList (แถวหมวดหมู่คลิกได้)
 │   └ EditorGroup (ขวา)     -> CategoryTitle, ChoiceList (ScrollRect), ConfirmButton
 ├ CompanyPanel            [CompanySelectionController]
 ├ InterviewPanel          [InterviewController] -> QuestionCard, AnswerList, HRArea [HRReactionController]
 ├ DailyEventPanel         [DailyEventPanelUI]
 ├ FinalDecisionPanel      [FinalDecisionController]
 ├ EndingPanel             [EndingController]
 ├ DialogueBox             [DialogueUI]   (ใช้ร่วมทั้งเกม)
 └ TransitionOverlay       (จอดำ)
```

### Reference ที่ต้องมี (Builder ตั้งให้แล้ว — เช็คเองหากสร้าง Scene มือ)
- **GameFlowManager**: database, dialogue, ui, dailyEvents, mainMenu, createApplicant, resumeEditor, companySelection, interview, finalDecision, ending
  (ถ้าขาด จะมี `Debug.LogError` บอกชื่อที่ขาด และเกมไม่เริ่ม)
- **DialogueManager**: ui (DialogueUI)
- **UIManager**: 8 Panel (CanvasGroup) + overlay + overlayText
- **DailyEventManager**: database, dialogue, eventPanel
- **HRReactionController**: dialogue, portrait, swayTarget, fan, flickerOverlay, visuals (4 อารมณ์)
- **ChoiceListUI**: scrollRect, content, itemPrefab (ChoiceButton), emptyState, canvasGroup

## 5. วิธีเพิ่มเนื้อหา (ไม่ต้องแก้โค้ด)

ขั้นตอนทั่วไป: คลิกขวาใน Project → `Create > First Jobber > ...` เก็บไว้ใน `Assets/Data/...` แล้วเพิ่มเข้า **GameDatabase** (ลากใส่ List หรือใช้ `Tools > First Jobber > Refresh Database Lists` ซึ่งเก็บทุกอย่างใน `Assets/Data` โดยเรียงตาม **ชื่อไฟล์** ดังนั้นตั้งชื่อนำหน้าด้วยเลขเช่น `Edu_05_...`)

ทุก `id` ต้อง **ไม่ซ้ำและไม่เปลี่ยนภายหลัง**

| ต้องการ | ทำอย่างไร |
|---|---|
| หมวด Resume ใหม่ | สร้าง `Resume Category` (id, displayName, emptyMessage) → ใส่ใน `database.categories` ลำดับใน List = ลำดับบนหน้าจอ แล้วสร้าง Choice ที่ชี้หมวดนี้ |
| ตัวเลือกใหม่ | สร้าง `Resume Choice`: id, category, displayText, description, **playerReaction** (บทพูดตอนกด), **hrReactions** (HR พูดตอนสัมภาษณ์: speaker=`HR` + emotion) |
| บริษัทใหม่ | สร้าง `Company`: ชื่อ, ตำแหน่ง, เงินเดือน, อุตสาหกรรม, โลโก้, playerReaction, (hrIntro / rejection ถ้าต้องการเฉพาะบริษัท) |
| คำถามสัมภาษณ์ | สร้าง `Interview Question`: questionText + answers (id, text, playerReaction, hrResponse) <br>- ใส่ **linkedChoice** = ถามเมื่อเลือก choice นั้น (ความสำคัญสูงสุด) <br>- ใส่ **linkedCategory** อย่างเดียว = ถามเมื่อเลือกอะไรก็ได้ในหมวด <br>- เว้นว่างทั้งคู่ = คำถามทั่วไป (fallback) <br>- เงื่อนไขเสริม: requiredCompany, requiredTag, weight |
| HR reaction ใหม่ | เพิ่มบรรทัดใน `hrReactions` ของ Choice (speaker=HR, emotion=Neutral/Interested/Disappointed/Uncomfortable) |
| Daily Event | สร้าง `Daily Event`: trigger (OnDayStart, BeforeResume, AfterResumeConfirmed, AfterCompanySelected, BeforeInterview, AfterInterview, AfterRejection, BeforeSleep), exactDay/minDay, priority, repeatable, requiredFlag/forbiddenFlag, setFlags, lines |
| แก้ฉากจบ | แก้ `Ending` asset: lines, finalText, background, backgroundColor, sound, optionLabel/optionDescription (ข้อความปุ่มเลือก) |
| บทพูดของพ่อ / ปฏิเสธ / แนะนำ HR | อยู่ใน `GameDatabase`: fatherOfferLines, rejectionHR, rejectionPlayer, defaultHrIntro, hrReviewLines, resumeIntroLines |

### DialogueLine
- `speaker`: `Player` (แสดงเป็นชื่อผู้เล่น), `HR`, `Father`, `Narrator` หรือชื่อใดก็ได้
- `text`: ใช้ตัวแปรได้ `{name} {age} {day} {company} {job}`
- `emotion`: ใช้เมื่อ speaker = HR (เปลี่ยนสีหน้า HR)
- `autoAdvanceDelay` (>0 = ข้ามอัตโนมัติ), `delayBefore`, `eventId`, `portrait`

### Flag พิเศษ (ตั้งผ่าน `setFlags` ของ Event)
- `resume_distorted` — ตัวหนังสือบน Resume กระตุก/เพี้ยน
- `hr_strange` — HR สั่น พัดลมหมุนเร็ว จอกะพริบถี่
- ตั้งชื่ออื่นเองได้ แล้วใช้เป็น requiredFlag/forbiddenFlag ของ Event อื่น

### ตั้งค่าใน GameDatabase
`questionsPerInterview` (ค่าเริ่มต้น 2), `showConsumedChoices` (แสดง choice ที่ใช้แล้วแบบสีเทา), `showRejectedCompanies` (แสดงบริษัทที่ปฏิเสธแล้วเป็น REJECTED), `minAge/maxAge/maxNameLength`

## 6. กฎการเลือกคำถามสัมภาษณ์
1. เก็บคำถามที่ผ่านเงื่อนไข (บริษัท/tag)
2. เรียงลำดับ: ตรง choice → ตรงหมวด → ทั่วไป (ในแต่ละกลุ่มเรียงตาม weight แล้วตามลำดับใน database = deterministic)
3. เลือกอันที่ **ยังไม่เคยถามในวันก่อน ๆ** ก่อน; ถ้าไม่พอจึงยอมถามซ้ำ; ถ้าไม่มีเลย ใช้คำถามฉุกเฉินใน Code (ไม่ค้าง)

## 7. HR / ภาพ placeholder
`HRReactionController.visuals` ใส่ Sprite + Tint ต่อ 1 อารมณ์ ถ้าไม่มี Sprite จะใช้สีและข้อความอารมณ์แทน พัดลม (`fan`) หมุนเอง ส่วน `swayTarget` ขยับเล็กน้อย `flickerOverlay` กะพริบ
เปลี่ยนภาพ: ใส่ Sprite ใน Image ต่าง ๆ ได้เลย (โลโก้บริษัทใส่ที่ `Company.logo`, พื้นหลังฉากจบที่ `Ending.background`)

## 8. วิธีทดสอบ Loop ทั้งเกม (Checklist)
1. Play → Main Menu → Start Game → ใส่ชื่อ/อายุ (ลองเว้นว่าง/อายุผิด ต้องมี error) → Confirm
2. Resume ซูมเข้ามากลางจอแล้วเลื่อนไปซ้าย ฝั่งขวาโผล่ขึ้น
3. เลือกหมวด/ตัวเลือก มีบทพูด Player; สลับหมวดแล้วตัวเลือกเดิมยังถูกเลือก; ปุ่ม Confirm เปิดเมื่อครบทุกหมวด
4. เลือกบริษัท → Apply → สัมภาษณ์: HR แนะนำ → รีวิว Resume (HR ตอบสนองตามที่เลือก สีหน้าเปลี่ยน) → คำถาม 2 ข้อ → ตอบ → Player/HR ตอบ → ปฏิเสธ
5. จอดำ "Day 1 is over..." → วัน 2 (Event ของพ่อ) → Resume Editor ชื่อ/อายุเดิม, ตัวเลือกที่ใช้แล้วหายไป
6. ทำซ้ำ 4 รอบ (ข้อมูลตัวอย่าง 4/4/4/4) → วัน 5: พ่อโทรมา → ปุ่ม 3 ฉากจบเลือกได้หมด
7. Restart → กลับไปสร้างตัวละครใหม่และข้อมูลสะอาด; Main Menu → Continue ใช้ได้เมื่อมี Save (บันทึกตอนเริ่มทุกวัน ลบเมื่อถึงฉากจบ)
8. Test H: เพิ่ม Choice/บริษัท/คำถาม/Event ใหม่ → Refresh Database Lists → Play ต้องปรากฏโดยไม่แก้โค้ด

## 9. ข้อจำกัดที่ทราบ
- ยังไม่ได้เล่นทดสอบใน Unity Editor (ตรวจเฉพาะการคอมไพล์ runtime)
- UI เป็น placeholder ตามโครงใน Prompt (ไม่พบ wireframe ในโปรเจกต์) — ปรับ Anchor/สี/ฟอนต์ได้โดยไม่กระทบโค้ด
- Typewriter ใช้ความเร็วตายตัว (`charactersPerSecond` บน DialogueManager); ไม่มีเสียง SFX ยกเว้น `Ending.sound`
- ไม่ได้ใช้ DOTween (ใช้ coroutine ตามที่กำหนด)
- Event ประเภท OnDayStart ไม่ถูกเรียกในวันที่ 1 (เริ่มเกมเข้า Resume Editor ตรง ๆ)
- ถ้า Choice ถูกลบ/เปลี่ยน id หลังมี Save เก่า จะถูกตัดออกอัตโนมัติ (`PruneInvalidSelections`) แต่ consumed id เก่าจะค้างในไฟล์ Save

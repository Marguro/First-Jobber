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

---

# 10. Atmosphere Progression System (บรรยากาศเปลี่ยนตามวัน)

ระบบนี้เปลี่ยนสี มืด/สว่าง ภาพ เสียง และเอฟเฟกต์ ตาม "วันปัจจุบัน" โดยไม่แตะข้อมูลเกม (Resume, บทสนทนา, ความคืบหน้า ไม่ถูกรีเซ็ต)

## ไฟล์
```
Assets/Scripts/Atmosphere/
  AtmospherePhaseData.cs      ข้อมูล 1 เฟส (ScriptableObject)
  AtmosphereConfiguration.cs  รายการเฟสทั้งหมด + ตรวจความถูกต้อง
  AtmosphereManager.cs        ตัวใช้เฟส: สีกล้อง, overlay, เสียง, flicker, สั่ง Binding
  AtmosphereSpriteBinding.cs  แปะบน Image / SpriteRenderer เพื่อสลับสไปรต์ตาม Target Id
Assets/Data/Atmosphere/       Phase_01_Normal (วัน 1), Phase_02_Deterioration (วัน 3), Phase_03_Despair (วัน 4), AtmosphereConfiguration
```

## ติดตั้งลง Scene
- `Tools > First Jobber > 1. Generate Sample Data` จะสร้าง asset ตัวอย่างให้ (ไม่ทับของเดิม)
- Scene ใหม่จาก `2. Build Game Scene` มีระบบนี้อยู่แล้ว
- Scene เดิม (เช่น GameLogic.unity): เปิด Scene แล้วเลือก `Tools > First Jobber > 4. Add Atmosphere To Open Scene` แล้วกด Save
  จะเพิ่ม AtmosphereOverlay (อยู่เหนือ Panel แต่ใต้กล่องบทพูด, ไม่บล็อกการคลิก), AtmosphereManager (บน GameSystems), AudioSource 2 ตัว และ Binding ให้พื้นหลังหลัก/Resume/บริษัท/สัมภาษณ์/กล่องบทพูด พร้อมต่อ `GameFlowManager.atmosphere`

## 1) สร้าง Atmosphere Phase
คลิกขวา `Create > First Jobber > Atmosphere Phase` แล้วกรอก
- **id** (ห้ามซ้ำ), **displayName**, **startDay**, **enabled**
- **Visuals**: `useCameraColor/cameraColor`, `overlayColor` + `overlayOpacity` (ความมืด), `uiTint` (คูณสี UI ที่มี Binding), `hrTint` (คูณสีตัว HR — ใช้ได้แม้ยังไม่มีสไปรต์), `fadeDuration`
- **Flicker**: `flickerEnabled`, `flickerInterval`, `flickerIntensity`
- **Audio**: `ambientClip`, `ambientVolume`, `audioCrossfade`
ทุกช่องเป็น optional ปล่อยว่างได้โดยไม่เกิด error

## 2) เลือกว่าวันไหนเปลี่ยนเฟส
ตั้ง `startDay` ของแต่ละเฟส ระบบใช้เฟสที่ **startDay มากที่สุดที่ ≤ วันปัจจุบัน** (ไม่ต้องเรียงใน List เอง)
ตัวอย่าง: Normal=1, Deterioration=3, Despair=4 → วัน 1–2 Normal, วัน 3 Deterioration, วัน 4+ Despair
แก้ Deterioration เป็น 2 ได้ทันที (Test E) ไม่ต้องแก้โค้ด

## 3) เพิ่ม/ลบเฟส
สร้าง Phase asset ใหม่ → ลากใส่ `AtmosphereConfiguration.phases` (เพิ่มเฟสที่ 4 ได้เลย) ลบ = เอาออกจาก List หรือติ๊ก `enabled` ออก
เมนู `3. Validate Content` จะเตือน: id ซ้ำ, startDay ซ้ำ, ไม่มีเฟสที่เริ่มวัน 1, Target Id ซ้ำในเฟสเดียวกัน

## 4) สไปรต์เฉพาะเฟส (Environment / UI / Props)
1. เลือก GameObject ที่เป็น Image (หรือ SpriteRenderer) → Add Component → **Atmosphere Sprite Binding**
2. ตั้ง **Target Id** (เช่น `BedroomBackground`, `Window`, `Computer`, `ResumeBackground`, `CompanyPanelBackground`, `InterviewBackground`, `UIDecoration`)
3. ใน Phase asset → `spriteOverrides` เพิ่มแถว: targetId = ชื่อเดียวกัน, sprite = รูปของเฟสนั้น
- เฟสที่ไม่ใส่ sprite จะใช้ **สไปรต์ดั้งเดิม** ของ Image
- `Apply Ui Tint` บน Binding: ติ๊กเพื่อให้สีถูกคูณด้วย `uiTint` ของเฟส
- สไปรต์เปลี่ยนด้วยการ fade ผ่านโปร่งใส (ใช้ `fadeDuration`)

## 5) สไปรต์ HR ตามเฟส + อารมณ์
- อารมณ์ (Neutral/Interested/Disappointed/Uncomfortable) ยังควบคุมโดย HR Reaction System ตามเดิม
- ระบบ Atmosphere เลือก **แบบ (variant)** ของภาพ: ใน Phase asset → `hrSprites` เพิ่มแถว emotion + sprite (เช่น Phase Despair: Neutral_Distorted ฯลฯ)
- ลำดับ fallback: สไปรต์ของ (เฟส + อารมณ์) → สไปรต์ใน `HRReactionController.visuals` → placeholder (สี + ข้อความ)
- เมื่อเฟสเปลี่ยนกลางเกม HR จะสลับ variant ทันทีโดยคงอารมณ์ปัจจุบัน ไม่ถูกเขียนทับ

## 6) สี และเสียง
แก้ใน Phase asset: `overlayColor/overlayOpacity` (ความมืด), `cameraColor`, `uiTint`, `hrTint`, `ambientClip/ambientVolume/audioCrossfade`
เสียงใช้ AudioSource 2 ตัวทำ crossfade; ถ้าเฟสใหม่ใช้คลิปเดิม จะแค่ปรับ volume (ไม่เริ่มเสียงใหม่)
ถ้าเฟสไม่เปลี่ยน ระบบจะไม่เริ่ม transition/เสียงซ้ำ

## 7) ทดสอบโดยไม่ต้องเล่นทั้งเกม
- Play Mode → เลือก `GameSystems` → ที่ AtmosphereManager มีปุ่ม **Preview: <ชื่อเฟส>** และ **Clear preview**
- Preview ไม่แก้วัน/ความคืบหน้า และจะถูกยกเลิกเองเมื่อวันถัดไปเริ่มคำนวณเฟสใหม่
- ดู Console: `[Atmosphere] Phase -> ...` ทุกครั้งที่เฟสเปลี่ยน

## การเชื่อมกับ Flow
`GameFlowManager` เรียก `AtmosphereManager.Apply(day)` เมื่อ: เริ่มเกม/Main Menu, New Game (วัน 1, instant), Continue (วันที่โหลด), และหลัง `LoopManager.AdvanceDay()` ตอนนอน → เฟสคงอยู่ข้าม Resume/Company/Interview โดยไม่รีเซ็ต
เริ่มเกมใหม่ (Restart) จะกลับไปเฟสของวัน 1

## Atmosphere vs Daily Event
- Atmosphere = การเปลี่ยนถาวรตามวัน, Daily Event = เหตุการณ์ครั้งเดียว
- Daily Event มีช่อง `flickerSeconds` เพื่อกะพริบจอ **ชั่วคราว** (ไม่เปลี่ยนเฟส)

## สถานะการทดสอบ
โค้ด runtime + Editor คอมไพล์ผ่าน (ตรวจด้วย Roslyn ของ Unity) แต่ **ยังไม่ได้ทดสอบ Test A–J ใน Play Mode**
ข้อจำกัด: ยังไม่มี Post-processing (Volume) — ใช้ overlay/tint แทน; ไม่มีสไปรต์จริง (ใช้ placeholder ตามสี)

---

## 11. ระบบ Resume Confidence (แถบความมั่นใจแนวตั้ง)

ระบบนี้เป็นกลไก "เสริม" ของหน้า Resume Editor เท่านั้น **ไม่มีผลต่อการสัมภาษณ์ การถูกปฏิเสธ พ่อเสนอ หรือ Ending** (ค่าถูกเก็บไว้ใน GameSession เพื่อให้ dialogue/event ในอนาคตเรียกใช้ได้)

### 11.1 สคริปต์ที่สร้างใหม่ / แก้ไข

| ไฟล์ | สถานะ | หน้าที่ |
|---|---|---|
| `Scripts/Core/ResumeConfidenceCalculator.cs` | ใหม่ | คำนวณล้วน ๆ (ไม่พึ่ง UI, ไม่สุ่ม): ค่าเฉลี่ยเลขคณิตของ choice ที่เลือกอยู่ + ฟังก์ชันแสดงผล `76.7%` |
| `Scripts/UI/ResumeConfidenceUI.cs` | ใหม่ | แถบแนวตั้ง (เติมจากล่างขึ้นบน) + ข้อความ % + สี + แอนิเมชันนุ่ม ๆ (coroutine ไม่ใช้ DOTween) |
| `Scripts/Editor/ConfidenceTools.cs` | ใหม่ | สร้าง hierarchy ของแถบ, เมนูเพิ่มแถบลง scene เดิม, เมนูใส่ค่าตัวอย่าง |
| `Scripts/Data/ResumeChoiceData.cs` | แก้ | เพิ่ม `confidencePercentage` (0–100, ทศนิยมได้, clamp อัตโนมัติ) |
| `Scripts/Core/ChoiceManager.cs` | แก้ | `GetOverallConfidence()`; เก็บ confidence ลง History ตอน consume และล้างค่า |
| `Scripts/Core/GameSession.cs` | แก้ | `confirmedConfidence` (ค่าที่ยืนยันของใบสมัครวันนี้) และ `HistoryEntry.confidence` |
| `Scripts/Resume/ResumeEditorController.cs` | แก้ | อัปเดตแถบแบบ event-driven เมื่อเลือก/เปลี่ยนหมวด/เปิด Editor, บันทึกค่าตอนกด Confirm |
| `Scripts/UI/ChoiceButtonUI.cs` | แก้ | ป้าย % ทางขวาของปุ่ม (ซ่อนเองถ้าไม่มีข้อความ) |
| `Scripts/Editor/GameSceneBuilder.cs`, `SampleDataGenerator.cs` | แก้ | สร้างแถบ + ต่อ reference ให้, ใส่ค่าตัวอย่าง |

### 11.2 ตั้งค่า Confidence ของแต่ละ Choice

เปิด asset ใน `Assets/Data/Choices/` → ช่อง **Confidence Percentage** (สไลเดอร์ 0–100 พิมพ์ทศนิยมได้) แก้แล้วมีผลทันทีโดยไม่ต้องแก้โค้ด ค่านี้เป็นค่าคงที่ ไม่สุ่ม และโค้ดไม่เขียนทับ asset ตอนเล่น

ค่าตัวอย่างที่ใส่ให้: Big City 85, Small Town 60, Art College 45, Online 35 / Game Dev 90, Artist 75, Programmer 55, Writer 40 / Game Design 80, Technology 65, Music 50, Storytelling 30

- asset ที่สร้างใหม่จะได้ค่าตัวอย่างเอง
- asset เดิมที่ยังเป็นค่าเริ่มต้น 50: ใช้เมนู `Tools → First Jobber → 8. Apply Sample Confidence Values` (ไม่เขียนทับค่าที่คุณแก้ไปแล้ว)

### 11.3 วิธีคำนวณ

`Overall = ผลรวม confidence ของ choice ที่เลือกอยู่ ÷ จำนวน choice ที่เลือกอยู่`

- นับเฉพาะหมวดที่ "มี choice ถูกเลือกอยู่และยังไม่ถูก consume" (หมวดที่ไม่ได้เลือก/หมวดที่ choice หมดแล้ว ไม่ถูกนับเป็น 0%)
- ไม่ได้เลือกอะไรเลย = 0% และขึ้นข้อความ "No choices selected"
- เปลี่ยน choice ในหมวดเดิม = แทนค่าเดิม ไม่บวกซ้ำ (เพราะ 1 หมวดมี 1 การเลือก)
- จำนวนหมวดไม่ถูก hardcode (เพิ่มหมวดที่ 4 ได้เลย)
- ผลลัพธ์ถูกจำกัด 0–100 เสมอ ไม่มีการหารด้วยศูนย์

### 11.4 วางแถบไว้ขวาสุดของ Resume Editor

เลือก `Canvas/ResumeEditorPanel/ConfidencePanel` แล้วปรับ **Anchor Min/Max** (ค่าเริ่มต้น x 0.88–0.97, y 0.28–0.97) หรือปรับ `VerticalConfidenceBar` ข้างในเพื่อเปลี่ยนความกว้าง/สูงของแถบ แถบอยู่นอก ScrollRect ของรายการ choice จึงไม่เลื่อนไปตาม scroll และไม่บังการคลิก (ทุก Image ปิด Raycast Target และ CanvasGroup ปิด Blocks Raycasts)

**ใช้กับ scene เดิม (ไม่ต้อง Build ใหม่ ไม่ทับ UI ที่คุณแก้ไว้):** รัน `Tools → First Jobber → 7. Add Confidence Bar To Open Scene` เมนูนี้จะสร้างแถบ, ต่อ reference, บีบคอลัมน์ editor ให้แคบลง (ถึง x 0.86) และเพิ่มป้าย % ให้ ChoiceButton prefab จากนั้นกด Ctrl+S

### 11.5 Image ที่ศิลปินจะใส่สไปรต์จริง

ใน `ConfidencePanel → VerticalConfidenceBar` (ลากสไปรต์ใส่ช่อง **Source Image** ได้เลย ไม่ต้องแก้โค้ด):

| Image | หน้าที่ | หมายเหตุ |
|---|---|---|
| `BarBackground` | พื้นหลังแถบ (นิ่ง) | |
| `BarFillMask/BarFill` | ส่วนที่เติม (ขยับ) | สีเปลี่ยนตามช่วงอัตโนมัติ จึงควรใช้สไปรต์สีขาว/เทา |
| `BarFrame` | กรอบทับด้านบน | ไม่มีสไปรต์ = ใช้เส้นกรอบชั่วคราว (`PlaceholderFrameEdges`) แทน |
| `OptionalDecoration` | ของตกแต่ง | ไม่มีสไปรต์ = ซ่อน |

- ไม่มีสไปรต์ที่ `BarFill` ก็ใช้งานได้ (ระบบใช้สี่เหลี่ยมขาวแทน)
- สไปรต์ Sliced/ซับซ้อนที่ Filled แสดงไม่ดี: ที่ `ConfidencePanel → ResumeConfidenceUI` ตั้ง **Fill Mode = RectMask** (ใช้ `BarFillMask` ตัดแทน) ตัวเลขไม่เปลี่ยน
- Atmosphere: ใน Phase asset เพิ่ม Sprite Override ด้วย Target Id `ConfidenceBackground`, `ConfidenceFill`, `ConfidenceFrame`, `ConfidenceDecoration` เพื่อเปลี่ยนสไปรต์ตามช่วงเวลา (ไม่กระทบตัวเลข) ถ้าไม่ตั้ง จะใช้สไปรต์ที่ใส่ไว้ตามปกติ

### 11.6 Inspector ที่ต้องมี

**ResumeEditorController**: `Confidence UI` (ลาก `ConfidencePanel`), `Show Choice Confidence` (เปิด/ปิดป้าย % บนปุ่ม), `Choice Confidence Decimals`

**ResumeConfidenceUI** (ต่อให้อัตโนมัติเมื่อใช้ Builder / เมนู 7): `Bar Background`, `Bar Fill`, `Bar Frame`, `Decoration`, `Placeholder Frame Edges`, `Fill Mask`, `Title Text`, `Percent Text`, `Categories Text`

**ChoiceButton prefab → ChoiceButtonUI**: `Badge Text`

ปรับแต่งสี: `Bands` (ค่าเริ่มต้น 0 แดง, 40 เหลือง, 70 เขียว — แก้ตัวเลข "From Percent" และสีได้) / ความเร็วแอนิเมชัน: `Animation Duration` (0 = ไม่มีแอนิเมชัน) / จำนวนทศนิยม: `Decimals`

### 11.7 การเชื่อมกับ Loop

- กด Confirm: เก็บค่า Overall ลง `GameSession.confirmedConfidence` (ยังไม่ consume ตามเดิม)
- หลังถูกปฏิเสธ: `ConsumeApplication` เก็บค่านี้ลง History ของวันนั้น แล้วล้างค่าและล้างการเลือก
- วันถัดไป: แถบถูกคำนวณใหม่จากการเลือกใหม่เท่านั้น (ไม่ยกค่าวันก่อนมา)

### 11.8 วิธีทดสอบ

1. รัน `Tools → First Jobber → 8. Apply Sample Confidence Values` และ `7. Add Confidence Bar To Open Scene` (หรือ Build Game Scene ใหม่) แล้วกด Play
2. เข้า Resume Editor: แถบต้องว่าง 0% ข้อความ "No choices selected"
3. เลือก Education 60 + Dream 90 → 75% แล้วเลือก Passion 30 → 60% เปลี่ยนเป็น Passion 80 → 76.7% (แถบเติมจากล่างขึ้นบน)
4. เลื่อนรายการ choice: แถบต้องไม่ขยับ
5. Confirm → เล่นจนถูกปฏิเสธ → วันถัดไปแถบกลับเป็น 0% และ choice เดิมใช้ไม่ได้
6. แก้ Confidence Percentage ของ asset แล้ว Play ใหม่: ตัวเลขเปลี่ยนตาม

---

## 12. เมนูหลักแบบใบสมัคร (Resume Menu)

เมนูหลักเปลี่ยนเป็นกระดาษ Resume ใบใหญ่กลางจอ มีช่อง Name, Age และปุ่ม **Submit** ปุ่มเดียว (ไม่มี Continue / Quit) กด Submit แล้วกระดาษจะซูมออกไปเป็นหน้า Resume Editor เต็ม ๆ (ใช้แอนิเมชัน intro เดิมของ Resume Editor) ขั้นตอน Create Applicant แยกต่างหากจึงไม่ถูกใช้แล้ว

- สคริปต์: `UI/MainMenuController.cs` (เขียนใหม่), `UI/CreateApplicantController.cs` (เพิ่ม `Validate` ที่ใช้ร่วมกัน), `Core/GameFlowManager.cs` (`NewGame(name, age)` ไปหน้า Resume Editor ตรง ๆ), `Resume/ResumeEditorController.cs` (ช่อง `Intro Start From`)
- Scene ใหม่ (Build Game Scene): ได้เมนูแบบใหม่ทันที
- Scene เดิม: รัน `Tools → First Jobber → 9. Convert Main Menu To Resume Style` (ลบของเดิมใน MainMenuPanel แล้วสร้างใหม่ + ต่อ `Intro Start From` ให้) แล้วกด Ctrl+S
- ปรับขนาด/ตำแหน่งกระดาษ: เลือก `MainMenuPanel/MenuPaper` (Size Delta 950×860) — ตอนซูมออก ระบบอ่านตำแหน่งและความกว้างจากกระดาษนี้เองโดยอัตโนมัติ
- ปุ่ม Restart ในหน้า Ending จะกลับมาที่เมนู Resume นี้ (ให้ใส่ชื่อใหม่)
- Continue / Quit ยังรองรับแบบเลือกได้: ถ้าลากปุ่มใส่ช่อง `Continue Button` / `Quit Button` ของ MainMenuController ปุ่มจะทำงาน

---

## 13. เปลี่ยนสไปรต์ UI ตาม Phase (Atmosphere)

ใช้ระบบ Atmosphere เดิม: ใน Phase asset ช่อง **Sprite Overrides** ใส่สไปรต์ตาม Target Id ส่วนปุ่ม/พาเนลใน scene ต้องมี `AtmosphereSpriteBinding` ที่ Target Id ตรงกัน

1. รัน `Tools → First Jobber → 10. Bind Common UI To Atmosphere` (ใส่ Binding ให้ MenuPaper, SubmitButton, ChoiceList, ConfirmButton และ prefab ChoiceButton ซึ่งทุกปุ่มรายการใช้ร่วมกัน) แล้ว Ctrl+S
2. เปิด Phase asset (เช่น `Phase_02_Deterioration`) กดปุ่ม **Add Missing UI Slots** จะได้ช่องครบทุก Target Id
3. ลากสไปรต์ใส่ช่องที่ต้องการเปลี่ยนใน Phase นั้น ช่องที่ว่างจะใช้สไปรต์เดิม
4. UI อื่นที่อยากให้เปลี่ยนด้วย: เลือก GameObject ที่มี Image แล้วรัน `11. Bind Selected UI To Atmosphere` (ใช้ชื่อ object เป็น Target Id) จากนั้นใส่ Sprite Override ที่ชื่อเดียวกัน

- **Sprite Only** บน Binding: สลับแค่สไปรต์ ไม่แตะสี ใช้กับปุ่มและ ChoiceButton (สี Available/Selected/Consumed ยังควบคุมโดย ChoiceButtonUI) ถ้าใช้สไปรต์จริง ให้ตั้งสีเหล่านั้นเป็นสีขาวเพื่อไม่ให้ภาพคล้ำ
- แถบ Confidence ใช้ id `ConfidenceBackground/Fill/Frame/Decoration` อยู่แล้ว

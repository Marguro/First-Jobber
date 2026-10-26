# First Jobber — คู่มือเพิ่ม/แก้ไขคอนเทนต์ (ฉบับละเอียด เข้าใจง่าย)

คู่มือนี้เขียนให้คนที่ **ไม่ใช่โปรแกรมเมอร์** ทำตามได้ ทุกอย่างแก้ผ่านหน้าต่าง **Inspector** ใน Unity ไม่ต้องแตะโค้ด
อ่านตามลำดับครั้งแรกครั้งเดียว หลังจากนั้นใช้ **สารบัญ** และ **หัวข้อ 14 (ตารางอยากทำอะไร ไปอ่านตรงไหน)** เป็นทางลัด

## สารบัญ
- **ส่วน A: เริ่มต้น**
  1. ภาพรวมเกม: เนื้อหาไหนถูกใช้ตอนไหน
  2. ศัพท์ที่ต้องรู้ (อ่านก่อน!)
  3. ทดลองแก้ครั้งแรก (5 นาที)
  4. กฎและขั้นตอนมาตรฐาน (Workflow)
- **ส่วน B: คอนเทนต์ทีละอย่าง**
  5. การเขียนบทพูด (ใช้กับทุกที่)
  6. ผู้สมัคร: ชื่อและเพศ
  7. Resume: หมวดและตัวเลือก
  8. บริษัท
  9. HR ประจำบริษัท
  10. คำถามสัมภาษณ์
  11. บทพูดกลางใน GameDatabase
  12. Daily Event
  13. Ending และ Cutscene
- **ส่วน C: ภาพและบรรยากาศ**
  14. ตารางทางลัด: อยากทำอะไร ไปอ่านตรงไหน · 15. **Atmosphere: เปลี่ยน Sprite ของเมนู/UI ตามเฟส**
- **ส่วน D: ปรับแต่งอื่นๆ และแก้ปัญหา**
  16. ค่าปรับแต่งใน Scene · 17. เช็กลิสต์ · 18. แก้ปัญหา · 19. เมนู Tools

---

# ส่วน A: เริ่มต้น

## 1. ภาพรวมเกม: เนื้อหาไหนถูกใช้ตอนไหน

เกมเป็นลูป: **สมัครงาน → สัมภาษณ์ → โดนปฏิเสธ → นอน → วันใหม่** ซ้ำจนไม่เหลือบริษัทหรือตัวเลือก แล้วพ่อโทรมาเสนองาน → เลือก 1 ใน 3 Ending

ภาพด้านล่างบอกว่า "ในแต่ละช่วง ผู้เล่นเห็นเนื้อหาจากไหน" (ในวงเล็บคือที่ที่ต้องไปแก้)

```
เริ่มเกม
  └─ หน้าเมนู: กรอกชื่อ + เลือกเพศ ........................ (GameDatabase > Genders)
        │
        ▼
  ┌──────────────────────── วนซ้ำทุกวัน ─────────────────────────┐
  │ [Event: OnDayStart / BeforeResume] ........................ (Daily Event)
  │ หน้าแก้ Resume
  │   • Resume เลื่อนเข้ามา -> บทพูด Intro ..................... (GameDatabase > Resume Intro Lines)
  │   • คลิกเลือก Choice -> ผู้เล่นพูด (ครั้งเดียวต่อ Choice) ... (Resume Choice > Player Reaction)
  │   • แถบ Confidence ขยับ ................................... (Resume Choice > Confidence Percentage)
  │   • กด Confirm -> บทพูดตามระดับ Confidence ................. (GameDatabase > High/Low Confidence Confirmed Lines)
  │ [Event: AfterResumeConfirmed]
  │ เลือกบริษัท
  │   • เลือกแล้วผู้เล่นพูด .................................. (Company > Player Reaction)
  │   • บทพูดหลังยืนยันบริษัท ................................. (GameDatabase > Company Confirmed Lines)
  │ [Event: AfterCompanySelected / BeforeInterview]
  │ สัมภาษณ์ (ใช้ HR ของบริษัทที่เลือก)
  │   1) HR แนะนำตัว ......................................... (HR > Introduction)
  │   2) HR ดู Resume + พูดถึง Choice แต่ละตัว ................. (HR > Resume Review, Choice Reactions)
  │   3) ถามคำถามตาม Sequence ................................ (HR > Fixed / Resume Linked Questions)
  │      ผู้เล่นเลือกคำตอบ -> ผู้เล่นพูด -> HR ตอบ ............. (Question > Answers)
  │ [Event: AfterInterview]
  │ ถูกปฏิเสธ ................................................ (HR > Rejection)
  │ [Event: AfterRejection / BeforeSleep]
  │ นอน (จอดำ) -> วันใหม่ -> Atmosphere เปลี่ยนตามวัน ......... (Atmosphere Phase)
  └─────────────────────────────────────────────────────────────┘
        │ ไม่เหลือบริษัท/ตัวเลือก
        ▼
  พ่อโทรมา ............................................... (GameDatabase > Father Offer Lines)
  เลือก Ending (3 แบบ) -> ฉากจบ ........................... (Ending)
```

## 2. ศัพท์ที่ต้องรู้ (อ่านก่อน!)

| คำ | ความหมายแบบง่าย |
|---|---|
| **Asset** | ไฟล์ข้อมูล 1 ไฟล์ใน Project (เช่น 1 Choice = 1 ไฟล์ `.asset`) แก้ได้ใน Inspector |
| **Inspector** | หน้าต่างด้านขวาของ Unity ที่แสดงช่องให้กรอกเมื่อคลิกไฟล์/วัตถุ |
| **GameDatabase** | ไฟล์กลาง `Assets/Data/GameDatabase.asset` ที่ **ลิสต์ทุกอย่าง** ที่เกมใช้ ของที่ไม่อยู่ในนี้ เกมมองไม่เห็น |
| **Id** | ชื่อรหัสของ Asset (ตัวพิมพ์เล็ก ไม่มีเว้นวรรค เช่น `edu_big_city`) **ห้ามซ้ำ และห้ามเปลี่ยนทีหลัง** |
| **Block / Chunk** | บทพูด 1 ก้อน = ผู้พูด 1 คน พูดได้หลายหน้า (1 Element ของลิสต์บทพูด) |
| **Line / หน้า** | ข้อความ 1 บรรทัดใน Block ผู้เล่นคลิก 1 ครั้งต่อ 1 หน้า |
| **Mode** | วิธีเล่นชุดบทพูด: Sequential = เล่นทุก Block ตามลำดับ / RandomOne = สุ่ม 1 Block |
| **Choice** | ตัวเลือกใน Resume เช่น "Big City University" |
| **Category** | หมวดของ Choice เช่น Education, Dream, Passion |
| **HR** | ผู้สัมภาษณ์ประจำบริษัท (1 บริษัท : 1 HR) |
| **Fixed Question** | คำถามที่ HR ถามเสมอ ตามลำดับ ไม่สุ่ม |
| **Resume-Linked Question** | คำถามที่ถามเฉพาะเมื่อผู้เล่นเลือก Choice/หมวดที่ผูกไว้ |
| **Phase** | ชุดภาพ/สี/เสียงบรรยากาศ 1 ชุด เปิดใช้ตามวัน (เช่น ปกติ → เริ่มทรุด → สิ้นหวัง) |
| **Target Id** | ชื่อ "ช่อง" ของ UI ที่ Phase เปลี่ยน Sprite ให้ได้ (เช่น `MenuPaper`) |
| **Sprite** | รูปภาพที่ใช้ในเกม 2D |
| **Flag** | ธงจำสถานะในเกม ใช้เชื่อม Event เข้าด้วยกัน |
| **Token** | ตัวแทนข้อความในปีกกา เช่น `{name}` ที่เกมแทนด้วยค่าจริง |

## 3. ทดลองแก้ครั้งแรก (5 นาที)

เป้าหมาย: แก้บทพูดหนึ่งบรรทัดแล้วเห็นผลในเกม

1. ใน Project window ไปที่ `Assets/Data/Choices` แล้วคลิก `Dream_01_GameDev`
2. ใน Inspector หาช่อง **Player Reaction** > คลิก Element 0
3. กาง **Dialogue Lines** > แก้ข้อความ Line 0 เป็น `ฉันอยากทำเกมมาตั้งแต่เด็ก`
4. กด **Ctrl+S**
5. กด **Play** > กรอกชื่อ เลือกเพศ กด Submit > คลิก Choice "Game Developer"
6. กล่องบทพูดด้านล่างต้องแสดงข้อความที่คุณแก้
7. **หยุดเกมก่อนแก้อย่างอื่น** (การแก้ Asset ตอนกำลัง Play ถูกบันทึกจริง แต่ไม่ควรทำจนเคยชิน)

> ถ้าเลือก Choice นี้แล้วไม่มีบทพูด: ระบบให้ Choice แต่ละตัวพูด **ครั้งเดียวต่อการเล่น 1 รอบ** (ดูหัวข้อ 7.3) ให้กลับไปเมนูแล้วเริ่ม New Game ใหม่

## 4. กฎและขั้นตอนมาตรฐาน (Workflow)

### 4.1 ที่เก็บไฟล์

| เนื้อหา | โฟลเดอร์ |
|---|---|
| ตัวรวมทุกอย่าง | `Assets/Data/GameDatabase.asset` |
| หมวด / ตัวเลือก Resume | `Assets/Data/Categories`, `Assets/Data/Choices` |
| บริษัท | `Assets/Data/Companies` |
| HR และคำถามของ HR | `Assets/Data/HR`, `Assets/Data/HR/Questions` |
| คลังคำถามกลาง (แบบเก่า) | `Assets/Data/Questions` |
| Event / Ending | `Assets/Data/Events`, `Assets/Data/Endings` |
| Atmosphere Phase | `Assets/Data/Atmosphere` |

### 4.2 ขั้นตอนทุกครั้งที่เพิ่มของใหม่ (จำ 5 ข้อนี้)

1. **สร้าง Asset:** คลิกขวาในโฟลเดอร์ที่ถูกต้อง > `Create > First Jobber > ...` > ตั้งชื่อไฟล์ **ขึ้นต้นด้วยเลขเรียงลำดับ** เช่น `Edu_05_MyUniversity`
2. **กรอกข้อมูล:** เริ่มจากช่อง `Id` (ต้องไม่ซ้ำกับของอื่นประเภทเดียวกัน)
3. **ลงทะเบียน:** เมนู `Tools > First Jobber > Refresh Database Lists` (ดึงทุก Asset ใต้ `Assets/Data` เข้า GameDatabase เรียงตามชื่อไฟล์ เลขนำหน้าจึงกำหนดลำดับที่แสดง)
4. **ตรวจ:** เมนู `Tools > First Jobber > 3. Validate Content` แล้วเปิดแท็บ **Console** อ่านคำเตือนสีเหลือง/แดง
5. **บันทึก + ทดสอบ:** Ctrl+S แล้วกด Play

### 4.3 กฎทอง
- Id ซ้ำ หรือเปลี่ยน Id ทีหลัง = เซฟ/ประวัติ/ลิงก์พัง
- สร้างแล้วลืม Refresh = เกมไม่เห็นไฟล์นั้น
- **ความยาวเกม** = `min(จำนวนบริษัท, จำนวน Choice ของหมวดที่ใหญ่สุด)` ครั้งที่สมัคร เพราะทุกครั้งที่ถูกปฏิเสธ จะใช้บริษัท 1 แห่ง และ Choice 1 ตัวต่อหมวด อยากให้เกมยาวขึ้นต้องเพิ่มทั้งบริษัท **และ** Choice
- แก้บทพูดของ HR คนหนึ่ง ไม่กระทบ HR อีกคน

---

# ส่วน B: คอนเทนต์ทีละอย่าง

## 5. การเขียนบทพูด (ใช้กับทุกที่)

ทุกช่องบทพูดในเกมหน้าตาเหมือนกัน เรียนครั้งเดียวใช้ได้ทุกที่ (Resume Choice, Company, HR, คำถาม, Event, Ending ฯลฯ)

### 5.1 โครงสร้าง

```
Player Reaction                     <- ช่องบทพูด (ลิสต์ของ Block)
 ├─ Element 0   (Block 1)  ▼        <- คลิกเพื่อกางดู
 │    Speaker:            Player
 │    Emotion:            Neutral
 │    Portrait:           None
 │    Auto Advance Delay: 0
 │    Delay Before:       0
 │    Event Id:
 │    Gender Ids:         (ว่าง)
 │    Variant Group:      (ว่าง)
 │    Dialogue Lines [3]  ▼
 │        Line 0  [ That's the best I can make it.            ] ↑ ↓ X
 │        Line 1  [ I've sent so many applications already.   ] ↑ ↓ X
 │        Line 2  [ Now, who will take me?                    ] ↑ ↓ X
 │        [ + Add Line ]
 └─ Element 1   (Block 2)  ...
```

อ่านแบบง่าย: **Block 1 ก้อน = คนหนึ่งคนพูดต่อเนื่อง 3 หน้า** ถ้าจะให้อีกคนพูดต่อ หรือเปลี่ยนสีหน้า ให้เพิ่ม Block ใหม่

### 5.2 ทำอะไรได้บ้าง (ขั้นตอน)

| อยากทำ | ทำอย่างไร |
|---|---|
| เพิ่มหน้าใหม่ในคนเดิม | กาง Block > กาง **Dialogue Lines** > กด **+ Add Line** > พิมพ์ข้อความ |
| ลบหน้า | กดปุ่ม **X** ท้ายบรรทัด |
| สลับลำดับหน้า | กดปุ่ม **↑** **↓** |
| เพิ่มคนพูดใหม่ | เพิ่ม Element ใหม่ของลิสต์บทพูด (กดปุ่ม **+** มุมล่างของลิสต์) แล้วตั้ง Speaker |
| ลบทั้ง Block | คลิกเลือก Element ในลิสต์ แล้วกดปุ่ม **−** มุมล่างขวาของลิสต์ |
| ย้ายลำดับ Block | ลากที่จับ (ขีดสามขีดด้านซ้าย) ของ Element ขึ้น/ลง |
| แปลงข้อความเก่า | ถ้าเห็น "Legacy Text" กดปุ่ม **Move into Dialogue Lines** |

### 5.3 ความหมายแต่ละช่องของ Block

| ช่อง | ใช้ทำอะไร | ตัวอย่างค่า |
|---|---|---|
| **Speaker** | ชื่อผู้พูดที่แสดงในกล่องบทพูด `Player` = ชื่อผู้เล่น, `HR` = ชื่อ HR ของบริษัทที่เลือก, ชื่ออื่นพิมพ์เองได้ | `Player`, `HR`, `Father`, `Narrator` |
| **Emotion** | สีหน้า HR **ใช้เฉพาะ Speaker = HR** (กับ Player ไม่มีผล) | `Neutral`, `Interested`, `Disappointed`, `Uncomfortable` |
| **Portrait** | รูปผู้พูดข้างกล่องบทพูด (ไม่บังคับ) | ลาก Sprite ใส่ |
| **Auto Advance Delay** | `0` = รอผู้เล่นคลิก, มากกว่า 0 = ไปหน้าถัดไปเองหลังกี่วินาที (นับหลังพิมพ์ข้อความจบ) | `0` หรือ `2.5` |
| **Delay Before** | หน่วงก่อน Block นี้ปรากฏ (วินาที) | `0.5` |
| **Event Id** | ส่งสัญญาณให้ระบบอื่นตอน Block เริ่ม ทำงานครั้งเดียวต่อ Block | (ส่วนใหญ่เว้นว่าง) |
| **Gender Ids / Variant Group** | บทพูดตามเพศ (หัวข้อ 5.5) | ดู 5.5 |

### 5.4 การเล่นของผู้เล่น (พฤติกรรม)
- ข้อความ **พิมพ์ทีละตัวอักษร** คลิกระหว่างพิมพ์ = แสดงเต็มหน้า คลิกอีกครั้ง (หรือ Space/Enter) = หน้าถัดไป
- มีหน่วงสั้นๆ กันคลิกเลยโดยไม่ตั้งใจ
- ปรับความเร็วพิมพ์: ตัววัตถุ `DialogueManager` ใน Scene > **Characters Per Second** (ค่าเริ่มต้น 55)
- บทพูดตอนเลือก Choice ใน Resume เลื่อนเอง (Auto Advance) ตามค่า **Reaction Auto Advance** (2 วินาที) ที่ `ResumeEditorController` ใน Scene

### 5.5 บทพูดตามเพศ (ทำ Mr. / Ms.)

ตัวอย่างต้องการคำทักทายต่างกันตามเพศใน `HR > Introduction`:

1. สร้าง 3 Block ในช่อง Introduction
2. ใส่ **Variant Group** = `greeting` ทั้ง 3 Block (ชื่ออะไรก็ได้ แต่ต้องเหมือนกัน)
3. Block 1: **Gender Ids** = `male` (กด `+` ในลิสต์ แล้วพิมพ์) → บรรทัด: `Good morning, Mr. {name}.`
4. Block 2: **Gender Ids** = `female` → `Good morning, Ms. {name}.`
5. Block 3: **Gender Ids ปล่อยว่าง** → `Good morning, {name}.` (ตัวสำรองของเพศอื่นทั้งหมด)

ผล: ชาย → Block 1, หญิง → Block 2, เพศอื่น/ไม่ระบุ → Block 3

กฎจำง่าย:
- Block ที่ **ไม่มี Gender Ids และไม่มี Variant Group** = เล่นกับทุกเพศ (ไม่ต้องเขียนซ้ำ)
- Block ที่มี Gender Ids แต่ **ไม่มี Variant Group** = ถูกข้ามสำหรับเพศอื่น (ไม่มีตัวสำรอง)
- Block สำรอง (ไม่มี Gender Ids) ต้องอยู่ **Variant Group เดียวกัน** ถึงจะทำหน้าที่สำรอง
- เพศมีผลแค่ข้อความ ไม่เกี่ยวกับ Confidence หรือผลการปฏิเสธ
- ใช้ได้กับบทพูดทุกชุด รวมถึงข้อความคำถาม

### 5.6 Token (ตัวแทนข้อความ)
พิมพ์ในข้อความ เกมจะแทนค่าให้:

| พิมพ์ | ได้ |
|---|---|
| `{name}` หรือ `{PlayerName}` | ชื่อผู้เล่น |
| `{gender}` | ชื่อเพศที่แสดง เช่น Male |
| `{company}` | ชื่อบริษัทที่เลือก |
| `{job}` | ตำแหน่งงาน |
| `{day}` | วันที่ |
| `{hr}` | ชื่อ HR |

ถ้าพิมพ์ผิด (เช่น `{nmae}`) จะแสดงตามที่พิมพ์ `{age}` เลิกใช้แล้ว

### 5.7 Mode: Sequential กับ RandomOne

ช่องบทพูดที่มี **Mode** อยู่ข้างๆ (เช่น Resume Intro Mode, Introduction ของ HR) ตั้งได้ 2 แบบ:

| Mode | เกิดอะไรขึ้น |
|---|---|
| **Sequential** | เล่น **ทุก Block** ตามลำดับ |
| **RandomOne** | สุ่ม **1 Block** (โอกาสเท่ากัน ซ้ำเดิมได้) แล้วเล่นทุกบรรทัดใน Block นั้นตามลำดับ ไม่สุ่มบรรทัด |

ตัวอย่าง: ช่อง Resume Intro มี 3 Block ตั้ง RandomOne → แต่ละครั้งที่ Resume เลื่อนเข้ามา ผู้เล่นได้ยินแค่ 1 ใน 3 Block
- ใช้ร่วมกับเพศได้: ระบบกรองเพศก่อน แล้วค่อยสุ่มจากที่เหลือ
- ช่องที่ **ไม่มี Mode** (Event, Ending, Reaction ของ Choice/บริษัท) เป็น Sequential เสมอ
- Block ที่ว่างเปล่า (ไม่มีข้อความ) จะไม่ถูกสุ่มเลือก

---

## 6. ผู้สมัคร: ชื่อและเพศ

หน้าเมนูแรกเป็นกระดาษ Resume ผู้เล่นพิมพ์ **ชื่อ** และเลือก **เพศ** (Dropdown) แล้วกด Submit
- ตัวเลือกแรกของ Dropdown คือข้อความ prompt (เช่น "Select gender...") **เลือกส่งไม่ได้** ผู้เล่นต้องเลือกตัวเลือกจริง
- เพศเก็บเป็น **Gender ID** ตลอดทั้งเกม เซฟเก่าที่ไม่มีเพศจะเป็น `unspecified`

### 6.1 แก้ตัวเลือกเพศ
`GameDatabase.asset` > **Genders**

| ช่อง | คำอธิบาย | ตัวอย่าง |
|---|---|---|
| id | รหัส (ตัวพิมพ์เล็ก ห้ามเปลี่ยนทีหลัง) ใช้ในช่อง Gender Ids ของบทพูด | `male`, `female`, `non_binary`, `unspecified` |
| displayName | ข้อความใน Dropdown และ `{gender}` | `Male`, `Prefer Not to Say` |

- เพิ่มเพศใหม่: กด `+` ที่ลิสต์ > ใส่ id และ displayName
- ลำดับในลิสต์ = ลำดับใน Dropdown
- **ต้องมี id `unspecified` เสมอ** (ใช้กับเซฟเก่า)
- แก้ข้อความ prompt: ช่อง **Gender Prompt**
- ความยาวชื่อสูงสุด: **Max Name Length**

### 6.2 ถ้า Dropdown ไม่ขึ้น (Scene เก่า)
รันเมนู `Tools > First Jobber > 9. Convert Main Menu To Resume Style` ⚠️ **คำสั่งนี้ลบทุกอย่างใน MainMenuPanel แล้วสร้างใหม่** (ถ้าคุณตกแต่งเมนูเองไว้ จะหาย) หลังรันต้องรัน **เมนู 10** อีกครั้ง เพื่อผูก Sprite ของกระดาษเมนูใหม่กับ Phase
ทางเลือก: สร้าง `TMP_Dropdown` เอง แล้วลากใส่ช่อง **Gender Dropdown** ของ `MainMenuController`
ถ้าไม่ได้ใส่ Dropdown เกมยังเล่นได้ แต่เพศเป็น `unspecified` เสมอ และมีคำเตือนใน Console

---

## 7. Resume: หมวดและตัวเลือก

### 7.1 เพิ่ม Choice ใหม่ (ทีละขั้น)

1. ไปที่ `Assets/Data/Choices` > คลิกขวา > `Create > First Jobber > Resume Choice` > ตั้งชื่อ `Edu_05_MyUniversity`
2. กรอกช่องใน Inspector:

| ช่อง | ใส่อะไร | ตัวอย่าง |
|---|---|---|
| Id | รหัสไม่ซ้ำ | `edu_my_university` |
| Category | ลากหมวด (เช่น `Cat_01_Education`) มาใส่ | |
| Display Text | ข้อความบนปุ่ม | `My University` |
| Description | คำอธิบายใต้ชื่อ | `B.Sc. Computer Science` |
| Tags | แท็ก (ใช้กับเงื่อนไขของคำถาม ไม่บังคับ) | `tech` |
| **Confidence Percentage** | 0–100 ความแข็งแรงของตัวเลือกนี้ **ค่าคงที่** (Choice เดิมให้ค่าเท่าเดิมเสมอ) ไม่มีผลต่อการปฏิเสธหรือ Ending | `70` |
| Player Reaction | สิ่งที่ผู้เล่นพูดตอนเลือก (บทพูด) | |
| Hr Reactions | HR พูดอะไรถึง Choice นี้ (บทพูด; HR แต่ละคน override ได้ ดูหัวข้อ 9) | |

3. `Tools > First Jobber > Refresh Database Lists` แล้ว Validate
4. กด Play ทดสอบ: Choice ต้องปรากฏในหมวดนั้น

> ต้องมี Player Reaction และ Hr Reactions อย่างน้อย 1 Block ไม่งั้น Validate เตือน

### 7.2 เพิ่มหมวดใหม่ (เช่น Skills)
1. `Create > First Jobber > Resume Category`
2. กรอก **Id**, **Display Name** (ชื่อหมวดที่แสดง), **Empty Message** (ข้อความตอนไม่เหลือตัวเลือกในหมวดนั้น)
3. สร้าง Choice ในหมวดนั้นอย่างน้อย 1 ตัว (หมวดที่ไม่มี Choice จะถูกเตือน)
4. Refresh Database Lists

### 7.3 พฤติกรรมที่ควรรู้
- **Choice พูดครั้งเดียว:** Player Reaction ของแต่ละ Choice เล่น **ครั้งเดียวต่อการเล่น 1 รอบ** เลือกซ้ำยังเลือกได้ปกติ (Preview และ Confidence ยังอัปเดต) แต่ไม่พูดซ้ำ ประวัติเก็บในเซฟ และรีเซ็ตเมื่อเริ่ม New Game ถ้า Choice ไม่มีบทพูดให้เล่น จะไม่ถือว่าเล่นแล้ว
- **สลับลำดับ Choice:** ตัวเลือกในหมวดสลับตำแหน่งแบบสุ่มวันละครั้ง ปิดได้ที่ `ResumeEditorController` > **Shuffle Choices**
- **แสดงจำนวน Confidence บนปุ่ม:** `Show Choice Confidence` และจำนวนทศนิยม `Choice Confidence Decimals`
- **Choice ที่ถูกใช้ไปแล้ว** (หลังโดนปฏิเสธ) จะหายไป หรือเป็นสีเทาถ้าเปิด `GameDatabase > Show Consumed Choices`
- **ลบ Choice:** เอาออกจาก `GameDatabase` (หรือลบไฟล์) แล้ว Validate ดูว่ามีคำถามที่ยังลิงก์ถึง Choice นั้นหรือไม่

### 7.4 แถบ Confidence
แถบรวมค่า Confidence ของ Choice ที่เลือกอยู่ (ค่าเฉลี่ย) เปลี่ยนสีตามช่วง ปรับที่วัตถุ `ResumeConfidenceUI` ใน Scene:
- **Bands:** แต่ละช่วงมี **From Percent** และ **Color** (ช่วงใช้ตั้งแต่เปอร์เซ็นต์นั้นขึ้นไปจนถึงช่วงถัดไป)
- **Title Label**, **Empty Message**, **Categories Format**, **Decimals**, **Animation Duration**
- ภาพของแถบเปลี่ยนตามเฟสได้ (หัวข้อ 15.2)

---

## 8. บริษัท

### 8.1 เพิ่มบริษัท (ทีละขั้น)
1. `Assets/Data/Companies` > `Create > First Jobber > Company` > ตั้งชื่อ `Company_05_MyCompany`
2. กรอก:

| ช่อง | ใส่อะไร |
|---|---|
| Id | `company_my_company` (ไม่ซ้ำ) |
| Company Name / Job Title / Salary / Industry | ข้อมูลที่โชว์ในหน้าเลือกบริษัท |
| Description | คำอธิบาย |
| Logo | Sprite โลโก้ (ไม่บังคับ) |
| Tags | แท็ก (ใช้กับเงื่อนไขของคำถาม ไม่บังคับ) |
| **Hr** | **ลาก HR ของบริษัทนี้มาใส่ (สำคัญ ดูหัวข้อ 9)** |
| Player Reaction | ผู้เล่นพูดตอนเลือกบริษัทนี้ |
| Hr Intro / Rejection | แบบเก่า ใช้เป็นตัวสำรองถ้า HR ไม่มีบทพูดของตัวเอง ปกติเว้นว่างได้ |

3. Refresh Database Lists > Validate
- บริษัทที่ถูกปฏิเสธแล้วจะหาย หรือเป็นสีเทาถ้าเปิด `Show Rejected Companies`
- ระบบเตือนถ้าบริษัทไม่มี HR

---

## 9. HR ประจำบริษัท

**กฎใหญ่:** ผู้เล่นเลือกบริษัทไหน เกมโหลด HR ของบริษัทนั้นเสมอ ไม่สุ่ม ไม่ยืมของบริษัทอื่น ทุกอย่างที่ HR พูด ถาม ตอบ หน้าตา และบทปฏิเสธ มาจาก Asset HR ตัวนั้น

### 9.1 สร้าง HR ใหม่ (ทีละขั้น)
1. `Assets/Data/HR` > คลิกขวา > `Create > First Jobber > HR Character` > ตั้งชื่อ `HR_05_Mina`
2. กรอก **Id** (ไม่ซ้ำ เช่น `hr_mina`) และ **Hr Name** (ชื่อที่แสดงเป็นผู้พูด)
3. Refresh Database Lists (เพื่อให้เข้าลิสต์ Hr Characters)
4. เปิด Company ของบริษัทนั้น > ลาก HR ใส่ช่อง **Hr**
5. เติมบทพูด/คำถามตามตารางข้างล่าง แล้ว Validate

### 9.2 ช่องใน HR

| ช่อง | ความหมาย |
|---|---|
| Introduction (+Mode) | บทพูดแรกของ HR ตอนเข้าห้อง |
| Resume Review (+Mode) | พูดตอนเริ่มดู Resume (ก่อนพูดถึง Choice ทีละตัว) |
| **Choice Reactions** | ปฏิกิริยาเฉพาะของ HR คนนี้ต่อ Choice ที่ระบุ (ใส่ Choice + บทพูด) ถ้าไม่ใส่ Choice ไหน จะใช้ **Hr Reactions** ที่ตัว Choice แทน |
| Rejection (+Mode) | พูดหลังสัมภาษณ์จบ ก่อนจบวัน |
| Default Emotion | สีหน้าเริ่มต้น |
| Fixed Questions / Resume Linked Questions / Fallback Question / Sequence | หัวข้อ 10 |
| Emotion Visuals / Default Portrait / Atmosphere Variants / Animator Controller | ภาพ ดูหัวข้อ 15.8 |
| Company / Description | บันทึกช่วยจำ (ใช้ในข้อความตรวจสอบ) |

**ช่องบทพูดที่ว่าง** เกมจะถอยไปใช้ของบริษัท แล้วของกลางใน GameDatabase ตามลำดับ (เพื่อไม่ให้ข้ามขั้นตอนสัมภาษณ์)

### 9.3 ถ้าบริษัทไม่มี HR
- ถ้า GameDatabase มี **Fallback Hr** → ใช้ HR สำรองนั้น
- ถ้าไม่มี → ขึ้น **Error ใน Console** แล้วเกมสัมภาษณ์แบบกลางแบบเก่า (เพื่อไม่ให้เกมค้าง) **อย่าปล่อยให้เป็นแบบนี้ตอนส่งงาน**

### 9.4 สร้างตัวอย่างสำเร็จรูป
เมนู `Tools > First Jobber > 12. Generate Sample HR Content` สร้าง HR 4 คน (Iris, Gregor, Helena, Noel) พร้อมคำถาม ข้อความเป็น placeholder ผูกให้บริษัทที่ **ยังไม่มี HR** เท่านั้น ไม่ทับของเดิม ใช้เป็นแม่แบบแล้วแก้ข้อความ

---

## 10. คำถามสัมภาษณ์

สร้างที่ `Assets/Data/HR/Questions` ด้วย `Create > First Jobber > Interview Question`
⚠️ **คำถาม 1 Asset ใช้กับ HR ได้คนเดียว** อย่าลากไฟล์เดียวไปใส่สอง HR (Validate เตือน) ถ้าอยากได้คำถามคล้ายกัน ให้ Duplicate (Ctrl+D) แล้วแก้ Id

### 10.1 ช่องของคำถาม

| ช่อง | ความหมาย |
|---|---|
| Id | รหัสไม่ซ้ำ |
| Question Text | ข้อความคำถามที่แสดงบนจอ และที่ HR พูดถ้าไม่ได้ใส่ Question Dialogue |
| Hr Emotion | สีหน้า HR ตอนถาม |
| Question Dialogue | (ไม่บังคับ) ถ้าใส่ HR จะพูดตาม Block เหล่านี้แทน Question Text (หลายบรรทัด/ตามเพศได้) ข้อความบนจอใช้หน้าแรกที่ตรงเพศ |
| Linked Choice | ผูกกับ Choice ตัวเดียว (ดูหัวข้อ 10.3) |
| Linked Category | ผูกกับทั้งหมวด |
| Required Company / Required Tag | เงื่อนไขเสริม ปกติเว้นว่าง |
| Weight | ใช้จัดลำดับเมื่อมีหลายข้อที่ตรง (มากกว่ามาก่อน) |
| **Answers** | รายการคำตอบ (หัวข้อ 10.4) |

### 10.2 Fixed Question (ถามเสมอ ตามลำดับ)
1. สร้างคำถาม **ไม่ใส่** Linked Choice และ Linked Category
2. ลากใส่ลิสต์ **Fixed Questions** ของ HR
3. ลำดับในลิสต์ = ลำดับที่ถาม ไม่มีการสุ่ม
ตัวอย่าง: `Tell me about yourself.` → `Why this company?` → `Where do you see yourself in five years?`

### 10.3 Resume-Linked Question (ถามตามที่ผู้เล่นเลือก)
1. สร้างคำถาม แล้วเลือก **อย่างใดอย่างหนึ่ง**:
   - **Linked Choice** = ถามเมื่อผู้เล่นเลือก Choice นั้น (ตรงตัว สำคัญที่สุด) เช่น Dream = Game Developer → "ทำไมถึงเลือกเป็นนักพัฒนาเกม?"
   - **Linked Category** = ถามเมื่อผู้เล่นเลือก Choice ใดๆ ของหมวดนั้น
2. ลากใส่ลิสต์ **Resume Linked Questions** ของ HR
3. ลำดับที่ระบบเลือก (ไม่มีการสุ่ม):
   - ① ตรง Choice ก่อน ② ตรงหมวด
   - ในระดับเดียวกัน: ข้อที่ยังไม่เคยถามก่อน → Weight สูงก่อน → ตามลำดับในลิสต์
   - ไม่ถามข้อซ้ำในการสัมภาษณ์รอบเดียว
4. ถ้าไม่มีข้อไหนตรงเลย → ใช้ **Fallback Question** ของ HR คนเดียวกัน (**ควรใส่เสมอ**) → ถ้ายังไม่มีก็ใช้คำถามฉุกเฉินในโค้ด (เกมไม่ค้าง)
- คำถาม Resume-Linked ที่ไม่มี Linked Choice และ Linked Category จะไม่มีวันถูกถาม (Validate เตือน)

### 10.4 Answers (คำตอบ)
กด `+` เพิ่มคำตอบ แต่ละคำตอบ:

| ช่อง | ความหมาย |
|---|---|
| Id | ไม่ซ้ำภายในคำถามเดียวกัน (`a1`, `a2`, `a3`) |
| Text | ข้อความบนปุ่มคำตอบ |
| Player Reaction (+Mode) | ผู้เล่นพูดหลังเลือกตอบ |
| Hr Response (+Mode) | HR ตอบกลับ ตั้ง `Speaker = HR` และใส่ **Emotion** เพื่อให้หน้า HR เปลี่ยน |

ลำดับที่ผู้เล่นเห็น: ถามคำถาม → ผู้เล่นเลือกคำตอบ → ผู้เล่นพูด → HR ตอบ → (คำถามถัดไป)

### 10.5 Sequence (ลำดับการสัมภาษณ์)
ในช่อง **Sequence** ของ HR แต่ละ Element = คำถาม 1 ข้อ

| Type | ความหมาย |
|---|---|
| `Fixed` | ถามคำถาม Fixed ถ้าช่อง `Fixed Question` ว่าง จะหยิบข้อถัดไปที่ยังไม่ใช้จาก Fixed Questions; ถ้าลากคำถามใส่ จะใช้ข้อนั้นตรงๆ |
| `ResumeLinked` | หาคำถามที่ตรง Resume จาก Resume Linked Questions |

ตัวอย่าง:
- ว่าง = `Fixed` 1 ข้อ + `ResumeLinked` 1 ข้อ (ค่าเริ่มต้น)
- `Fixed, Fixed, ResumeLinked` = ถาม Fixed 2 ข้อ แล้วถามตาม Resume 1 ข้อ
- `ResumeLinked, Fixed` = ถามตาม Resume ก่อน
จำนวนคำถามต่อการสัมภาษณ์ = จำนวน Element ใน Sequence

---

## 11. บทพูดกลางใน GameDatabase

ชุดบทพูดที่ใช้ทั่วเกม อยู่ใน `GameDatabase.asset` ส่วน **Shared dialogue**

| ช่อง | เล่นตอนไหน | Mode |
|---|---|---|
| Resume Intro Lines | Resume เลื่อนเข้าหน้าแก้ไขตอนเริ่มวันแรก | มี |
| **High / Low Confidence Confirmed Lines** | กด Confirm Resume (ดูข้างล่าง) | RandomOne เสมอ |
| Resume Confirmed Lines (legacy) | ตัวสำรองสุดท้ายถ้า High และ Low ว่างทั้งคู่ | มี |
| Company Confirmed Lines | เลือกบริษัทแล้ว ก่อนสัมภาษณ์ (ใช้ `{company}` `{job}` ได้) | มี |
| Default Hr Intro | ตัวสำรองแนะนำตัวเมื่อ HR และบริษัทไม่มี | มี |
| Hr Review Lines | ตัวสำรองตอน HR ดู Resume | มี |
| Rejection HR / Rejection Player | ตัวสำรองการปฏิเสธ (ผู้เล่นพูดต่อจาก HR) | มี |
| Father Offer Lines | พ่อโทรมาก่อนเลือก Ending | มี |

### 11.1 บทพูดตอนกด Confirm Resume (แบบละเอียด)
1. ตอนกด Confirm ระบบ **บันทึกค่า Confidence รวม** ณ ตอนนั้น (ไม่คำนวณใหม่ระหว่างพูด)
2. เทียบกับ **Resume Confirmed Threshold** (ค่าเริ่มต้น 50):
   - Confidence **มากกว่า** Threshold → ใช้ชุด **High**
   - **เท่ากับหรือน้อยกว่า** (รวม 50 พอดี และ 0) → ใช้ชุด **Low**
3. สุ่ม **1 Block** จากชุดนั้น แล้วเล่นทุกบรรทัดเรียงลำดับ แล้วไปหน้าเลือกบริษัท
4. ถ้าชุดที่เลือกว่าง → ใช้อีกชุด → ถ้าว่างทั้งคู่ → ใช้ Resume Confirmed Lines (legacy) → ถ้าว่างหมดก็ไปต่อทันที

วิธีแก้: เพิ่ม/ลบ Block ในลิสต์ High หรือ Low ได้ตามต้องการ เลื่อน Threshold ที่แถบเลื่อน 0–100

---

## 12. Daily Event

Event = ฉากบทพูดพิเศษที่แทรกในเกมตามเงื่อนไข สร้างด้วย `Create > First Jobber > Daily Event` ที่ `Assets/Data/Events`

### 12.1 ช่องสำคัญ

| ช่อง | ความหมาย |
|---|---|
| Id | ไม่ซ้ำ |
| **Trigger** | จะเล่นตอนไหน (ตารางข้างล่าง) |
| Exact Day | `0` = ไม่สน, อื่นๆ = เล่นเฉพาะวันนั้น |
| Min Day | `0` = ไม่สน, อื่นๆ = ตั้งแต่วันนั้นเป็นต้นไป |
| Repeatable | เล่นซ้ำได้ทุกครั้งที่เข้าเงื่อนไขไหม (ถ้าไม่ติ๊ก เล่นครั้งเดียว) |
| Priority | หลาย Event เข้าเงื่อนไขพร้อมกัน ค่าสูงเล่นก่อน |
| Required Flag / Forbidden Flag | ต้องมี / ต้องไม่มี Flag นี้ถึงจะเล่น |
| Set Flags | Flag ที่ตั้งเมื่อ Event เล่น |
| Caption / Background | ข้อความ/ภาพเต็มจอ (ใช้กับ Trigger = OnDayStart) |
| Flicker Seconds | จอกระพริบชั่วคราว (ไม่เปลี่ยน Phase) |
| Lines | บทพูด |

### 12.2 Trigger

| Trigger | เล่นเมื่อ |
|---|---|
| OnDayStart | เริ่มวันใหม่ (ฉากเต็มจอ) |
| BeforeResume | ก่อนผู้เล่นเริ่มแก้ Resume |
| AfterResumeConfirmed | หลังกด Confirm Resume |
| AfterCompanySelected | หลังยืนยันบริษัท |
| BeforeInterview | ก่อนสัมภาษณ์ |
| AfterInterview | หลังสัมภาษณ์ ก่อนบทปฏิเสธ |
| AfterRejection | หลังบทปฏิเสธ |
| BeforeSleep | ก่อนนอน |

### 12.3 Flag (กาวเชื่อม Event)
ใช้ Flag ทำให้ Event หนึ่งเกิดหลังอีกอัน: Event A ตั้ง `Set Flags = saw_letter`, Event B ตั้ง `Required Flag = saw_letter` → B เกิดได้เฉพาะหลัง A

Flag ในตัวที่มีผลต่อภาพ: `resume_distorted` (ใบสมัครกระตุก/ตัวอักษรเพี้ยน) · `hr_strange` (HR สั่น พัดลมเร็วขึ้น จอกระพริบถี่)

---

## 13. Ending และ Cutscene

### 13.1 Ending
- ต้องมี **3 Ending** (Validate เตือนถ้าไม่ครบ) สร้างด้วย `Create > First Jobber > Ending`
- ช่อง: `Id`, `Title`, **Option Label** (ข้อความบนปุ่มเลือก), **Option Description**, `Lines` (บทพูดของฉากจบ), `Background` (Sprite) หรือ `Background Color`, `Final Text` (ข้อความท้ายสุด), `Sound`
- บทพ่อโทรมา แก้ที่ `GameDatabase > Father Offer Lines`

### 13.2 Cutscene (Timeline)
ที่ Ending แต่ละอัน:
- **Cutscene Prefab** (ง่ายสุด): Prefab ที่มี PlayableDirector + Timeline อยู่ในตัว ลากใส่ได้เลย
- **Timeline** (ขั้นสูง): ใส่ Timeline asset แล้วผูก Track ที่ Timeline Bindings ของ `EndingController`
- **Timeline Mode:** `BeforeDialogue` (เล่น Timeline ก่อนแล้วค่อยพูด) · `WithDialogue` (เล่นพร้อมกัน) · `InsteadOfDialogue` (เล่น Timeline อย่างเดียว) · `None` (ไม่ใช้)
- **Timeline Skippable:** กด Space/Enter/คลิกข้ามได้ (เมื่อเล่นโดดเดี่ยว)
- ครั้งแรกให้รันเมนู `6. Add Ending Timeline Support To Open Scene`

### 13.3 ทดสอบ Ending ทันที
ระหว่าง Play กดปุ่ม **`** (backquote) หรือ **F1** เปิด Debug Console แล้วกดปุ่ม Ending ได้เลย หรือพิมพ์ `endings` / `ending 2` (ไม่ลบเซฟ)

---

# ส่วน C: ภาพและบรรยากาศ

## 14. ตารางทางลัด: อยากทำอะไร ไปอ่านตรงไหน

| อยาก... | ไปที่ |
|---|---|
| แก้บทพูดตอนเลือก Choice | หัวข้อ 7.1 (Player Reaction) + 7.3 |
| เพิ่มตัวเลือก Resume | 7.1 |
| เพิ่มบริษัท | 8.1 |
| สร้าง HR ใหม่และผูกกับบริษัท | 9.1 |
| เพิ่มคำถามที่ถามเสมอ | 10.2 |
| เพิ่มคำถามตามที่ผู้เล่นเลือก | 10.3 |
| เปลี่ยนจำนวน/ลำดับคำถาม | 10.5 |
| ให้ HR เรียก Mr./Ms. | 5.5 |
| เพิ่มตัวเลือกเพศ | 6.1 |
| ให้บทพูดสุ่ม 1 ชุด | 5.7 |
| เปลี่ยนบทพูดตอนกด Confirm | 11.1 |
| เพิ่มฉากพิเศษ/เรื่องราว | 12 |
| แก้ Ending | 13 |
| เปลี่ยนกระดาษเมนู/ปุ่ม/กรอบตามเฟส | 15.4 |
| เปลี่ยนรูป HR ตามอารมณ์/เฟส | 15.8 |
| เพิ่มเฟสใหม่ | 15.6 |
| ปรับสี/แสง/เสียงของเฟส | 15.6 |
| ปรับความเร็วพิมพ์ข้อความ | 5.4 |
| ปรับสีแถบ Confidence | 7.4 |

## 15. Atmosphere: เปลี่ยน Sprite ของเมนู/UI ตามเฟส

### 15.1 หลักการ (อ่านให้เข้าใจก่อน)

เกมมี **3 Phase ตัวอย่าง** ที่เปลี่ยนตามวัน:

| Phase | Start Day | บรรยากาศ |
|---|---|---|
| Normal | 1 | ปกติ |
| Deterioration | 3 | เริ่มทรุด |
| Despair | 4 | สิ้นหวัง |

กฎการเลือก Phase: ใช้ Phase ที่ **Start Day สูงสุดที่ไม่เกินวันปัจจุบัน** (และ Enabled) เช่น วัน 1–2 = Normal, วัน 3 = Deterioration, วัน 4 ขึ้นไป = Despair Phase เปลี่ยนตอนข้ามวัน (หลังจอดำตอนนอน) และตอนเริ่มเกม/Continue

การเปลี่ยน Sprite ทำงานด้วย **คู่กุญแจ-ช่อง**:

```
       Phase (Asset)                              UI ใน Scene
 ┌──────────────────────────┐              ┌────────────────────────────┐
 │ Sprite Overrides         │              │ วัตถุ Image (เช่น MenuPaper)  │
 │   Target Id : MenuPaper  │ ◄── ตรงกัน ──►│ + AtmosphereSpriteBinding    │
 │   Sprite    : paper_old  │              │     Target Id : MenuPaper    │
 └──────────────────────────┘              └────────────────────────────┘
```

เมื่อ Phase เปลี่ยน ระบบดูว่า Phase ใหม่มี Sprite สำหรับ Target Id ของแต่ละ UI ไหม:
- **มี** → สลับเป็นรูปนั้น
- **ช่อง Sprite ว่าง / ไม่มีรายการ** → ใช้รูปต้นฉบับที่ตั้งไว้บน Image ใน Scene

ทุกอย่างเป็นเรื่องภาพ/เสียงเท่านั้น ไม่กระทบเกมเพลย์

### 15.2 ตาราง Target Id: เปลี่ยนอะไรได้บ้าง

| Target Id | คือรูปอะไร (ที่อยู่ใน Hierarchy) | ผูกด้วย |
|---|---|---|
| `MainBackground` | พื้นหลังเต็มจอหลังทุกหน้า (`Canvas/Background`) | เมนู 4 |
| `MenuPaper` | กระดาษ Resume ของเมนูหลัก (`MainMenuPanel/MenuPaper`) | เมนู 10 |
| `SubmitButton` | ปุ่ม Submit ในเมนูหลัก | เมนู 10 |
| `ResumeBackground` | กระดาษ Resume ฝั่งซ้ายตอนแก้ Resume (`ResumeEditorPanel/ResumePreview`) | เมนู 4 |
| `ChoiceListBackground` | กรอบรายการตัวเลือก (`ResumeEditorPanel/EditorGroup/ChoiceList`) | เมนู 10 |
| `ConfirmButton` | ปุ่ม Confirm Resume | เมนู 10 |
| `ChoiceButton` | **ปุ่มทุกปุ่มในลิสต์** (ตัวเลือก Resume, บริษัท, คำตอบสัมภาษณ์) ผูกไว้ที่ Prefab `Assets/Prefabs/ChoiceButton.prefab` | เมนู 10 |
| `CompanyPanelBackground` | การ์ดรายละเอียดบริษัท (`CompanyPanel/DetailsCard`) | เมนู 4 |
| `InterviewBackground` | ฉาก HR ตอนสัมภาษณ์ (`InterviewPanel/HRArea`) | เมนู 4 |
| `UIDecoration` | กล่องบทพูดด้านล่าง (`DialogueBox`) | เมนู 4 |
| `ConfidenceBackground` | พื้นหลังแถบ Confidence | ใช้ได้เลยหลังเมนู 7 (ไม่ต้องผูกเอง) |
| `ConfidenceFill` | ส่วนที่เติมของแถบ | เหมือนข้างบน |
| `ConfidenceFrame` | กรอบแถบ (ถ้าไม่มีรูป จะใช้เส้นขอบชั่วคราว) | เหมือนข้างบน |
| `ConfidenceDecoration` | ของตกแต่งข้างแถบ (ไม่มีรูป = ซ่อน) | เหมือนข้างบน |

**ไม่ได้อยู่ในระบบ Phase:** พื้นหลัง Ending (ตั้งที่ `EndingData.background`), รูป HR (ตั้งที่ตัว HR หัวข้อ 15.8), โลโก้บริษัท (`CompanyData.logo`)

### 15.3 ตั้งค่า Scene ครั้งแรก (ทำครั้งเดียว)
เปิด Scene เกมก่อน (`Assets/Scenes/Game.unity`) แล้วรันตามลำดับ:

| ลำดับ | เมนู (`Tools > First Jobber >`) | ผลลัพธ์ |
|---|---|---|
| 1 | **4. Add Atmosphere To Open Scene** | เพิ่ม `AtmosphereManager` + overlay + ช่องเสียง + ผูก `MainBackground`, `ResumeBackground`, `CompanyPanelBackground`, `InterviewBackground`, `UIDecoration` (ถ้ามีอยู่แล้ว จะไม่ทำซ้ำ) |
| 2 | **10. Bind Common UI To Atmosphere** | ผูก `MenuPaper`, `SubmitButton`, `ChoiceListBackground`, `ConfirmButton` และ Prefab `ChoiceButton` (รันซ้ำได้ ไม่ทำซ้ำของเดิม) |
| 3 | **7. Add Confidence Bar To Open Scene** | (ถ้ายังไม่มีแถบ Confidence) เพิ่มแถบ |
| 4 | (ไม่บังคับ) **5. Add Scrolling Background To Open Scene** | ทำให้พื้นหลังเลื่อนวนได้ |
| 5 | กด **Ctrl+S** | บันทึก Scene |

ถ้า Console ขึ้น "already has" แปลว่าเคยทำแล้ว ไม่ใช่ข้อผิดพลาด

### 15.4 ตัวอย่างเต็ม: เปลี่ยนกระดาษเมนูในเฟส Deterioration

เป้าหมาย: ตั้งแต่วันที่ 3 กระดาษ Resume ของเมนูหลักและฉากแก้ Resume เปลี่ยนเป็นกระดาษเก่า

**ขั้น 1: เตรียมรูป**
1. ลากไฟล์รูป (PNG) เข้า Project เช่นที่ `Assets/Sprites/Phase2/`
2. คลิกรูป > Inspector > **Texture Type = `Sprite (2D and UI)`** > กด **Apply**
3. ขนาดควรใกล้เคียงรูปเดิมของ UI นั้น ถ้าเป็นกรอบ/ปุ่มที่ยืดได้ ใช้ **Sprite Editor** ตั้งเส้น 9-slice (Border) ไม่งั้นรูปจะยืดเพี้ยน

**ขั้น 2: ใส่รูปใน Phase**
1. เปิด `Assets/Data/Atmosphere/Phase_02_Deterioration`
2. เลื่อนลงล่างสุด กดปุ่ม **Add Missing UI Slots** (เพิ่มช่องของ Target Id ทั้งหมดให้ครั้งเดียว)
3. ที่ **Sprite Overrides** หา element ที่ Target Id = `MenuPaper` → ลากรูปกระดาษเก่ามาใส่ช่อง **Sprite**
4. ทำเหมือนกันกับ `ResumeBackground`
5. Target Id ที่ไม่ได้ใส่รูป จะใช้รูปเดิม

**ขั้น 3: ทดสอบ**
1. กด **Play**
2. เลือกวัตถุ `GameFlowManager` ใน Hierarchy (มี `AtmosphereManager` อยู่บนวัตถุเดียวกัน)
3. ใน Inspector ส่วน **Preview** กดปุ่ม **Preview: Deterioration (day 3)** — ภาพต้องเปลี่ยนทันที ไม่ต้องเล่นถึงวัน 3
4. กด **Clear preview (back to current day)** เพื่อกลับ
5. หยุดเกม (การ Preview ไม่เปลี่ยนความคืบหน้าของเกมและไม่บันทึก)

### 15.5 ข้อควรรู้เรื่องการสลับภาพ
- ภาพ **จางลงแล้วสลับ** ตามค่า **Fade Duration** ของ Phase (วินาที)
- **ปุ่ม** (`SubmitButton`, `ConfirmButton`, `ChoiceButton`) ตั้งเป็น **Sprite Only**: เปลี่ยนแค่รูป ไม่ยุ่งกับสี เพราะสีปุ่มถูกควบคุมโดยระบบอื่น (เช่น เลือก/ใช้แล้ว) การสลับจึงทันที ไม่มี Fade
- ภาพอื่นถูกคูณสีด้วย **Ui Tint** ของ Phase ถ้ารูปดูมืด/เพี้ยน ให้ตรวจ Ui Tint (สีขาว = ไม่เปลี่ยนสี)
- ชื่อ Target Id **ตัวพิมพ์เล็ก-ใหญ่มีผล** ต้องตรงเป๊ะ

### 15.6 ตั้งค่า Phase และเพิ่ม Phase ใหม่

**เพิ่ม Phase:**
1. `Assets/Data/Atmosphere` > คลิกขวา > `Create > First Jobber > Atmosphere Phase`
2. ตั้ง **Id** (ไม่ซ้ำ), **Display Name**, **Start Day**, ติ๊ก **Enabled**
3. ลาก Asset เข้าลิสต์ **Phases** ของ `AtmosphereConfiguration.asset` (ลำดับในลิสต์ไม่สำคัญ)
4. ต้องมี Phase ที่ Start Day = 1 ไม่งั้นวันแรกๆ จะใช้ภาพต้นฉบับ (Validate เตือน) และห้ามมี 2 Phase เริ่มวันเดียวกัน

**ช่องของ Phase:**

| กลุ่ม | ช่อง | ผล |
|---|---|---|
| ทั่วไป | Id, Display Name, Start Day, Enabled | ระบุและเปิดใช้ Phase |
| สีจอ | Use Camera Color + Camera Color | เปลี่ยนสีพื้นหลังกล้อง |
| ม่านสี | Overlay Color + Overlay Opacity (0–1) | ม่านสีโปร่งทับทั้งจอ (อยู่ใต้กล่องบทพูด คลิกทะลุได้) |
| โทนสี | **Ui Tint** | คูณสี UI ทุกชิ้นที่ผูกไว้ (ขาว = ไม่เปลี่ยน) |
| โทนสี HR | Hr Tint | คูณสีภาพ HR (มีประโยชน์กับภาพชั่วคราว) |
| ความนุ่มนวล | Fade Duration | เวลาเปลี่ยนจาก Phase ก่อนหน้า |
| กระพริบ | Flicker Enabled / Interval / Intensity | จอกระพริบเป็นระยะ |
| เสียง | Ambient Clip / Ambient Volume / Audio Crossfade | เสียงบรรยากาศ ครอสเฟดตอนเปลี่ยน Phase |
| Sprite | Sprite Overrides | รูป UI ต่อ Target Id (15.4) |
| Sprite HR (เก่า) | Hr Sprites | ใช้เฉพาะตอนบริษัทไม่มี HR (หัวข้อ 15.8) |

### 15.7 เพิ่ม UI อื่นให้เปลี่ยนตามเฟส

**วิธีเร็ว:**
1. ใน Hierarchy เลือกวัตถุที่มีคอมโพเนนต์ **Image** (เลือกได้หลายตัว)
2. รันเมนู **11. Bind Selected UI To Atmosphere** — ระบบเพิ่ม Binding และใช้ **ชื่อวัตถุเป็น Target Id** (ถ้าวัตถุเป็นปุ่ม จะเป็น Sprite Only อัตโนมัติ)
3. เปิด Phase > **Sprite Overrides** > กด `+` > พิมพ์ Target Id ให้ตรงกับชื่อวัตถุเป๊ะ > ลากรูปใส่
4. Preview ใน Play Mode เพื่อตรวจ

**วิธีมือ:** ที่วัตถุ Image กด **Add Component > AtmosphereSpriteBinding** แล้วกรอก **Target Id** (ตั้งเองได้อิสระ) จากนั้นเพิ่ม Target Id เดียวกันใน Phase
ติ๊ก **Sprite Only** ถ้าสีของ Image ถูกสคริปต์อื่นควบคุม (เช่นปุ่ม) ปิด **Apply Ui Tint** ถ้าไม่อยากให้รูปนี้ถูกย้อมสีตาม Phase

### 15.8 รูป HR ตามอารมณ์และ Phase (ตั้งที่ตัว HR)

รูป HR **ไม่ได้ตั้งที่ Phase** แต่ตั้งที่ Asset ของ HR แต่ละคน (เพื่อไม่ให้ HR คนหนึ่งใช้รูปของอีกคน)

1. เปิด `HR_xx`
2. **Emotion Visuals:** เพิ่ม Element ต่ออารมณ์ (`Neutral`, `Interested`, `Disappointed`, `Uncomfortable`)
   - ช่อง **Sprite:** ลากรูปใส่ (ว่าง = ใช้สีทึบ + ชื่อ HR แทนรูป)
   - ช่อง **Tint:** สีย้อม/สีทึบชั่วคราว
3. **Default Portrait:** รูปสำรองเมื่ออารมณ์นั้นไม่มีรูป
4. **Atmosphere Variants:** เพิ่ม Element ต่อ Phase
   - **Phase:** ลาก Phase ที่ต้องการ (เช่น `Phase_03_Despair`)
   - **Sprites:** รูปต่ออารมณ์สำหรับ Phase นั้น (อารมณ์ที่ไม่ใส่ จะใช้รูปปกติ)
   - **Tint:** สีย้อมเฉพาะ Phase
5. **Animator Controller** (ไม่บังคับ): ใส่ที่ HR และลากวัตถุ `Animator` ของภาพ HR ไปใส่ช่อง **Animator** ของ `HRReactionController` ใน Scene

ลำดับที่เกมเลือกรูป: **รูปของ Phase นั้น → รูปอารมณ์ปกติ → Default Portrait → ข้อความ placeholder** (ภาพไม่ครบไม่ทำให้เกมพัง)
เมื่อ Phase เปลี่ยนกลางการสัมภาษณ์ รูปเปลี่ยนแต่อารมณ์ปัจจุบันไม่หาย
ช่อง **Hr Sprites** ของ Phase (แบบเก่า) ใช้เฉพาะตอนเป็นการสัมภาษณ์แบบกลางที่บริษัทไม่มี HR

### 15.9 พื้นหลังเลื่อน (Scrolling Background)
รันเมนู 5 แล้วใส่ Sprite ที่ Image ของ `Canvas/Background` ปรับที่คอมโพเนนต์ `ScrollingBackground`:

| ช่อง | ความหมาย |
|---|---|
| Mode | `Loop` วนต่อเนื่อง · `EdgeToEdge` แพนไปสุดขอบแล้วกลับ · `CornerToCorner` แพนมุมต่อมุม |
| Axis | `Horizontal` / `Vertical` / `Diagonal` |
| Speed | ความเร็ว (หน่วยต่อวินาที, 0 = นิ่ง, ติดลบ = กลับทิศ) |
| Scale | ซูมภาพ |
| Mirror Alternate Tiles | กลับด้านภาพที่ต่อกัน (ใช้ถ้ารูปไม่ต่อเนียน) |

Sprite ของ `MainBackground` ใน Phase ยังเปลี่ยนได้ตามปกติ (ระบบอ่านทุกเฟรม)

### 15.10 ภาพ/เสียงอื่นๆ ที่ไม่ใช่ Phase

| อยากเปลี่ยน | แก้ที่ไหน |
|---|---|
| โลโก้บริษัท | `Company > Logo` |
| พื้นหลัง/สีของ Ending | `Ending > Background / Background Color` |
| เสียงของ Ending | `Ending > Sound` |
| ภาพ/ข้อความเต็มจอตอนเริ่มวัน | `Daily Event > Background / Caption` |
| รูปผู้พูดข้างกล่องบทพูด | `Portrait` ของ Block บทพูด |
| เสียงบรรยากาศตามเฟส | `Ambient Clip` ของ Phase |

---

# ส่วน D: ปรับแต่งอื่นๆ และแก้ปัญหา

## 16. ค่าปรับแต่งใน Scene

เลือกวัตถุใน Hierarchy (ระหว่างไม่ Play) แล้วแก้ใน Inspector:

| วัตถุ | ช่อง | ใช้ทำอะไร |
|---|---|---|
| `DialogueManager` | Characters Per Second | ความเร็วพิมพ์ข้อความ |
| | Min Advance Delay | หน่วงกันคลิกข้ามหน้าโดยพลาด |
| `ResumeEditorController` | Shuffle Choices | สลับลำดับ Choice ในหมวดวันละครั้ง |
| | Reaction Auto Advance | วินาทีที่บทพูดตอนเลือก Choice เลื่อนเอง |
| | Show Choice Confidence / Choice Confidence Decimals | แสดงเปอร์เซ็นต์บนปุ่ม |
| | Intro Hold / Intro Duration / Intro Start Scale | แอนิเมชัน Resume เลื่อนเข้ามา |
| `ResumeConfidenceUI` | Bands, Title Label, Decimals, Animation Duration | แถบ Confidence |
| `HRReactionController` | Fan Speed, Sway Amount, Flicker Interval | ภาพเคลื่อนไหวพัดลม/โยกตัว/กระพริบของ HR |
| `GameFlowManager` | Sleep Hold | เวลาจอดำตอนนอน |
| `GameDatabase` | Show Consumed Choices, Show Rejected Companies, Max Name Length, Questions Per Interview | ค่าทั่วไป (Questions Per Interview ใช้กับการสัมภาษณ์แบบกลางเท่านั้น HR ใช้ Sequence แทน) |

## 17. เช็กลิสต์หลังแก้

- [ ] Refresh Database Lists
- [ ] Validate Content ไม่มีคำเตือนสำคัญ (ไม่มี HR, intro ว่าง, คำถามใช้ซ้ำ 2 HR, คำถามไม่มีคำตอบ)
- [ ] เล่นด้วยเพศ 2 แบบ และบริษัท 2 แห่ง เทียบชื่อ HR คำถาม บทพูดว่าต่างกัน
- [ ] Preview ทุก Phase ใน Play Mode ไม่มีรูปหาย/เพี้ยน
- [ ] ลองเลือก Choice เดิมซ้ำ (ไม่พูดซ้ำ) และ New Game (พูดได้อีก)
- [ ] Ctrl+S แล้ว commit

## 18. แก้ปัญหา

| อาการ | สาเหตุที่น่าจะเป็น | วิธีแก้ |
|---|---|---|
| Asset ใหม่ไม่ทำงาน | ไม่อยู่ใน GameDatabase | Refresh Database Lists |
| HR ไม่พูดบางช่วง | ช่องบทพูดของ HR ว่างและตัวสำรองก็ว่าง | เติมบทพูดให้ HR |
| HR ไม่ขึ้น/ใช้ HR ผิดคน | บริษัทยังไม่ได้ใส่ `Hr` | ใส่ที่ Company |
| บทพูดถูกข้าม | Gender Ids ไม่ตรง และไม่มี Block สำรองใน Variant Group เดียวกัน | เพิ่ม Block สำรอง (5.5) |
| สุ่มได้ Block เดิมตลอด | Mode เป็น Sequential หรือมี Block ที่ใช้ได้แค่ 1 อัน | ตรวจ Mode / เพิ่ม Block |
| คำถามไม่ถูกถาม | Resume-Linked ไม่มี Linked Choice/Category หรือผู้เล่นไม่ได้เลือก Choice นั้น | ตรวจการผูก |
| Choice ไม่พูดตอนคลิก | เคยพูดแล้วใน Playthrough นี้ หรือไม่มีบทพูด | เริ่ม New Game / เติมบทพูด |
| บทพูด Confirm ไม่ตรงระดับ | Threshold, หรือชุด High/Low ว่าง จึงใช้อีกชุด | ตรวจ 11.1 |
| Sprite ของ Phase ไม่เปลี่ยน | ไม่มี Binding บน UI นั้น · Target Id สะกดไม่ตรง · ช่อง Sprite ว่าง · รูปไม่ใช่ Sprite (2D and UI) · ไม่มี `AtmosphereManager` ใน Scene | ตรวจตาม 15.3–15.5 |
| รูปยืดเพี้ยนหลังเปลี่ยน | ไม่ได้ตั้ง 9-slice | ตั้ง Border ใน Sprite Editor |
| เมนูหลักไม่เปลี่ยนภาพหลังรันเมนู 9 | เมนู 9 สร้างกระดาษใหม่ Binding หาย | รันเมนู 10 ซ้ำ |
| ปุ่มไม่เปลี่ยนสีตาม Phase | ปุ่มเป็น Sprite Only โดยตั้งใจ | เปลี่ยนรูปแทนการย้อมสี |
| เห็น `{age}` ในข้อความ | Token อายุถูกยกเลิก | ใช้ `{name}` / `{gender}` |
| Dropdown เพศไม่มี / ได้ unspecified เสมอ | Scene เก่า | หัวข้อ 6.2 |
| เกมใช้การสัมภาษณ์แบบเก่า + Error ใน Console | บริษัทไม่มี HR | หัวข้อ 9.3 |

## 19. เมนู Tools > First Jobber

| เมนู | ทำอะไร |
|---|---|
| 1. Generate Sample Data | สร้างข้อมูลตัวอย่างใน `Assets/Data` (ไม่ทับของเดิม) |
| 2. Build Game Scene | สร้าง `Game.unity` ใหม่ทั้งหมด ⚠️ ใช้กับโปรเจกต์เริ่มใหม่ ระวังทับ Scene ที่ตกแต่งไว้ |
| 3. Validate Content | ตรวจ Id ซ้ำ ลิงก์หาย บทพูดว่าง HR เพศ Atmosphere |
| 4. Add Atmosphere To Open Scene | เพิ่มระบบ Atmosphere + ผูก UI หลัก |
| 5. Add Scrolling Background To Open Scene | พื้นหลังเลื่อน |
| 6. Add Ending Timeline Support To Open Scene | เพิ่ม PlayableDirector ให้ Ending |
| 7. Add Confidence Bar To Open Scene | เพิ่มแถบ Confidence |
| 8. Apply Sample Confidence Values | ใส่ค่า Confidence ตัวอย่างให้ Choice |
| 9. Convert Main Menu To Resume Style | สร้างเมนูหลักแบบกระดาษ Resume ใหม่ ⚠️ ลบของเดิมในเมนู |
| 10. Bind Common UI To Atmosphere | ผูก UI หลักและ Prefab ปุ่มกับ Phase |
| 11. Bind Selected UI To Atmosphere | ผูก UI ที่เลือกไว้ (Target Id = ชื่อวัตถุ) |
| 12. Generate Sample HR Content | สร้าง HR และคำถามตัวอย่างต่อบริษัท |
| Refresh Database Lists | เก็บ Asset ทั้งหมดใต้ `Assets/Data` เข้า GameDatabase |

---

เอกสารที่เกี่ยวข้อง: `GAME_SETUP_TH.md` (สถาปัตยกรรมและรายละเอียดเชิงเทคนิคของแต่ละระบบ), `HOW_TO_CHANGE_CONTENT.md` (ฉบับย่อภาษาอังกฤษ)

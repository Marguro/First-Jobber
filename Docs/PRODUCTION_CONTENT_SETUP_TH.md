# Production Content — วิธีตั้งค่าและใช้งาน

เอกสารนี้อธิบายเนื้อหา Production ที่ import จาก `ContentImport/FirstJobber_Production_Content/production_content_bilingual.json`
(3 หมวด, 15 Choice, 42 หน้าบทพูด, 15 คำถาม Resume-linked, 42 คำตอบ) รวมถึงตัวเลือกภาษา English / ไทย ที่ Main Menu

หลักการ: **อังกฤษเป็นต้นฉบับ** เก็บใน ScriptableObject ส่วน **ภาษาไทยอยู่ในตารางแปลอย่างเดียว** (`LocalizationTable_TH`)
ถ้าข้อความไหนไม่มีคำแปล เกมแสดงอังกฤษ ไม่แสดงช่องว่าง

---

## 1. สิ่งที่ถูกสร้างขึ้น

| อะไร | อยู่ที่ไหน |
|---|---|
| หมวด 3 อัน (`faculty`, `university`, `motto`) | `Assets/Data/Production/Resume/Categories/Cat_P_*.asset` |
| Choice 15 อัน (พร้อมบทพูดผู้เล่นหลายหน้า) | `Assets/Data/Production/Resume/Choices/Choice_P_*.asset` |
| คำถาม HR แบบ Resume-linked 15 ข้อ (พร้อมคำตอบ 42 ข้อ) | `Assets/Data/Production/Interview/ResumeLinkedQuestions/Q_P_*.asset` |
| Database ของ Production | `Assets/Data/Production/GameDatabase_Production.asset` |
| ตารางแปลไทย (เพิ่ม 117 คีย์) | `Assets/Resources/Localization/LocalizationTable_TH.asset` |
| CSV สำหรับส่งนักแปล | `Assets/Localization/translation_template.csv` |

ข้อมูลทดสอบเดิม (Demo) **ไม่ถูกแก้**: `Assets/Data/GameDatabase.asset` และ asset ใน `Assets/Data/...` ยังอยู่ครบ
`Refresh Database Lists` ข้ามโฟลเดอร์ `Production` เสมอ จึงไม่ปนกับ Demo

`GameDatabase_Production` ใช้บริษัท, HR, Event, Ending และบทพูดกลางร่วมกับ Demo (อ้างอิงถึงตัวเดียวกัน ไม่ได้ทำสำเนา)
แต่มีเฉพาะหมวดและ Choice ใหม่ และ **ช่อง Questions ว่างโดยตั้งใจ** (ดูหัวข้อ 4)

## 2. สลับระหว่าง Demo กับ Production

ซีนไม่ได้ถูกสลับให้เอง (ยังใช้ Demo อยู่) เลือกเองได้ 2 วิธี

**วิธี A — เมนู:** เปิดซีน `Game` แล้วเลือก
- `Tools > First Jobber > 16. Use Production Database In Scene`
- `Tools > First Jobber > 17. Use Demo Database In Scene`
จากนั้นกด Ctrl+S

**วิธี B — Inspector:** เลือก `GameFlowManager` ใน Hierarchy ลาก `GameDatabase_Production` ใส่ช่อง **Database**
ทำแบบเดียวกันที่ `DailyEventManager` (ช่อง Database)

> สลับกลับ Demo ได้ทุกเมื่อ ข้อมูล Save เก่าที่อ้าง Choice ของ Demo ใช้กับ Production ไม่ได้ ให้เริ่มเกมใหม่

## 3. เนื้อหาแต่ละ Choice

- **Display Text** = ภาษาอังกฤษ
- **Player Reaction** = 1 Block (Speaker = Player) ใน Block มีหลายหน้าเรียงตามต้นฉบับ เล่นครบทุกหน้า ไม่สุ่ม
- เลือกซ้ำ Choice เดิมจะไม่เล่น Reaction ซ้ำ (ระบบ "เล่นครั้งเดียวต่อ Choice" เดิมทำงานตามปกติ) แต่ Resume และ Confidence ยังอัปเดต
- **Tags** ใส่ `todo_confidence` ไว้เป็นเครื่องหมายว่ายังไม่ได้กำหนดค่า (ไม่มีผลต่อเกม)

## 4. คำถาม HR: ยังไม่ผูกกับ HR คนใด (ตั้งใจ)

ผู้เขียนยังไม่ระบุว่า HR คนไหนเป็นเจ้าของ จึง **ไม่ได้ใส่ให้ HR คนใดอัตโนมัติ** และไม่ได้ใส่ใน Database กลางด้วย

ทุกคำถามตั้งค่าเป็น:
- **Linked Choice** = Choice ของมันเอง (ตรงกันแบบ exact; ช่อง Linked Category เว้นว่าง)
- คำตอบแยกเป็นตัวเลือกละหนึ่งข้อ (ไม่รวมด้วย `/`)
- Player Reaction และ Hr Response ของคำตอบ **เว้นว่าง** (ยังไม่มีบทเขียน) เกมข้ามไปได้โดยไม่ค้าง

### วิธีกำหนด HR เจ้าของ

**วิธีที่ 1 — ใช้เมนู (แนะนำ)**
1. ใน Project window คลิกเลือก HR หนึ่งคน เช่น `Assets/Data/HR/HR_01_Iris`
2. `Tools > First Jobber > 18. Assign Production Questions To Selected HR`
3. กด Assign

เมนูจะใส่ 15 ข้อลงใน **Resume Linked Questions** ของ HR คนนั้นเท่านั้น
ถ้าข้อใด HR คนอื่นใช้อยู่แล้ว เมนูจะสร้างสำเนา (id เป็น `q_xxx__<hr>`) และคัดลอกคำแปลไทยตามไปให้ จึงไม่มีคำถามเดียวกันถูกแชร์ระหว่าง HR สองคน
รันซ้ำได้ ไม่ใส่ซ้ำ

**วิธีที่ 2 — ใน Inspector:** เลือก HR → **Resume Linked Questions** → ลากคำถามจากโฟลเดอร์ `ResumeLinkedQuestions` เข้าไป

HR ที่ยังไม่ได้รับคำถามจะไม่ถามคำถามชุดนี้เลย (ใช้ Fixed / Fallback เดิม)

## 5. การแปล

คีย์ที่ใช้ (สร้างจาก `LocalizationKeys` ตัวเดิม ไม่ได้คิดใหม่):

| ข้อความ | คีย์ |
|---|---|
| ชื่อหมวด | `category/<id>/displayName` |
| ชื่อ Choice | `choice/<id>/displayText` |
| บทพูดผู้เล่น แต่ละหน้า | `choice/<id>/playerReaction/0/lines/<หน้า>` |
| คำถาม | `question/<id>/questionText` |
| คำตอบ | `question/<id>/answers/<answerId>/text` |
| ชื่อเพศใน dropdown | `database/genders/<id>` และ `database/genderPrompt` |

ผลการ import: 3 หมวด + 15 Choice + 42 หน้า + 15 คำถาม + 42 คำตอบ = **117 คำแปล** ไม่มีคีย์ซ้ำ
บทพูดหลายหน้าแปลทีละหน้า แล้วค่อยแทน token เช่น `{name}`

Import ซ้ำได้อย่างปลอดภัย: ไม่สร้าง asset, คีย์, คำตอบ หรือบทพูดซ้ำ และ **ไม่ทับคำแปลที่มีอยู่แล้ว** (ถ้าต่างจาก JSON จะขึ้น Warning)

### เพิ่มหรือแก้คำแปลต่อ
1. `Tools > First Jobber > 12. Export Translation Template`
2. แก้คอลัมน์ `thai` ใน `Assets/Localization/translation_template.csv`
3. `Tools > First Jobber > 13. Import Translation CSV`

(ถ้า CSV มีคำแปลที่ยังไม่ import เมนู 12 จะไม่ทับ ให้รัน 13 ก่อน)

## 6. ตัวเลือกภาษาที่ Main Menu

- Dropdown "English / ไทย" อยู่มุมขวาบนของ Main Menu (`MainMenuPanel/LanguageDropdown`) ถูกเพิ่มในซีน `Game` แล้ว
- สคริปต์ `LanguageDropdown` ส่งค่าไปที่ `LocalizationManager.SetLanguage` เท่านั้น ไม่เก็บภาษาเอง
- เปิดเกมครั้งถัดไปจะจำภาษาจาก PlayerPrefs (`FirstJobber.Language`)
- เปลี่ยนภาษาแล้วข้อความทั้งเกมเปลี่ยนตาม รวมรายการเพศใน dropdown (ค่าที่เลือกไว้ยังอยู่) และ ไม่รีเซ็ต Choice, Confidence, เพศ, บริษัท หรือความคืบหน้า
- Listener ถูกถอนทุกครั้งก่อนลงทะเบียนใหม่ และการ sync ใช้ `SetValueWithoutNotify` จึงไม่ซ้ำและไม่วนลูป
- ปรับหน้าตาเอง: เปลี่ยน Image/Font ของ dropdown ได้อิสระ สคริปต์ใช้แค่ `TMP_Dropdown`; ข้อความ "English" / "ไทย" แก้ได้ที่ช่อง **Captions** ของ `LanguageDropdown`
- ซีนที่ยังไม่มี dropdown ให้รัน `Tools > First Jobber > 19. Add Language Selector To Main Menu` (รันซ้ำได้ ไม่สร้างซ้ำ)

## 7. สิ่งที่ยังค้าง (ต้องให้ผู้เขียนตัดสินใจ)

1. **HR เจ้าของคำถาม** — ยังไม่ผูก ดูหัวข้อ 4
2. **Confidence (%) ของ 15 Choice** — ผู้เขียนยังไม่ได้กำหนด ตั้งเป็นค่า default 50 ไว้ก่อน แก้ได้ที่ Inspector ของ `Choice_P_*` ช่อง **Confidence Percentage** (หา Choice ที่ติดแท็ก `todo_confidence`)
3. **บทตอบของ HR และอารมณ์หลังผู้เล่นเลือกคำตอบ** — ยังไม่มี ช่อง Hr Response ของคำตอบทั้ง 42 ข้อว่างอยู่
4. **Hr Reactions ของ Choice ใหม่** — ยังว่าง (HR จะข้ามตอนรีวิวเรซูเม่ ยกเว้น HR ที่มี Choice Reactions ของตัวเอง)
5. **ข้อสังเกตจากผู้เขียน JSON ที่ต้องให้นักเขียนตรวจ:**
   - ภาษาอังกฤษเป็นฉบับแปลใหม่ รอตรวจสำนวน
   - คำตอบสั้น `ครับ/ค่ะ` ยังเป็นข้อความเดียว ควรตัดสินใจว่าจะทำ Gender variant หรือไม่
   - ภาษาไทยของ `faculty_digital_art` ใช้ "นี้แหละ" ตามต้นฉบับ ตรวจว่าจะแก้เป็น "นี่แหละ" หรือไม่
   - ชื่อไทยของมหาวิทยาลัยใน JSON เป็น "มหาลัย A" ส่วนคำถามใช้ "มหาวิทยาลัย A" (คงตามต้นฉบับ)
6. **Validate Content** ของ Production DB จะเตือนว่ามี generic question น้อยกว่าที่แนะนำ เพราะไม่ได้ใส่คำถามกลางไว้ (ตั้งใจ)
7. **ชื่อ HR และบทพูดของ HR** ที่เป็นข้อมูลทดสอบยังไม่มีคีย์แปล (ยังเป็นอังกฤษในโหมดไทย)

## 8. ขั้นตอนทดสอบใน Unity (ยังไม่ได้ทดสอบ Play Mode)

1. เปิดซีน `Game` → เมนู 16 เพื่อใช้ Production DB → Ctrl+S
2. เมนู 18 ผูกคำถามกับ HR ที่ต้องการทดสอบ (HR ต้องผูกกับบริษัทในเกมด้วย)
3. Play: Main Menu ต้องเห็น dropdown ภาษา สลับ English ↔ ไทย แล้วรายการเพศเปลี่ยนภาษา
4. เริ่มเกม เลือก Choice → เล่นบทพูดครบทุกหน้า → เลือกซ้ำ Choice เดิมต้องไม่เล่นซ้ำ
5. เลือกบริษัทที่ HR ได้รับคำถามแล้ว → ต้องถามเฉพาะข้อที่ตรงกับ Choice ที่เลือก คำตอบแสดงเป็นปุ่มแยกกัน
6. ปิดเกมแล้วเปิดใหม่: ภาษาต้องคงเดิม
7. สลับภาษากลางเกมที่หน้า Resume Editor: ข้อความหน้าจอเปลี่ยน (บทพูดที่แสดงอยู่แล้วจะเปลี่ยนในบรรทัดถัดไป)

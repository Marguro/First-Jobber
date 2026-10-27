# First-Jobber — HR Production Content v2 (5 companies)

## เปลี่ยนจากเวอร์ชันก่อนหน้า

1. **HR แอฟแฟร์/หัวงู**: Intro และ Rejection เลือกตามเพศใน `ApplicantProfile.genderId` ที่ผู้เล่นกรอกตอนเริ่มเกม (`male` / `female`) โดยตรง ไม่ใช่สุ่มเพศ ไม่ต้องสร้าง HR สองคน และ **ไม่ได้เพิ่มบทพูดสำรองที่ผู้ใช้ไม่ได้เขียน** หากเลือกเพศอื่นให้ใช้ข้อความ neutral ที่เขียนไว้แล้ว (ถ้ามี) หรือเตือนว่าขาดเนื้อหาและข้ามบทพูดที่ไม่มีอย่างปลอดภัย
2. **Control Group**: เติมตัวเลือกคำตอบของ Fixed Question 2 และ 3 ข้อละ 3 ตัวเลือก ตามข้อความที่ผู้ใช้ให้มา โดยทำ English source + Thai translation
3. **ทุก HR ถาม 4 ข้อต่อ Interview**: 2 Fixed Question สุ่มจาก HR คนนั้น + 2 Resume-Linked Question สุ่มจากตัวที่ Match Resume จริงและอยู่ใน Bank ของ HR คนนั้น ทั้งสองประเภทสุ่ม **โดยไม่ซ้ำคำถามภายใน Interview** และแสดงตามลำดับ Fixed, Fixed, Linked, Linked เป็นค่าเริ่มต้น

## ไฟล์ในแพ็ก

- `hr_production_bilingual.json` — ข้อมูลบริษัทและ HR ทั้ง 5 คน, 15 Fixed Questions, คำตอบ 39 ข้อ, InterviewPlan ต่อ HR
- `CLAUDE_HR_IMPORT_PROMPT.md` — Prompt ฉบับเต็มสำหรับกรณียังไม่ Import เวอร์ชันก่อน
- `CLAUDE_HR_PATCH_PROMPT.md` — Prompt สำหรับ **กรณีนำเข้าเวอร์ชันก่อนแล้ว** ให้แก้ข้อมูลเดิมโดยใช้ Stable ID เดิมและไม่ทำ Duplicate Asset
- `IMPORT_STATUS.json` — จำนวนข้อมูลและรายการที่ยังต้องเติม
- `references/` — ภาพต้นฉบับผู้ใช้ (ใช้ตรวจข้อความ ไม่ใช่ Sprite Production)

## ชุดคำตอบ Control Group ที่เติม

| Question | คำตอบที่ 1 | คำตอบที่ 2 | คำตอบที่ 3 |
|---|---|---|---|
| Q2 ลูกค้าแก้งานซ้ำ | สามารถยอมปรับตามได้ทุกอย่าง | อาจจะมีการพูดคุย | คิดว่าได้นะ |
| Q3 งานที่ไม่ตรงความชอบ | ยินดี | จะพยายามเรียนรู้ | คิดว่าได้นะ |

## ข้อสำคัญเรื่อง Resume-Linked 2 ข้อ

ข้อมูลนี้ **ไม่ได้สร้างคำถาม Resume ใหม่เอง** เพราะเคยนำเข้าชุด Production Resume จากคณะ/มหาวิทยาลัย/คติประจำใจไว้ก่อนแล้ว (รวม 15 ข้อ) แต่ผู้ใช้ยังไม่ได้ระบุว่าแต่ละ HR มีสิทธิ์ถามข้อใดเป็นพิเศษ ดังนั้นการได้ **ครบ 4 ข้อจริง** ต้องกำหนด `Resume-Linked Questions` ที่ HR แต่ละคนให้มีตัวเลือกที่ Match Resume อย่างน้อย 2 ข้อในทุกกรณีที่ต้องการเล่นจริง อย่าดึง Question ของ HR อื่นมาใช้เงียบ ๆ ถ้าไม่ครบต้องแจ้งเตือนและไม่ทำเกมค้าง

การสุ่ม Fixed ไม่ผูก Resume; Resume-Linked ต้อง Match Resume แล้วจึงสุ่มจากตัวที่ผ่านเงื่อนไข ภายในหนึ่ง Interview ห้ามได้คำถามซ้ำ และห้ามสุ่มใหม่เมื่อคลิกหน้าถัดไป

## วิธีใช้

1. แตก ZIP ไปไว้ที่ `<UnityProjectRoot>/ContentImport/HRProduction/`
2. ถ้ายังไม่ Import เลย: ส่ง `CLAUDE_HR_IMPORT_PROMPT.md` ให้ Claude Code
3. ถ้า Import ไปแล้ว: ส่ง `CLAUDE_HR_PATCH_PROMPT.md` ให้ Claude Code เพื่อแก้ Content และ Interview Manager แบบไม่ทำ Duplicate Assets
4. ให้ Claude ตรวจ References ของ `CompanyData -> HRCharacterData`, `HRCharacterData -> FixedQuestions`, `HRCharacterData -> ResumeLinkedQuestions`
5. English อยู่ใน ScriptableObject; Thai ลง `LocalizationTable_TH.asset` ด้วยคีย์ที่ระบบเดิมสร้าง และไม่ overwrite CSV ที่มีงานแปลค้าง
6. ทดสอบ Play Mode ว่า Intro/Rejection HR หัวงูตรงกับเพศ, Control Group เลือกตอบได้ทั้ง Q2/Q3 และ 4 Questions เกิดจริงเมื่อ Linked Pool ครบ

## ยังต้องเขียนเพิ่ม (ไม่ได้แต่งข้อมูลแทน)

- บท HR ตอบกลับหลังผู้เล่นเลือกคำตอบ
- รายการผูก Resume-Linked Question สำหรับ HR แต่ละคนให้รับประกันว่ามี 2 ข้อที่ Match ในทุก Resume ที่ต้องการรองรับ
- บท gender-neutral ของ HR แอฟแฟร์สำหรับเพศที่ยังไม่มีข้อความ (เฉพาะกรณีต้องการใช้)
- Sprite/Animation จริงของ HR และ UI

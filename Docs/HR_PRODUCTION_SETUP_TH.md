# HR Production v2 — คู่มือตั้งค่า (ไทย)

## ไฟล์ข้อมูลที่ได้
| ที่อยู่ | เนื้อหา |
|---|---|
| `Assets/Data/Production/Companies/Company_P_*.asset` | 5 บริษัท (id `prod_company_*`) |
| `Assets/Data/Production/HR/HR_P_*.asset` | 5 HR (id `prod_hr_*`) |
| `Assets/Data/Production/Interview/FixedQuestions/Q_P_*.asset` | 15 Fixed Question (HR ละ 3 ข้อ) |
| `Assets/Data/Production/GameDatabase_Production.asset` | companies/hrCharacters = ชุด Production 5 ชุด (Demo DB ไม่ถูกแตะ) |

บริษัท ↔ HR: `prod_company_X` ↔ `prod_hr_X` (focusframe, redline, ideaharvest, appfair, controlgroup) ตามชื่อใน JSON — ผูกทั้ง `Company.hr` และ `HR.company`

## เมนู Unity
- `Tools > First Jobber > 20. Import HR Production Content` — สร้าง/อัปเดต (รันซ้ำได้ ไม่สร้างซ้ำ GUID เดิมคงเดิม ไม่ทับคำแปลไทยเดิม ไม่ทับบทที่เขียนเองใน HR)
- `Tools > First Jobber > 21. Validate HR Production Content` — รายงาน (อ่านอย่างเดียว)
- `Tools > First Jobber > 18. Assign Production Questions To Selected HR` — ใส่ Resume-Linked ให้ HR ที่เลือก

## ลำดับ 4 คำถาม (ตั้งใน HR Inspector)
`Sequence` = Fixed, Fixed, Resume Linked, Resume Linked (แก้ลำดับ/จำนวนได้)
- `Fixed Pick Mode = Random` : สุ่ม Fixed จากคลังของ HR นั้น ไม่ซ้ำใน Interview เดียว
- `Resume Linked Pick Mode = Random` : สุ่มจากคำถามที่ HR ถูกกำหนดไว้และตรง Resume (ตรง choice หรือ category) ไม่ซ้ำ
- `InOrder` = พฤติกรรมเดิม (HR Demo ยังใช้ค่านี้)
- สุ่มครั้งเดียวตอนเริ่ม Interview ไม่สุ่มใหม่เมื่อกดข้ามบทพูด; ลำดับคำตอบไม่ถูกสุ่ม
- ถ้าคำถามไม่ครบ จะถามเท่าที่มี (มี Warning) ไม่ค้าง

## เพศ (AppFair)
Intro/Rejection มี block `male` และ `female` ใช้ `ApplicantProfile` gender ตอนสมัคร; กรองเพศก่อน RandomOne; ไม่มีบทสำรองสำหรับเพศอื่น (เมนู 21 จะเตือน ถ้า DB มีเพศที่ไม่มีบท — ตอนนี้ Production DB มีแค่ male/female จึงไม่เตือน)

## Control Group
Q2 และ Q3 มีตัวเลือก 3 ข้อตามลำดับที่กำหนด (อังกฤษใน asset / ไทยใน `LocalizationTable_TH`) ไม่มีบท HR ตอบกลับ

## สิ่งที่ยังต้องทำเอง
1. **Resume-Linked ของ HR ทั้ง 5 ยังว่าง** (ข้อมูลไม่ได้ระบุว่า HR ไหนถามข้อไหน) → เลือกแต่ละ HR แล้วรันเมนู 18 หรือลากคำถามใส่ `Resume Linked Questions` เอง (แต่ละ HR ต้องมี asset ของตัวเอง เมนู 18 จะสร้างสำเนาให้เมื่อซ้ำ)
2. บท HR ตอบหลังเลือกคำตอบ, Portrait/Animator, `Company.playerReaction`, jobTitle/salary/logo — ยังไม่มีข้อมูล
3. เพิ่มคำแปลใหม่: เมนู 12 Export → แก้ CSV → เมนู 13 Import

# First-Jobber — Production Content Import

ไฟล์ในชุดนี้มีไว้ส่งต่อให้ Claude Code ที่เปิดอยู่ใน Root ของ Unity Project **First-Jobber**:

- `production_content_bilingual.json` — 3 หมวด, 15 Choices, 42 หน้าบทพูดผู้เล่น, 15 คำถาม HR แบบ Resume-linked, 42 ตัวเลือกคำตอบ; อังกฤษเป็นต้นฉบับ และมีคำแปลไทยคู่กันเพื่อ import เข้า LocalizationTable_TH
- `CLAUDE_IMPORT_PROMPT.md` — คำสั่งให้ Claude นำข้อมูลเข้าระบบ ScriptableObject / Localization / Main Menu โดยเก็บแยกจากข้อมูลทดสอบ

## วิธีใช้

1. สำรอง/Commit โปรเจกต์ก่อน
2. คัดลอกสองไฟล์ไปไว้ที่ Root ของโปรเจกต์ Unity หรือโฟลเดอร์เช่น `ContentImport/`
3. เปิด Claude Code ในโฟลเดอร์ Unity เดียวกัน
4. ส่งข้อความ: `Read ContentImport/CLAUDE_IMPORT_PROMPT.md and ContentImport/production_content_bilingual.json in full, then implement the requested production content import safely.`
5. ตรวจผลลัพธ์ผ่าน Console และ Editor; เลือก HR ที่จะผูกคำถามจริงผ่าน Inspector เมื่อได้ข้อสรุป

## ขอบเขตที่ยังต้องระบุ

- ยังไม่ระบุ Confidence (%) ของ 15 Choices: ไม่สร้างค่าจริงขึ้นมาเอง
- ยังไม่ระบุ HR คนใดเป็นเจ้าของ Question Bank: **ห้าม** ผูกให้ทุก HR อัตโนมัติ
- ยังไม่มีบทพูดตอบกลับของ HR ต่อคำตอบ 42 แบบ: เว้นเป็นค่าไม่บังคับหรือ placeholder สำหรับการทดสอบ
- คำตอบสั้น `ครับ/ค่ะ` อาจต้องเขียน variant สำหรับ gender/neutral ในรอบขัดเกลา
- ข้อความไทยคำแรกของคณะ Digital Art ใช้ `นี้แหละ` ตามต้นฉบับ; ตรวจว่าอยากแก้เป็น `นี่แหละ` หรือไม่

ข้อมูลไทยแยกหน้าบทพูดตามเนื้อหาที่ส่งมา และแปลอังกฤษใหม่เพื่อเป็น source text ของ ScriptableObject โปรดตรวจสำนวนอังกฤษก่อนส่งเกมจริง

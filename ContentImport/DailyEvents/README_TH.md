# First-Jobber — Production Daily Events, Day 1–6

## วิธีใช้

1. แตก ZIP แล้วนำโฟลเดอร์ `DailyEvents` ที่มีไฟล์ `day_events_production_th.json` และ `CODEX_IMPORT_PROMPT.md` ไปไว้ที่ `First-Jobber/ContentImport/DailyEvents/` (ระดับเดียวกับ `Assets/`, `Packages/`, `ProjectSettings/`).
2. เปิด Codex CLI ที่ Root ของ Unity Project เดิม.
3. สั่ง: `Read ContentImport/DailyEvents/CODEX_IMPORT_PROMPT.md and ContentImport/DailyEvents/day_events_production_th.json completely, then implement the Production day events.`
4. ให้ Codex ตรวจ Asset Production จริงก่อน Import เพื่อป้องกันการสร้างซ้ำ และให้เปิด Unity ทดสอบ Play Mode หลังแก้เสร็จ.

## ข้อมูล

- Event Trigger 13 กลุ่ม รวม Day 1 x4, Days 2–5 x2 ต่อวัน และ Day 6 x1.
- บทพูด Event รวม 54 หน้า (Sequential) และบท Preview สำหรับ Ending อีก 4 หน้า.
- โทรศัพท์ Day 2 และ Day 4 มี Popup ให้เลือก 2 ข้อต่อสาย.
- Day 6 มี 3 ทางจบ โดยต้องคลิกเส้นทาง → ฟังบท Preview → กดยืนยันก่อนเล่น Ending.
- Day 6 ไม่มี Resume/เลือกบริษัท/สัมภาษณ์.
- `lines_th` หนึ่งข้อความคือหนึ่งหน้า Dialogue ที่ต้องกดเดินต่อ **ไม่ใช่ RandomOne**. `RandomOne` ของ Confidence Dialogue เป็นอีกระบบหนึ่ง.

## สิ่งที่ต้องตรวจด้านบท

1. **Ending 1/2:** ผู้ใช้เขียน Option 1 = สอบข้าราชการ แต่ Preview ว่า "พ่อก็คงไม่ปฏิเสธ..."; Option 2 = ทำงานกับพ่อ แต่ Preview กล่าวถึง "งานที่มั่นคง". อาจสลับบริบทกัน — ไฟล์เก็บตามหมายเลขที่ผู้ใช้ระบุ ไม่ได้สลับให้เอง.
2. ต้นฉบับมีการสะกด/วรรคตอนผิดเล็กน้อย จึงปรับให้อ่านได้โดยไม่เปลี่ยนใจความ เช่น `เป็ยไง` → `เป็นไง`, `โ?รมาถาม` → `โทรมาถาม`, `งั้นไปไปคิดดูนะ` → `งั้นไปคิดดูนะ`, เครื่องหมายคำพูดซ้อนและช่องว่างหน้าประโยค.
3. ประโยค `เพื่อน: ไง...ชื่อ...` เปลี่ยนส่วนชื่อเป็น `{name}` เพื่อให้แสดงชื่อผู้เล่นที่เลือกจริง (ตรวจว่า DialogueManager ใช้ Token เดียวกันก่อน Import).
4. ยังไม่มี AudioClip สำหรับสายเข้า/วางสาย, ตัวเลือกโทรศัพท์ยังไม่มีผลแตกแขนงที่ผู้ใช้เขียนไว้, และยังไม่ได้ระบุชื่อ Timeline Asset ของ Ending 1/2 — ให้ Codex ตรวจของเดิมก่อนผูก.
5. ตัวละครพูดถึง “ความมั่นใจ” และ “โอกาสได้งานเพิ่มขึ้น” เป็นเนื้อเรื่อง ไม่ใช่คำสั่งให้แก้ระบบ Hire/Rejection หรือ Confidence Score.

## หลักความปลอดภัย

- JSON เป็นข้อมูลส่งให้ Codex แปลงเป็น ScriptableObjects ตามระบบที่มีอยู่ ไม่ใช่ข้อมูลให้เกมโหลดจากไฟล์ JSON โดยตรง.
- ห้ามลบหรือสร้าง GUID ใหม่ให้ Production Assets เดิม.
- ภาษาอังกฤษเป็น source, ภาษาไทยอยู่ใน LocalizationTable_TH.asset.
- ต้องตรวจว่า Production Scene เรียก Event Asset จริงและไม่ซ้อนกับ HR/Confidence Dialogue.

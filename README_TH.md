# ย้าย First-Jobber จาก Claude Code ไป Codex CLI

1. สำรองหรือ Git commit Unity Project ก่อน.
2. วาง `FIRSTJOBBER_HANDOFF.md`, `DAY_CONFIDENCE_DIALOGUE_TH.md`, `CODEX_START_PROMPT.txt` และโฟลเดอร์ `reference_packages/` ไว้ที่ Root ของ Unity Project (โฟลเดอร์เดียวกับ `Assets`, `Packages`, `ProjectSettings`). ห้ามแตก reference packages ลง Assets แบบอัตโนมัติ เพราะของจริงอาจถูก Import ไปแล้ว.
3. เปิด PowerShell แล้ว `cd` เข้า Unity Project เดิม; รัน `codex`.
4. หากต้องการดึง Claude Code chats/setup ด้วย ให้พิมพ์ `/import` แล้วเลือก Claude Code (ฟีเจอร์ที่รองรับขึ้นกับ Codex CLI รุ่นและสภาพแวดล้อมของคุณ). อย่า Import ซ้ำโดยไม่ตรวจรายการเดิม.
5. คัดลอกข้อความใน `CODEX_START_PROMPT.txt` ไปส่งใน Codex CLI เพื่อให้ Codex ตรวจงานในเครื่องเทียบกับข้อกำหนดจากแชท ก่อนเขียนโค้ด.
6. ถ้ามี `AGENTS.md` อยู่แล้ว อย่าเขียนทับ; ให้ Codex สรุปและแก้ไฟล์นี้หลังอ่าน `FIRSTJOBBER_HANDOFF.md`, `CLAUDE.md` และ `NEXT_STEPS.md` แล้ว.
7. งานครั้งต่อไปใช้ `codex resume` หรือ `codex resume --last` ภายใน project เดิมเพื่อกลับไปยัง Codex sessions เท่านั้น (ไม่ใช่ Claude Code sessions).

ข้อควรระวัง: แชท ChatGPT นี้ไม่ถูก Import อัตโนมัติด้วย `/import` ซึ่งนำเข้าข้อมูลจาก Claude Code/Cursor. เอกสาร Handoff จึงเป็นส่วนสำคัญในการส่งต่อการตัดสินใจด้านเกมที่คุยไว้ที่นี่.

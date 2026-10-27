# Day-Specific Confidence Reaction — Authored Thai Content

**Source:** User-authored text in ChatGPT conversation (27 Oct 2026). **Status:** design content only. Verify whether imported into Unity project; do not duplicate existing asset entries. English source translations are not provided here, so use production locale workflow when importing. One candidate = one alternative, randomly pick **exactly one** per Day + Confidence branch; do not play them all sequentially.

- Trigger: Press Apply on a confirmed Company in Company Selection; after Resume Confirmed has snapshotted confidence, before HR Intro.
- High: Confidence > 50%; Low: Confidence <= 50%; 50% is Low.
- Use Player speaker. Multiple pages inside a single candidate remain supported but the lines listed below are independent candidates.

## Day 1 — High (3)

- Candidate 0: โอ้โห! ดูโปรไฟล์ของฉันสิ! แบบนี้บริษัทเกมจะพลาดคนเก่งอย่างฉันไปได้ยังไงกันล่ะ?!
- Candidate 1: เอาล่ะ บริษัทนี้แหละ! งานแรกของฉันต้องดีแน่นอน!
- Candidate 2: ส่งเลยแล้วกันนนน!!

## Day 1 — Low (5)

- Candidate 0: เอ่อ... จะว่าไป โปรไฟล์ของเราก็ยังดูไม่ได้โดดเด่นขนาดนั้นแฮะ...
- Candidate 1: บริษัทนี้จะสนใจเราจริง ๆ เหรอนะ? หรือเขาจะเปิด Resume มาแล้วก็ปิดทิ้งเลย...
- Candidate 2: แต่ถ้าไม่ลองก็ไม่รู้!
- Candidate 3: ถึงจะยังไม่มั่นใจเต็มร้อย แต่ฉันก็อยากทำงานเกมจริง ๆ นี่นา!
- Candidate 4: ลุยเลย! อย่างน้อยก็ต้องให้โอกาสตัวเองก่อน!

## Day 2 — High (1)

- Candidate 0: เอาล่ะ! บริษัทที่แล้วอาจแค่ไม่ใช่ที่ของเราก็ได้! ฝีมือเราก็ไม่ได้แย่สักหน่อย บริษัทนี้ต้องมีอะไรดี ๆ บ้างแหละ!

## Day 2 — Low (2)

- Candidate 0: อ่า... ต้องส่งอีกแล้วสินะ หวังว่าคราวนี้จะไม่โดนปฏิเสธเร็วเกินไปนะ...
- Candidate 1: เอาเถอะ ลองดูก็ได้

## Day 3 — High (2)

- Candidate 0: ผลงานเราก็ไม่ได้แย่ เราก็มีความสามารถ แล้วมันยังขาดอะไรอีกล่ะ?
- Candidate 1: ช่างเถอะ ส่งไปก่อนละกัน เดี๋ยวค่อยรู้ว่าคราวนี้เขาจะหาเหตุผลอะไรมาไม่รับเรา

## Day 3 — Low (1)

- Candidate 0: เรซูเม่เดิม ผลงานเดิม คำถามเดิม... เหมือนทำทุกอย่างซ้ำไปซ้ำมาเลยแฮะ ต่างกันแค่ชื่อบริษัทกับคำปฏิเสธที่กำลังจะได้รับ...

## Day 4 — High (2)

- Candidate 0: เออ ฝีมือเราก็ดี เราก็รู้ตัวดี... แล้วไงต่อล่ะ? สุดท้ายก็ต้องมานั่งสมัครงานเหมือนเดิมอยู่ดีนี่นา ฮะ ๆ...
- Candidate 1: เอาเถอะ ส่ง ๆ ไป จะได้จบอีกวัน

## Day 4 — Low (2)

- Candidate 0: ...ไม่อยากเปิดดูเรซูเม่แล้วด้วยซ้ำ
- Candidate 1: แต่ถ้าไม่ส่ง ก็ไม่มีงาน ไม่มีเงิน แล้วก็ต้องนั่งกังวลเรื่องเดิม ๆ ต่อไป... เหนื่อยจังวะ

## Day 5 — High (3)

- Candidate 0: เราก็ทำได้ดีมาตลอดนี่นา... ตั้งใจเรียน ทำพอร์ต ฝึกฝนตัวเองมาตั้งกี่ปี
- Candidate 1: แล้วทำไมแค่จะหางานทำสักที่ มันถึงยากขนาดนี้กันนะ?
- Candidate 2: ...เออ ช่างมัน ส่งไปเถอะ จะได้รู้กันสักทีว่าคราวนี้จะโดนปฏิเสธด้วยเหตุผลอะไร

## Day 5 — Low (3)

- Candidate 0: ต้องทำอีกแล้วเหรอ... เลือกบริษัท แนบพอร์ต เขียนเรซูเม่ แล้วก็นั่งรอ...
- Candidate 1: ไม่รู้ด้วยซ้ำว่าถ้าส่งไปแล้วจะมีใครเปิดดูไหม
- Candidate 2: ...แต่ถ้าไม่ส่ง พรุ่งนี้ก็คงต้องกลับมานั่งทำแบบนี้อยู่ดี

## Import precautions

- Stable per-candidate IDs and per-page localization keys, preserving authored Thai text and natural English source translation.
- If production asset(s) already exist, **update in place** only after comparing IDs; keep GUIDs and .meta files.
- Respect dynamic day progression; Day 5 doesn't automatically mean the game loop lasts five applications.
- Ensure transition waits for selected candidate completion and can't be queued twice by rapid Apply presses.

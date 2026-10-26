# Production Content Mapping

ข้อมูลสำหรับตรวจสอบ IDs ก่อน Import (คำถามยังไม่ได้ผูกให้ HR คนใด)

| Category | Choice ID | ชื่อที่แสดง (ไทย) | Reaction Pages | Linked Question ID | Answers |
|---|---|---|---:|---|---:|
| faculty | `faculty_digital_art` | คณะดิจิทัลอาร์ต (Digital Art) | 1 | `q_faculty_digital_art` | 3 |
| faculty | `faculty_fine_arts` | คณะศิลปกรรมศาสตร์ | 2 | `q_faculty_fine_arts` | 3 |
| faculty | `faculty_digital_communications` | คณะนิเทศศาสตร์ (สื่อดิจิทัล) | 3 | `q_faculty_digital_communications` | 3 |
| faculty | `faculty_information_technology` | คณะเทคโนโลยีสารสนเทศ | 3 | `q_faculty_information_technology` | 3 |
| faculty | `faculty_accounting` | คณะบัญชี | 3 | `q_faculty_accounting` | 3 |
| university | `university_a` | มหาลัย A | 2 | `q_university_a` | 3 |
| university | `university_b` | มหาลัย B | 3 | `q_university_b` | 3 |
| university | `university_c` | มหาลัย C | 3 | `q_university_c` | 3 |
| university | `university_d` | มหาลัย D | 4 | `q_university_d` | 3 |
| university | `university_e` | มหาลัย E | 3 | `q_university_e` | 3 |
| motto | `motto_do_everything_you_want` | ทำทุกอย่างที่อยากทำ | 3 | `q_motto_do_everything_you_want` | 2 |
| motto | `motto_dreams_never_expire` | ความฝันไม่มีวันหมดอายุ | 3 | `q_motto_dreams_never_expire` | 2 |
| motto | `motto_every_day_learning` | ทุกวันคือโอกาสในการเรียนรู้ | 3 | `q_motto_every_day_learning` | 3 |
| motto | `motto_adapt_to_every_problem` | พร้อมปรับตัวกับทุกปัญหา | 3 | `q_motto_adapt_to_every_problem` | 3 |
| motto | `motto_flexibility_key` | ความยืดหยุ่น คือกุญแจสู่ความสำเร็จ | 3 | `q_motto_flexibility_key` | 2 |

## ตรวจข้อมูลก่อนเชื่อมเข้าระบบ

- Confidence (%) ยังไม่ได้รับค่า authored จริง
- HR Character เจ้าของ Question Bank ยังไม่ได้รับการระบุ
- HR Reaction/Emotion หลังเลือกคำตอบยังไม่ได้รับบท authored
- ตรวจบทตอบสั้น `ครับ/ค่ะ` กับ Gender Dialogue Variant
- ข้อความอังกฤษเป็นฉบับแปลที่รอตรวจสำนวน

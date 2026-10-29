# Card Battle — trận chiến dùng thẻ kéo thả vào mục tiêu

Card Battle là hệ thống chiến đấu trong đó nhân vật là những thẻ cỡ lớn còn kỹ năng của họ là những thẻ nhỏ hơn: chọn một nhân vật, rồi kéo một thẻ hành động của nhân vật đó vào mục tiêu. Đây là hệ thống chiến đấu của Cthangover, và RPG Plus cũng có thể dùng nó thay cho FF Battle. Còn bản thân trận chiến — khởi đầu, nền, màn hình thắng thua, kinh nghiệm và chiến lợi phẩm — do Turn Battle Base đảm nhiệm.

## Màn hình chiến đấu

Chiến binh RPG Plus đấu với sói, sau khi nhấp vào anh hùng và rê chuột lên Power strike:

![Màn hình chiến đấu thẻ bài: bảng Card battle (Round 1, Enemies alive 1) ở trên trái, nút End turn và các nút tốc độ ở trên phải, thẻ của con sói bên phải (HP 31/31, DEF 2, ATK 7, AP 1), thẻ của anh hùng dưới trái (HP 42/42, DEF 4, ATK 12, AP 1), nhật ký chiến đấu ở giữa và bảng Actions với ba thẻ — Basic attack, Defence, Power strike — kèm gợi ý "Vung trọn đòn: một lần rưỡi sát thương và thêm một chút từ sức mạnh".](assets/card-hover.jpg)

- **Thẻ nhân vật** hiển thị máu (HP), lớp khiên (DEF, hấp thụ sát thương trước), sát thương (ATK) và điểm hành động (AP).
- **Bảng Actions** — các thẻ hành động của nhân vật đang chọn, kèm mục tiêu (địch hoặc đồng đội), giá AP và các giá trị. Thẻ nào nhân vật không trả nổi sẽ bị mờ đi.
- **Dòng gợi ý** dưới các thẻ cho biết thẻ đang rê chuột sẽ làm gì: sát thương của đòn đánh thường, lớp khiên của phép phòng thủ, mô tả của kỹ năng viết bằng công thức.
- **End turn** kết thúc sớm lượt của đội; khi hết AP (còn 0), lượt tự kết thúc.

## Diễn biến một lượt

1. **Nhấp vào thẻ của một thành viên đội** để xem các hành động của họ.
2. **Kéo thẻ hành động vào mục tiêu** — thẻ địch để tấn công, thẻ đồng đội để phòng thủ. Mục tiêu sẽ cho biết hành động đó có áp dụng được lên nó hay không.
3. Khi đội hết điểm hành động, quân địch sẽ hành động và một vòng mới bắt đầu.

![Nhật ký chiến đấu và bảng hành động sau một vòng: Shield of Wolf absorbed 2; Power strike: Hero → Wolf · −19 HP; The party is out of action points; Enemy turn; Defence: Wolf · +4 DEF; Player turn · round 2.](assets/card-turn.jpg)

Power strike của anh hùng gây 21 sát thương: phần đầu là 2 điểm lớp khiên của sói hấp thụ, máu tụt 19, từ 31 xuống 12. Sau đó con sói nâng lớp khiên lên 4.

## Các hành động

Card Battle xử lý những loại hành động cơ bản giống hệt FF Battle:

- `Attack=1.5` — sát thương của đòn đánh nhân với con số; lớp khiên của mục tiêu hấp thụ trước;
- `Defence=4` — đặt lớp khiên lên đồng đội;
- `Turn=2` — khiến mục tiêu choáng đúng số lượt đó.

Kỹ năng viết bằng công thức (giá trị `Formula=`, chẳng hạn Power strike của Classic RPG Rules) cũng hoạt động ở đây thông qua Battle Formulas.

## Chọn Card Battle

Trong bộ sưu tập của game, đặt `"battle.core": "card_battle"`, hoặc đổi hệ thống từ kịch bản trước trận đấu:

```scenario
action battle.set_core core=card_battle
```

## Khi có sự cố

1. **Kéo thẻ mà chẳng có gì xảy ra.** Chưa chọn nhân vật, nhân vật không đủ điểm hành động, hoặc mục tiêu sai phe (đòn đánh dành cho địch, phép phòng thủ dành cho đồng đội).
2. **Trận chiến mở bằng FF Battle.** Thiết lập `battle.core` của game đang chỉ `ff_battle`.

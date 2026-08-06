# DICE DICE DICE! — Game Design Specification

## 1. Tổng quan

**Tên game:** DICE DICE DICE!  
**Thể loại:** Tower Defense màn hình ngang + Merge Item + Roguelite  
**Góc nhìn:** Side-view, quái di chuyển từ phải sang trái  
**Trải nghiệm chính:** Xây dựng kinh tế bằng Dice, dùng vàng mua item chiến đấu, merge để nâng cấp và tạo build đủ mạnh để vượt qua các đợt quái.

---

## 2. High Concept

Người chơi bảo vệ căn cứ nằm bên trái màn hình trước các đợt quái xuất hiện từ bên phải.

Người chơi có một bảng gồm **8 ô item**, chia thành:

- 2 cột.
- Mỗi cột 4 ô (4 hàng).
- Bảng nằm ở phía trái màn hình, phía sau tường thành.
- Ngay bên phải bảng item là **tường thành** — công trình mà quái tấn công.

Mỗi item chiếm một ô trên bảng.

Dice là item kinh tế chủ đạo:

- Dice được mua trong Shop bằng vàng.
- Dice không trực tiếp tấn công.
- Sau mỗi khoảng thời gian, Dice tự roll.
- Dice chỉ roll khi wave đang diễn ra, không roll trong giai đoạn mua sắm.
- Kết quả roll quyết định lượng vàng được tạo ra.
- Dice hoạt động tương tự vai trò của Sunflower trong Plants vs. Zombies.
- Đặt nhiều Dice giúp phát triển kinh tế nhanh nhưng làm giảm số ô dành cho item chiến đấu.

Các item còn lại có thể:

- Tấn công quái bằng projectile.
- Tạo phép thuật.
- Gây hiệu ứng khống chế.
- Tăng chỉ số cho đội hình.
- Bảo vệ hoặc hồi phục căn cứ.

Hai item giống nhau, cùng phẩm cấp, có thể merge thành một item phẩm cấp cao hơn.

---

## 3. Fantasy người chơi

Người chơi có cảm giác đang xây dựng một “cỗ máy thủ thành” nhỏ gọn gồm Dice, vũ khí, phép thuật và các item hỗ trợ.

Fantasy chính:

- Bắt đầu với một hệ thống yếu và ít tài nguyên.
- Đầu tư Dice để tạo ra nhiều vàng hơn.
- Dùng vàng mua item mới.
- Merge item để giải phóng ô và tăng sức mạnh.
- Tạo ra các combo item mạnh.
- Chuyển dần từ đội hình kinh tế sang đội hình chiến đấu.
- Sống sót trước những wave quái ngày càng đông và mạnh.

---

## 4. Trụ cột thiết kế

### 4.1. Dice là nền kinh tế

Dice phải là nguồn vàng quan trọng nhất trong một run.

Người chơi phải cân nhắc:

- Mua Dice để mạnh hơn về lâu dài.
- Hay mua vũ khí để sống sót ngay lập tức.

Dice không kích hoạt các item bên cạnh và không phải là trung tâm của hệ thống liên kết vị trí.

Vai trò chính của Dice là:

- Chiếm ô.
- Roll theo thời gian.
- Tạo vàng.
- Có thể merge để tăng hiệu suất kinh tế trên mỗi ô.

### 4.2. Chỉ có 8 ô

Giới hạn 8 ô tạo ra áp lực chiến thuật rõ ràng.

Người chơi không thể giữ tất cả item mình muốn.

Mỗi ô được dùng cho Dice đồng nghĩa với việc mất một ô chiến đấu.

Người chơi phải liên tục quyết định:

- Giữ Dice.
- Merge Dice.
- Bán Dice.
- Thay Dice bằng item chiến đấu.
- Giữ item yếu để chờ merge.
- Bán item để tạo không gian cho build khác.

### 4.3. Merge tạo sức mạnh và giải phóng không gian

Merge không chỉ tăng chỉ số mà còn giúp giải phóng ô.

Đây là giá trị quan trọng nhất của merge trong bảng chỉ có 8 ô.

### 4.4. Roguelike tạo build khác nhau

Mỗi lần lên cấp, người chơi chọn một trong ba nâng cấp.

Các lựa chọn phải có khả năng:

- Tăng sức mạnh một nhóm item.
- Thay đổi cách vận hành của Dice.
- Tạo combo giữa các hiệu ứng.
- Khuyến khích người chơi theo đuổi một hướng build cụ thể.
- Làm cho mỗi run có cảm giác khác nhau.

---

## 5. Vòng lặp gameplay chính

### 5.1. Core Loop

Game chia thành hai giai đoạn xen kẽ nhau: **giai đoạn mua sắm** và **giai đoạn wave**. Shop chỉ mở trong giai đoạn mua sắm — không thể mua, reroll hay bán item khi wave đang diễn ra.

**Giai đoạn mua sắm (giữa các wave):**

1. Shop mở, hiển thị các item có thể mua.
2. Người chơi dùng vàng để mua item; item được đưa vào một ô trống trên bảng.
3. Người chơi sắp xếp, bán hoặc merge item (hai item giống nhau, cùng phẩm cấp).
4. Khi sẵn sàng, người chơi bấm bắt đầu wave.

**Giai đoạn wave:**

5. Wave quái bắt đầu, Shop đóng.
6. Dice tự roll và tạo vàng (Dice chỉ roll trong giai đoạn này).
7. Item chiến đấu tự động tấn công quái.
8. Người chơi vẫn có thể sắp xếp và merge item trên bảng.
9. Quái bị tiêu diệt tạo kinh nghiệm.
10. Khi đủ kinh nghiệm, người chơi lên cấp và chọn một trong ba nâng cấp roguelike.
11. Wave kết thúc, quay lại giai đoạn mua sắm; wave tiếp theo có độ khó cao hơn.

### 5.2. Vòng lặp quyết định

Trong suốt run, người chơi thường xuyên phải trả lời các câu hỏi:

- Có nên mua thêm Dice không?
- Đội hình hiện tại có đủ sức qua wave tiếp theo không?
- Có nên dùng vàng reroll Shop?
- Có nên giữ một item yếu để chờ bản sao?
- Có nên bán Dice để lấy thêm ô chiến đấu?
- Có nên đổi hướng build sau khi nhận được nâng cấp roguelike mạnh?
- Có nên tiết kiệm vàng hay mua ngay để tăng sức mạnh tức thời?

---

## 6. Bố cục màn hình

Toàn bộ gameplay phải gói gọn trong một màn hình **1920x1080**, không cuộn. Khi cửa sổ có kích thước khác, toàn bộ khung game được scale đồng bộ để vừa màn hình.

Thứ tự bố cục từ trái sang phải: **bảng item (2 cột × 4 hàng) → tường thành → chiến trường**.

### 6.1. Tường thành (căn cứ)

- Nằm ngay bên phải bảng item, kéo dài theo chiều dọc màn hình.
- Có thanh máu.
- Quái đến được tường thành sẽ gây sát thương.
- Run kết thúc khi máu tường thành về 0.

### 6.2. Bảng item

Bảng gồm 8 ô, xếp thành 2 cột × 4 hàng:

```text
[ Slot 1 ] [ Slot 2 ]
[ Slot 3 ] [ Slot 4 ]
[ Slot 5 ] [ Slot 6 ]
[ Slot 7 ] [ Slot 8 ]
```

Đặc điểm:

- Nằm tại phía trái màn hình, phía sau (bên trái) tường thành.
- Item được thể hiện rõ ràng và dễ phân biệt.
- Phẩm cấp của item phải được thể hiện bằng màu, viền hoặc hiệu ứng.
- Dice cần hiển thị trạng thái chuẩn bị roll và kết quả roll.
- Item chiến đấu cần có hoạt ảnh tấn công rõ ràng.

### 6.3. Chiến trường

- Trải dài từ tường thành sang bên phải màn hình.
- Quái xuất hiện từ phía phải.
- Quái di chuyển sang trái và tấn công tường thành.
- Các projectile và phép thuật được tạo từ vị trí ô item, bay qua tường thành vào chiến trường.

### 6.4. Shop

Shop hiển thị một số item có thể mua bằng vàng.

**Shop chỉ mở trong giai đoạn mua sắm giữa các wave.** Khi wave đang diễn ra, Shop đóng và không thể mua, reroll hay bán item.

Shop cần hỗ trợ các hành động:

- Mua item.
- Reroll danh sách item.
- Khóa Shop để giữ item sang lần làm mới tiếp theo.
- Bán item đang có.
- Hiển thị giá rõ ràng.
- Nút bắt đầu wave tiếp theo khi người chơi đã mua sắm xong.

---

## 7. Tài nguyên

### 7.1. Vàng

Vàng dùng để:

- Mua item.
- Reroll Shop.
- Thực hiện một số lựa chọn đặc biệt trong run.
- Có thể dùng để nâng Shop nếu hệ thống này được bổ sung sau.

Nguồn vàng chính:

- Dice roll.

Nguồn vàng phụ:

- Thưởng cuối wave.
- Boss.
- Một số nâng cấp roguelike.
- Một số item hoặc hiệu ứng đặc biệt.

Không nên để quái thường tạo quá nhiều vàng vì sẽ làm giảm vai trò của Dice.

### 7.2. Kinh nghiệm

Kinh nghiệm dùng để tăng level trong run.

Nguồn kinh nghiệm chính:

- Tiêu diệt quái.
- Tiêu diệt Elite.
- Tiêu diệt Boss.

Khi đủ kinh nghiệm:

- Game tạm dừng.
- Hiển thị ba lựa chọn roguelike.
- Người chơi chọn một nâng cấp.
- Gameplay tiếp tục.

### 7.3. Máu căn cứ

- Đại diện cho khả năng sống sót của người chơi.
- Quái chạm căn cứ sẽ gây sát thương.
- Một số item và nâng cấp có thể hồi máu hoặc tạo khiên.
- Run thất bại khi máu căn cứ về 0.

---

## 8. Hệ thống Dice

### 8.1. Dice cơ bản

Dice là item kinh tế phổ biến nhất.

Hành vi:

- Chiếm một ô.
- Không gây sát thương.
- Tự roll sau mỗi khoảng thời gian, **chỉ khi wave đang diễn ra** (không roll trong giai đoạn mua sắm; tiến độ roll đang tích dở được giữ nguyên sang wave sau).
- Kết quả từ 1 đến 6.
- Tạo vàng dựa trên kết quả roll.
- Sau khi roll xong, bắt đầu đếm thời gian cho lần roll tiếp theo.

Ví dụ:

- Roll 1: nhận 1 vàng.
- Roll 2: nhận 2 vàng.
- Roll 3: nhận 3 vàng.
- Roll 4: nhận 4 vàng.
- Roll 5: nhận 5 vàng.
- Roll 6: nhận 6 vàng.

### 8.2. Cảm giác roll

Mỗi lần roll phải tạo cảm giác vui và dễ nhận biết:

- Dice rung nhẹ trước khi roll.
- Dice nảy lên.
- Dice xoay nhanh.
- Dice rơi xuống với lực rõ ràng.
- Mặt số cuối cùng được phóng to hoặc nhấn mạnh.
- Vàng bay từ Dice về khu vực hiển thị tài nguyên.
- Roll ra số cao cần tạo cảm giác thỏa mãn hơn số thấp.

### 8.3. Merge Dice

Hai Dice giống nhau và cùng phẩm cấp có thể merge.

Ví dụ:

```text
2 Common Dice → 1 Rare Dice
2 Rare Dice → 1 Epic Dice
2 Epic Dice → 1 Legendary Dice
```

Dice phẩm cấp cao hơn có thể:

- Roll nhanh hơn.
- Tạo nhiều vàng hơn.
- Có giá trị roll tối thiểu cao hơn.
- Có cơ hội roll thêm.
- Có hiệu ứng kinh tế đặc biệt.

Merge Dice phải giúp:

- Tăng hiệu suất kinh tế trên mỗi ô.
- Giải phóng một ô cho item khác.
- Tạo cảm giác tiến triển rõ ràng.

### 8.4. Các loại Dice mở rộng

Phiên bản đầu tiên chỉ cần một Dice cơ bản.

Các Dice sau có thể được bổ sung khi core loop đã ổn định.

#### Lucky Dice

- Roll từ 1 đến 6.
- Roll ra 6 có cơ hội roll thêm một lần.

#### Loaded Dice

- Không thể roll ra số quá thấp.
- Giá mua cao hơn Dice thường.

#### Golden Dice

- Roll chậm.
- Mỗi lần roll tạo lượng vàng lớn.

#### Chaos Dice

- Có thể roll ra giá trị rất thấp hoặc rất cao.
- Phù hợp với build rủi ro.

#### Blood Dice

- Có khả năng tạo vàng khi quái bị tiêu diệt.
- Vẫn chiếm một ô kinh tế.

---

## 9. Hệ thống item chiến đấu

### 9.1. Nguyên tắc chung

Item chiến đấu:

- Chiếm một ô trên bảng.
- Tự động tìm mục tiêu.
- Tự động tấn công hoặc cast phép.
- Có cooldown hoặc tốc độ đánh riêng.
- Có thể merge.
- Phẩm cấp cao hơn phải mạnh hơn và có thể mở thêm hiệu ứng.

### 9.2. Weapon Item

#### Bow

Vai trò:

- Tấn công tầm xa.
- Tốc độ cao.
- Sát thương mỗi đòn thấp.
- Hiệu quả với quái yếu hoặc quái chạy nhanh.

Nâng cấp phẩm cấp có thể:

- Tăng tốc độ bắn.
- Bắn nhiều mũi tên.
- Xuyên mục tiêu.
- Có tỉ lệ chí mạng.

#### Sword

Vai trò:

- Tạo kiếm khí hoặc thanh kiếm bay.
- Sát thương trung bình đến cao.
- Ưu tiên mục tiêu gần căn cứ.

Nâng cấp phẩm cấp có thể:

- Tăng kích thước đường kiếm.
- Chém nhiều mục tiêu.
- Tạo thêm nhát chém.
- Gây chảy máu.

#### Crossbow

Vai trò:

- Tốc độ chậm.
- Sát thương lớn.
- Có khả năng chí mạng cao.
- Hiệu quả với Elite và Boss.

Nâng cấp phẩm cấp có thể:

- Tăng sát thương chí mạng.
- Xuyên giáp.
- Bắn xuyên nhiều mục tiêu.
- Đòn chí mạng gây nổ nhỏ.

#### Cannon

Vai trò:

- Bắn chậm.
- Gây sát thương diện rộng.
- Hiệu quả với nhóm quái đông.

Nâng cấp phẩm cấp có thể:

- Tăng bán kính nổ.
- Đẩy lùi quái.
- Để lại vùng cháy.
- Bắn thêm đạn phụ.

### 9.3. Magic Item

#### Fire Book

Vai trò:

- Gọi cầu lửa hoặc thiên thạch.
- Gây sát thương diện rộng.
- Có thể gây hiệu ứng đốt cháy.

#### Frost Stone

Vai trò:

- Gây sát thương thấp.
- Làm chậm quái.
- Có thể đóng băng quái trong thời gian ngắn.

#### Lightning Orb

Vai trò:

- Tạo sét lan giữa nhiều mục tiêu.
- Hiệu quả với nhóm quái đứng gần nhau.

#### Poison Flask

Vai trò:

- Tạo vùng độc.
- Gây sát thương theo thời gian.
- Hiệu quả với quái nhiều máu.

### 9.4. Support Item

#### Anvil

Vai trò:

- Tăng sát thương cho các item chiến đấu.
- Bản thân không trực tiếp tấn công.

Có thể áp dụng hiệu ứng cho:

- Toàn bộ đội hình.
- Các item cùng hàng.
- Một số item được chỉ định.

Cách áp dụng cụ thể cần được cân bằng sau khi thử nghiệm.

#### Hourglass

Vai trò:

- Tăng tốc độ hoạt động của item.
- Có thể giảm thời gian roll của Dice hoặc cooldown của item chiến đấu.

Hourglass cần được cân bằng cẩn thận để không trở thành lựa chọn bắt buộc.

#### Crystal

Vai trò:

- Tăng sát thương phép.
- Hỗ trợ build Magic.

#### Sharpening Stone

Vai trò:

- Tăng sát thương vật lý.
- Hỗ trợ build Weapon.

### 9.5. Defense Item

#### Shield

Vai trò:

- Tạo khiên cho căn cứ.
- Hấp thụ một phần sát thương.

#### Healing Totem

Vai trò:

- Hồi một lượng máu nhỏ theo thời gian hoặc sau mỗi wave.

#### Barrier Stone

Vai trò:

- Làm chậm quái khi chúng đến gần căn cứ.
- Tạo thêm thời gian cho đội hình gây sát thương.

---

## 10. Hệ thống phẩm cấp

Các phẩm cấp đề xuất:

1. Common.
2. Rare.
3. Epic.
4. Legendary.

Mỗi lần merge:

- Tăng một bậc phẩm cấp.
- Tăng chỉ số chính.
- Có thể thay đổi ngoại hình.
- Có thể mở thêm hiệu ứng mới ở các mốc quan trọng.

Định hướng:

- Common: hành vi cơ bản.
- Rare: mạnh hơn rõ ràng.
- Epic: có thêm một passive.
- Legendary: có hiệu ứng đặc trưng thay đổi cách item hoạt động.

Ví dụ với Bow:

- Common: bắn một mũi tên.
- Rare: tăng tốc độ bắn.
- Epic: mũi tên có thể xuyên một mục tiêu.
- Legendary: thỉnh thoảng tạo mưa tên.

---

## 11. Quy tắc merge

Điều kiện merge:

- Hai item phải giống cùng loại.
- Hai item phải cùng phẩm cấp.

Kết quả:

- Hai item được thay bằng một item phẩm cấp cao hơn.
- Một ô được giải phóng.
- Item mới giữ nguyên loại.

Merge không nên tạo item ngẫu nhiên vì người chơi cần cảm giác kiểm soát build.

### 11.1. Trạng thái bảng đầy

Khi bảng đủ 8 ô:

- Người chơi không thể mua item mới nếu không có chỗ trống.
- Người chơi có thể merge để giải phóng ô.
- Người chơi có thể bán item.
- Shop phải thông báo rõ khi không thể mua vì đầy bảng.

Có thể bổ sung một ô chờ bên ngoài bảng trong tương lai, nhưng không bắt buộc cho MVP.

---

## 12. Hệ thống Shop

Shop chỉ hoạt động trong **giai đoạn mua sắm** giữa các wave. Người chơi mua sắm xong thì bấm bắt đầu wave; trong wave, Shop đóng hoàn toàn.

### 12.1. Danh sách Shop

Shop hiển thị 3 item tại một thời điểm.

Mỗi item hiển thị:

- Icon.
- Tên.
- Phẩm cấp.
- Giá.
- Nhóm item.
- Mô tả ngắn.

### 12.2. Mua item

Khi mua:

- Trừ vàng.
- Item được đặt vào một ô trống.
- Nếu bảng đầy, không thể mua.
- Người chơi có thể tự chọn ô đặt item hoặc item được đặt vào ô trống đầu tiên.

Ưu tiên trải nghiệm dễ hiểu và nhanh.

### 12.3. Reroll Shop

- Người chơi trả vàng để thay toàn bộ item đang bán.
- Giá reroll có thể tăng nhẹ nếu dùng nhiều lần trong cùng một wave.
- Một số nâng cấp roguelike có thể giảm giá hoặc miễn phí reroll.

### 12.4. Khóa Shop

- Giữ lại danh sách Shop qua lần làm mới tiếp theo.
- Giúp người chơi giữ một item muốn mua nhưng chưa đủ vàng.

### 12.5. Tần suất xuất hiện Dice

Dice phải đủ phổ biến để người chơi có thể xây dựng kinh tế.

Đề xuất:

- Shop đầu tiên luôn có ít nhất một Dice.
- Người chơi có thể bắt đầu với một Dice miễn phí.
- Sau giai đoạn đầu, Dice xuất hiện ngẫu nhiên như các item khác.

Dice không nên quá hiếm vì một run có thể thất bại chỉ do thiếu nguồn kinh tế.

---

## 13. Hệ thống bán item

Người chơi có thể bán item trên bảng, **chỉ trong giai đoạn mua sắm** (không bán được khi wave đang diễn ra).

Khi bán:

- Item bị xóa khỏi bảng.
- Người chơi nhận lại một phần giá trị vàng.
- Ô được giải phóng.

Mục đích:

- Sửa sai trong quá trình xây build.
- Loại bỏ item không còn phù hợp.
- Chuyển từ kinh tế sang chiến đấu ở late game.
- Tạo không gian cho item mạnh hơn.

Không nên hoàn lại toàn bộ vàng vì sẽ làm quyết định mua item không còn ý nghĩa.

---

## 14. Tiến trình một run

### 14.1. Đầu run

Người chơi bắt đầu với:

- Một Dice Common.
- Một item chiến đấu cơ bản.
- Một lượng vàng nhỏ.
- Căn cứ đầy máu.

Mục tiêu đầu run:

- Mua thêm Dice hoặc item chiến đấu.
- Xây nền kinh tế.
- Không để số lượng quái vượt khỏi khả năng kiểm soát.

### 14.2. Giữa run

Người chơi:

- Bắt đầu có nhiều item.
- Merge item để giải phóng ô.
- Chọn hướng build dựa trên Shop và nâng cấp roguelike.
- Đối mặt với Elite và quái có cơ chế đặc biệt.

### 14.3. Cuối run

Người chơi:

- Hoàn thiện build.
- Cân nhắc bán Dice.
- Chuyển nhiều ô sang item chiến đấu.
- Chuẩn bị đối đầu Boss cuối.
- Tận dụng combo nâng cấp đã chọn.

---

## 15. Hệ thống wave

Một run MVP có thể gồm 10 wave.

Cấu trúc đề xuất:

- Wave 1–2: Giới thiệu quái cơ bản.
- Wave 3–4: Tăng số lượng và tốc độ.
- Wave 5: Mini-boss.
- Wave 6–7: Kết hợp nhiều loại quái.
- Wave 8–9: Áp lực cao, xuất hiện Elite.
- Wave 10: Boss cuối.

Mỗi wave phải có mục đích rõ ràng:

- Kiểm tra sát thương đơn mục tiêu.
- Kiểm tra khả năng dọn đám đông.
- Kiểm tra khả năng khống chế.
- Kiểm tra độ ổn định của nền kinh tế.
- Kiểm tra khả năng hoàn thiện build.

---

## 16. Các loại quái

### 16.1. Basic Enemy

- Máu trung bình.
- Tốc độ trung bình.
- Không có hiệu ứng đặc biệt.

### 16.2. Runner

- Máu thấp.
- Di chuyển nhanh.
- Kiểm tra tốc độ phản ứng và tốc độ bắn.

### 16.3. Tank

- Máu cao.
- Di chuyển chậm.
- Kiểm tra sát thương đơn mục tiêu.

### 16.4. Swarm Enemy

- Máu thấp.
- Xuất hiện theo nhóm đông.
- Kiểm tra khả năng gây sát thương diện rộng.

### 16.5. Armored Enemy

- Giảm sát thương vật lý.
- Khuyến khích sử dụng phép thuật.

### 16.6. Magic Resistant Enemy

- Giảm sát thương phép.
- Khuyến khích sử dụng vũ khí vật lý.

### 16.7. Healer Enemy

- Hồi máu cho quái gần đó.
- Nên được ưu tiên tiêu diệt.

### 16.8. Disruptor Enemy

- Tạo hiệu ứng làm chậm hoạt động của một số item.
- Chỉ nên xuất hiện ở giữa hoặc cuối run.

---

## 17. Boss

Boss cần có cơ chế dễ hiểu, liên quan đến chủ đề Dice.

### 17.1. Snake Eyes

- Có hai giai đoạn.
- Tạo hiệu ứng xấu khi người chơi nhận nhiều roll thấp.
- Có thể triệu hồi hai quái phụ tượng trưng cho hai mặt số 1.

### 17.2. Loaded Golem

- Có nhiều lớp giáp.
- Mỗi lớp giáp cần lượng sát thương lớn để phá.
- Khuyến khích build sát thương đơn mục tiêu.

### 17.3. The Dealer

- Tạo áp lực kinh tế.
- Có thể tăng tạm thời giá Shop hoặc khóa một Dice trong thời gian ngắn.
- Không nên phá hủy item vĩnh viễn.

Boss cuối trong MVP chỉ cần một boss có hành vi rõ ràng và dễ cân bằng.

---

## 18. Hệ thống lên cấp và Roguelike Choice

Khi người chơi lên cấp:

- Gameplay tạm dừng.
- Xuất hiện ba lựa chọn.
- Người chơi chọn một.
- Nâng cấp có hiệu lực trong phần còn lại của run.

Các lựa chọn nên được chia thành nhóm.

### 18.1. Nâng cấp Dice

- Dice roll nhanh hơn.
- Dice có giá trị roll tối thiểu cao hơn.
- Roll ra 6 nhận thêm vàng.
- Dice có cơ hội roll hai lần.
- Sau một số lần roll thấp, lần tiếp theo chắc chắn là roll cao.
- Dice phẩm cấp cao tạo thêm vàng.
- Mỗi lần merge Dice nhận một lượng vàng.
- Dice đầu tiên mua trong mỗi wave được giảm giá.

### 18.2. Nâng cấp Weapon

- Tăng sát thương vật lý.
- Tăng tốc độ đánh.
- Tăng tỉ lệ chí mạng.
- Projectile xuyên thêm mục tiêu.
- Đòn chí mạng gây nổ.
- Quái bị tiêu diệt bởi Weapon có thể tạo projectile phụ.

### 18.3. Nâng cấp Magic

- Tăng sát thương phép.
- Giảm cooldown phép.
- Tăng phạm vi.
- Tăng thời gian hiệu ứng đốt, độc hoặc đóng băng.
- Phép có cơ hội cast hai lần.
- Kẻ địch chịu nhiều hiệu ứng nguyên tố nhận thêm sát thương.

### 18.4. Nâng cấp Support

- Tăng hiệu quả item hỗ trợ.
- Tăng sức mạnh cho item cùng hàng.
- Tăng sức mạnh cho item cùng nhóm.
- Support item chiếm ô nhưng tạo bonus mạnh hơn.

### 18.5. Nâng cấp Shop và kinh tế

- Giảm giá reroll.
- Reroll đầu tiên mỗi wave miễn phí.
- Tăng cơ hội xuất hiện item mong muốn.
- Tăng giá trị bán item.
- Nhận thưởng vàng khi kết thúc wave.
- Nhận lãi dựa trên lượng vàng đang giữ.

### 18.6. Nâng cấp căn cứ

- Tăng máu tối đa.
- Hồi máu sau mỗi wave.
- Tạo khiên đầu wave.
- Khi căn cứ mất máu, đẩy lùi quái gần đó.
- Chặn lần sát thương đầu tiên trong mỗi wave.

---

## 19. Hướng build

### 19.1. Economy Build

Đặc điểm:

- Dùng nhiều Dice.
- Ưu tiên nâng cấp tốc độ roll và vàng.
- Sức mạnh đầu game thấp.
- Có khả năng snowball mạnh ở giữa và cuối run.

Rủi ro:

- Thiếu ô chiến đấu.
- Có thể chết trước khi nền kinh tế phát huy hiệu quả.

### 19.2. Rapid Attack Build

Item chính:

- Bow.
- Dagger hoặc Weapon tốc độ cao.
- Hourglass.

Nâng cấp ưu tiên:

- Tốc độ đánh.
- Projectile phụ.
- Chí mạng.
- Hiệu ứng khi đánh nhiều lần.

### 19.3. Heavy Weapon Build

Item chính:

- Crossbow.
- Cannon.
- Sword.

Nâng cấp ưu tiên:

- Sát thương lớn.
- Xuyên giáp.
- Nổ diện rộng.
- Đòn đánh chậm nhưng mạnh.

### 19.4. Magic Build

Item chính:

- Fire Book.
- Frost Stone.
- Lightning Orb.
- Crystal.

Nâng cấp ưu tiên:

- Giảm cooldown.
- Tăng phạm vi.
- Phản ứng giữa các hiệu ứng.
- Cast nhiều lần.

### 19.5. Fortress Build

Item chính:

- Shield.
- Healing Totem.
- Frost Stone.
- Cannon.

Nâng cấp ưu tiên:

- Hồi máu.
- Khiên.
- Làm chậm.
- Đẩy lùi.
- Sát thương diện rộng ổn định.

---

## 20. Chuyển đổi từ Economy sang Combat

Đây là một quyết định quan trọng ở cuối run.

Đầu và giữa run:

- Dice tạo vàng để mở rộng đội hình.
- Người chơi chấp nhận ít ô chiến đấu hơn.

Cuối run:

- Giá trị của vàng giảm dần.
- Người chơi có thể bán Dice.
- Các ô trống được thay bằng Weapon, Magic hoặc Support.
- Build chuyển từ phát triển kinh tế sang tối đa sức mạnh chiến đấu.

Game cần khuyến khích quá trình chuyển đổi này nhưng không bắt buộc hoàn toàn.

Một số build có thể tiếp tục giữ Dice nếu nhận được nâng cấp khiến Dice vẫn có giá trị ở late game.

---

## 21. Skill đặc trưng: DICE DICE DICE!

Game có thể có một kỹ năng đặc biệt mang tên game.

Thanh kỹ năng được tích bằng:

- Tiêu diệt quái.
- Merge item.
- Roll ra 6.
- Hoàn thành wave.

Khi kích hoạt:

- Tất cả Dice roll ngay lập tức.
- Dice có thể roll nhiều lần liên tiếp.
- Người chơi nhận một lượng vàng lớn.
- Item chiến đấu được tăng tốc trong thời gian ngắn.
- Hiệu ứng chữ “DICE! DICE! DICE!” xuất hiện mạnh mẽ trên màn hình.

Kỹ năng này cần tạo khoảnh khắc cao trào và phù hợp để sử dụng trong quảng cáo gameplay.

Không bắt buộc đưa vào MVP đầu tiên nếu làm phức tạp vòng lặp chính.

---

## 22. Onboarding

Màn chơi đầu tiên cần hướng dẫn theo thứ tự:

1. Giới thiệu căn cứ và hướng di chuyển của quái.
2. Giới thiệu bảng 8 ô.
3. Cho người chơi một Dice.
4. Dice tự roll và tạo vàng.
5. Yêu cầu người chơi dùng vàng mua Bow.
6. Quái đầu tiên xuất hiện.
7. Giới thiệu Shop.
8. Cho người chơi mua một Dice hoặc Bow thứ hai.
9. Hướng dẫn merge hai item giống nhau.
10. Giới thiệu thanh kinh nghiệm.
11. Cho người chơi chọn nâng cấp roguelike đầu tiên.

Onboarding phải thể hiện rõ:

> Dice tạo vàng, còn vũ khí và phép thuật tiêu diệt quái.

---

## 23. Game Feel

### 23.1. Dice

- Roll phải có độ nảy.
- Số roll phải dễ đọc.
- Roll cao có hiệu ứng mạnh hơn.
- Vàng được tạo ra phải có chuyển động rõ ràng.
- Nhiều Dice roll gần nhau cần tạo cảm giác vui nhưng không rối mắt.

### 23.2. Tấn công

- Projectile có đường bay rõ ràng.
- Quái phản ứng khi trúng đòn.
- Đòn chí mạng có nhấn mạnh.
- Vụ nổ có lực.
- Phép thuật diện rộng phải dễ nhận biết phạm vi.

### 23.3. Merge

- Hai item được hút vào nhau.
- Có hiệu ứng lóe sáng.
- Item mới xuất hiện lớn hơn trong thời gian ngắn.
- Phẩm cấp mới được thể hiện rõ.
- Âm thanh merge phải tạo cảm giác tiến triển.

### 23.4. Quái chết

- Quái có phản ứng khác nhau tùy loại sát thương.
- Quái bị hạ phải tạo cảm giác “dọn sạch”.
- Khi tiêu diệt một nhóm lớn, game nên tạo hiệu ứng thỏa mãn.

---

## 24. Art Direction

Phong cách đề xuất:

- Stylized 3D.
- Toy-like.
- Hình khối rõ ràng.
- Màu sắc tươi sáng.
- Item dễ đọc ở kích thước nhỏ.
- Dice là hình ảnh nổi bật nhất.
- Vũ khí và sách phép mang cảm giác như mô hình đồ chơi đặt trên bàn.

Màu nhóm item:

- Dice và Economy: vàng.
- Weapon: đỏ hoặc cam.
- Magic: tím hoặc xanh dương.
- Support: xanh lá.
- Defense: xanh cyan hoặc bạc.

Phẩm cấp:

- Common: trắng hoặc xám.
- Rare: xanh dương.
- Epic: tím.
- Legendary: vàng hoặc cam sáng.

---

## 25. Âm thanh

Âm thanh cần hỗ trợ nhận biết hành động:

- Dice chuẩn bị roll.
- Dice va xuống bàn.
- Âm thanh riêng cho từng mặt số hoặc nhóm số thấp/cao.
- Vàng được cộng.
- Mua item.
- Không đủ vàng.
- Merge.
- Item lên phẩm cấp.
- Projectile.
- Phép thuật.
- Quái trúng đòn.
- Quái chết.
- Căn cứ mất máu.
- Lên level.
- Chọn roguelike upgrade.
- Boss xuất hiện.

---

## 26. Meta Progression đề xuất

Meta progression không phải trọng tâm của MVP.

Có thể bổ sung sau:

- Mở khóa item mới.
- Mở khóa Dice mới.
- Mở khóa nhân vật hoặc căn cứ.
- Nâng cấp nhỏ trước run.
- Bộ sưu tập item.
- Nhiệm vụ hằng ngày.
- Thành tựu.
- Chế độ Endless.
- Các map có luật chơi khác nhau.

Meta progression không nên làm mất đi giá trị của kỹ năng xây build trong từng run.

---

## 27. Nội dung MVP

MVP nên tập trung kiểm chứng ba yếu tố:

1. Dice tạo kinh tế có vui không?
2. Giới hạn 8 ô có tạo quyết định thú vị không?
3. Shop + Merge + Roguelike có tạo build đa dạng không?

### 27.1. Nội dung MVP đề xuất

- 1 map.
- 10 wave.
- 1 mini-boss.
- 1 boss cuối.
- 8 ô item.
- 4 phẩm cấp.
- 1 loại Dice.
- 4 Weapon.
- 3 Magic.
- 2 Support.
- 1 Defense.
- Khoảng 25 nâng cấp roguelike.
- 6 loại quái thường.
- 1 loại Elite.
- 1 boss.

### 27.2. Danh sách item MVP

1. Dice.
2. Bow.
3. Sword.
4. Crossbow.
5. Cannon.
6. Fire Book.
7. Frost Stone.
8. Lightning Orb.
9. Anvil.
10. Hourglass.
11. Shield.

---

## 28. Điều kiện thắng và thua

### Thắng

- Tiêu diệt Boss cuối.
- Căn cứ còn ít nhất 1 máu.

### Thua

- Máu căn cứ về 0.

Sau khi kết thúc run, hiển thị:

- Wave đạt được.
- Tổng số quái tiêu diệt.
- Tổng vàng Dice tạo ra.
- Số lần roll.
- Số lần roll ra 6.
- Số item đã mua.
- Số lần merge.
- Item gây nhiều sát thương nhất.
- Nâng cấp roguelike đã chọn.

---

## 29. Nguyên tắc cân bằng

### 29.1. Dice

- Dice phải hoàn vốn sau một khoảng thời gian hợp lý.
- Mua Dice quá muộn không nên luôn có lợi.
- Dice phẩm cấp cao phải hiệu quả hơn trên mỗi ô.
- Nhiều Dice phải tạo ra rủi ro thiếu sát thương.

### 29.2. Shop

- Người chơi phải thường xuyên có lựa chọn mua hữu ích.
- Không nên phụ thuộc hoàn toàn vào may mắn.
- Dice đầu game phải đủ dễ tiếp cận.
- Reroll phải có giá trị nhưng không được dùng vô hạn miễn phí.

### 29.3. Merge

- Merge phải tạo cảm giác mạnh hơn rõ ràng.
- Merge cần giúp giải phóng ô.
- Người chơi không nên luôn bắt buộc merge ngay lập tức.
- Giữ hai item cấp thấp đôi khi có thể mạnh hơn một item cấp cao, đổi lại chiếm nhiều ô hơn.

### 29.4. Roguelike

- Lựa chọn phải có tác động rõ ràng.
- Hạn chế các nâng cấp chỉ tăng chỉ số rất nhỏ.
- Nên có nâng cấp tạo combo.
- Các build khác nhau phải có cơ hội hoàn thành run.

---

## 30. Các câu hỏi cần kiểm chứng khi playtest

- Người chơi có hiểu ngay Dice dùng để tạo vàng không?
- Người chơi có cảm thấy vui khi chờ Dice roll không?
- Người chơi có mua quá nhiều Dice và chết sớm không?
- Người chơi có hiểu giá trị của việc merge không?
- Tám ô có quá ít hoặc quá nhiều không?
- Shop có tạo cảm giác may rủi quá cao không?
- Người chơi có thường xuyên bị kẹt vì đầy bảng không?
- Có đủ lý do để bán item không?
- Người chơi có bán Dice ở cuối run không?
- Weapon, Magic và Support có tạo build khác nhau rõ ràng không?
- Roguelike choice có làm thay đổi cách chơi không?
- Wave có đủ áp lực để buộc người chơi đưa ra quyết định không?
- Roll ra số cao có đủ thỏa mãn không?
- Người chơi có muốn chơi lại để thử build khác không?

---

## 31. Các yêu cầu không thuộc phạm vi tài liệu này

Tài liệu này chỉ mô tả thiết kế game và trải nghiệm người chơi.

Không bao gồm:

- Kiến trúc code.
- Cấu trúc class.
- Design Pattern.
- Unity component.
- ScriptableObject.
- Hệ thống save kỹ thuật.
- Object Pooling.
- Rendering.
- Shader.
- Animation Controller.
- Input API.
- Build pipeline.
- Tối ưu CPU, GPU hoặc bộ nhớ.
- Cấu trúc thư mục.
- Quy tắc Git.
- Chi tiết triển khai kỹ thuật.

AI hỗ trợ cần ưu tiên giữ đúng trải nghiệm và luật gameplay trong tài liệu, không tự thêm các cơ chế làm thay đổi vai trò cốt lõi của Dice.

---

## 32. Tóm tắt bắt buộc

Các nguyên tắc không được thay đổi:

1. Game là Tower Defense màn hình ngang, gói gọn trong một màn hình 1920x1080.
2. Quái đi từ phải sang trái và tấn công tường thành.
3. Người chơi có đúng 8 ô item, chia thành 2 cột, mỗi cột 4 ô, nằm bên trái tường thành.
4. Dice là một item có thể mua trong Shop bằng vàng.
5. Dice chiếm một ô như các item khác.
6. Dice hoạt động tương tự Sunflower: tạo tài nguyên theo thời gian.
7. Dice tự roll và tạo vàng dựa trên kết quả roll, và chỉ roll khi wave đang diễn ra.
8. Dice không trực tiếp tấn công quái.
9. Weapon tạo projectile hoặc đòn đánh để tiêu diệt quái.
10. Magic item tự cast phép.
11. Support item tăng sức mạnh hoặc hỗ trợ đội hình.
12. Hai item giống nhau, cùng phẩm cấp, có thể merge.
13. Merge tạo item phẩm cấp cao hơn và giải phóng một ô.
14. Quái chủ yếu tạo kinh nghiệm.
15. Lên level cho người chơi chọn một trong ba nâng cấp roguelike.
16. Core decision là cân bằng giữa kinh tế và sức mạnh chiến đấu.
17. Không tự thêm cơ chế Dice kích hoạt các item đứng cạnh.
18. Không biến Dice thành vũ khí mặc định.
19. Không bỏ giới hạn 8 ô.
20. Không đưa yêu cầu triển khai kỹ thuật vào Game Design Spec.
21. Shop chỉ mở trong giai đoạn mua sắm giữa các wave; mua sắm xong mới bắt đầu wave.

# Đảo Bình Yên · Thiết kế gameplay đặc trưng

Bản nháp 2, ngày 20/11/2026. Tác giả: Thuc Pham. Các số liệu ở đây là điểm xuất phát để cân chỉnh khi chơi thử.

## 1. Định vị

**Một câu:** sống chậm trên một hòn đảo làng biển Việt Nam vừa qua bão, theo nhịp con nước, tuần trăng và bốn mùa vườn ruộng, chữa lành hòn đảo và học các nghề truyền thống.

**Bốn trụ cột đặc trưng**

| Trụ cột | Người chơi cảm nhận | Khác các game nông trại cùng dòng ở chỗ |
|---|---|---|
| Con nước theo lịch âm | Bản đồ thay đổi mỗi ngày, phải canh giờ nước | Bờ biển là hệ thống sống, không phải đường viền cố định |
| Vườn ruộng bốn mùa | Mỗi mùa một bộ giống riêng, lúa nước, ghép cây, giữ giống | Canh tác gắn với đất, nước mặn, con nước và hệ sinh thái, không chỉ gieo, tưới, gặt |
| Hồi sinh hòn đảo | Mỗi việc làm khiến đảo tươi dần, thú và người quay về | Tiến độ đến từ chữa lành, không phải chặt phá và mở rộng |
| Nghề làng biển và chợ phiên | Học nghề với thợ cả, tự bán hàng ở chợ | Thay hệ thống giấy phép và thùng bán đồ |

Câu cá, lặn biển (biển) và vườn ruộng (đất) là hai nửa cân bằng của game. Con nước nối hai nửa: nó quyết định bãi triều, giờ câu, và cả việc ruộng ven đầm có bị nhiễm mặn hay không.

**Giữ nguyên (thuộc thể loại, không riêng game nào):** trồng trọt, câu cá, bắt bọ, nuôi thú, nấu ăn, chế tạo, túi đồ, chỉ số sinh tồn nhẹ, ngày và đêm, mùa.

**Tránh:** khởi đầu bằng việc một người dẫn dắt dựng lều rồi phát nhiệm vụ lập thị trấn; mua giấy phép bằng điểm để mở công cụ; mời dân về bằng giấy chứng nhận đất; bối cảnh và động vật vùng hoang mạc Úc.

## 2. Nhịp thời gian

| Đơn vị | Độ dài | Ghi chú |
|---|---|---|
| Ngày | 7:00–23:00, 2 phút game mỗi giây thật (8 phút thật) | Như đặc tả hiện tại |
| Tuần | 7 ngày | Ngày thứ 7 mỗi tuần họp chợ phiên |
| Tháng âm | 4 tuần = 28 ngày | Mỗi ngày game là một ngày âm. Mùng 1 trăng non, rằm (ngày 15) trăng tròn |
| Mùa | 2 tháng = 56 ngày | Xuân (tháng 1–2), Hạ (3–4), Thu (5–6, mùa bão), Đông (7–8) |
| Năm | 4 mùa = 8 tháng = 224 ngày | Khoảng 30 giờ chơi thật. Tết là mùng 1 tháng 1 |

Lịch âm là trục chính: con nước, chợ phiên, lễ hội, mùa cá và mùa vụ đều neo vào lịch này. Nhật ký và bản đồ lớn (M) có trang lịch, ghi pha trăng, bảng con nước và lịch thời vụ.

## 3. Trụ cột 1: Con nước

### Cơ chế
- Mỗi 24 giờ game có hai lần nước lớn và hai lần nước ròng (chu kỳ 12 giờ 25 phút). Trong khoảng 7:00–23:00 người chơi thường gặp một lần nước lớn và một lần nước ròng.
- Giờ nước lớn lùi khoảng 50 phút mỗi ngày, giống ngoài đời.
- Biên độ theo pha trăng: **nước rong** quanh mùng 1 và rằm (biên độ lớn nhất), **nước kém** quanh mùng 8 và 22 (biên độ nhỏ nhất).
- Mực nước = `SEA + A(trăng) × cos(2π × (giờ − giờ_lớn) / 12.42)`. Biên độ đề xuất: rong ±0,45 đơn vị, kém ±0,15. Cân chỉnh theo độ dốc bờ để khi ròng kiệt lộ ra dải bãi rộng 3–8 ô.

### Nội dung theo con nước

| Khi nào | Có gì |
|---|---|
| Nước ròng | Bãi triều: sò, nghêu, ốc, cua, sao biển, rong biển (đào bằng xẻng hoặc nhặt tay). Vũng nước đọng có cá bống và tôm tép (bắt bằng vợt) |
| Ròng kiệt ngày rong | Cồn cát nối ra đảo nhỏ, xác tàu đắm, rương kho báu theo bản đồ |
| Nước lớn | Cá vào sát bờ, cửa lạch đủ sâu cho ghe. Một số cá chỉ cắn câu khi nước đang lên |
| Đổi con nước | Đăng và lờ (bẫy cá cắm ở bãi triều) có cá. Mỗi lần nước lên rồi xuống thì nhặt được một lần |
| Nước rong | Ruộng thấp ven đầm bị nước mặn tràn vào nếu chưa đắp bờ bao (mục 4.6) |

### Ghi chú kỹ thuật
- `IslandData.SEA` đang là hằng số. Đổi thành mực nước động, đọc từ một `TideClock` trong `GameSession`.
- Các hàm `Walkable`, `TileHeight < SEA` (lội nước) và mặt nước đọc mực nước động. Mặt nước chỉ cần dời theo trục y.
- Màu đất: thêm dải cát ướt sẫm màu theo mực nước trong shader địa hình (không cần dựng lại lưới).
- Vật phẩm bãi triều sinh lại mỗi lần ròng, chỉ ở ô nằm giữa mực nước lớn và ròng của ngày đó.
- Thanh HUD có đồng hồ con nước nhỏ (mũi tên lên hoặc xuống + giờ nước kế tiếp).

## 4. Trụ cột 2: Vườn ruộng bốn mùa

### 4.1 Đất và nước
Mỗi ô đất có **loại đất** lấy từ vùng sinh thái của bản sinh địa hình. Mỗi giống hợp với một vài loại đất.

| Loại đất | Ở đâu | Hợp với |
|---|---|---|
| Cát pha | Bãi ven biển, gò cát | Dưa hấu, đậu phộng, khoai lang, tỏi, hành tím, thanh long, dừa |
| Phù sa | Ven rạch, ven suối | Lúa, rau muống, khoai môn, rau ăn lá, cây ăn trái miệt vườn |
| Đất đỏ đồi | Đồi giữa đảo | Bí đỏ, bắp, gừng, nghệ, xoài, mãng cầu, tiêu |
| Đất lợ | Quanh đầm, rừng đước | Lúa chịu mặn, nuôi tôm luân canh, cói, sen và súng ở ô ngập |
| Đất vườn nhà | Quanh lều, nhà | Rau thơm, ớt, cà, rau gia vị, chậu hoa |

**Nước tưới**
- Nguồn nước ngọt: suối, giếng tự đào bằng xẻng ở vùng có mạch nước, lu hứng nước mưa, mương dẫn nước.
- Ngày mưa thì cây tự được tưới.
- Mùa Hạ có những đợt nắng hạn: cây cần tưới 2 lần mỗi ngày.
- Tưới nước biển hoặc nước lợ làm cây héo, trừ các giống chịu mặn.

### 4.2 Giống theo mùa
Mỗi giống chỉ nảy mầm trong mùa của nó. Sang mùa khác thì cây ngắn ngày chết. Cây lâu năm sống qua các mùa nhưng chỉ ra trái đúng mùa.

**Xuân (tháng 1–2, mát, mưa phùn)**

| Giống | Ngày lớn | Thu lại | Đất | Ghi chú |
|---|---|---|---|---|
| Cải bẹ xanh | 5 | | Vườn, phù sa | |
| Xà lách | 4 | | Vườn | |
| Hành lá | 6 | Mỗi 4 ngày | Vườn | |
| Củ cải trắng | 8 | | Cát pha | Làm củ cải muối |
| Dưa leo | 10 | Mỗi 3 ngày | Mọi loại | Cần giàn tre |
| Đậu que | 9 | Mỗi 3 ngày | Mọi loại | Cần giàn tre, cải tạo đất (mục 4.4) |
| Bí đỏ | 14 | | Đất đỏ | |
| Nếp | 24 | | Ruộng nước | Gói bánh chưng Tết năm sau |
| Lúa đông xuân | 24 | | Ruộng nước | Vụ lúa chính, năng suất cao nhất |
| Cây trái vào mùa | | | | Mận (roi), vú sữa, bưởi |

**Hạ (tháng 3–4, nắng gắt, có đợt hạn)**

| Giống | Ngày lớn | Thu lại | Đất | Ghi chú |
|---|---|---|---|---|
| Dưa hấu | 12 | | Cát pha | |
| Bắp | 12 | | Đất đỏ, phù sa | Thân bắp làm giàn cho đậu |
| Đậu phộng | 14 | | Cát pha | Cải tạo đất |
| Khoai lang | 16 | | Cát pha | Thu cả ngọn rau lang |
| Ớt | 12 | Mỗi 4 ngày | Vườn | Lâu năm, qua được mùa Thu |
| Cà tím | 12 | Mỗi 4 ngày | Vườn | |
| Mướp, khổ qua | 10 | Mỗi 3 ngày | Mọi loại | Cần giàn tre |
| Rau muống | 5 | Cắt lại mỗi 3 ngày | Ruộng nước, ao | |
| Sen | 20 | | Ô ngập, đầm | Cho hạt sen, ngó sen, hoa sen |
| Cây trái vào mùa | | | | Xoài, chôm chôm, sầu riêng, măng cụt, thanh long |

**Thu (tháng 5–6, mùa mưa bão)**

| Giống | Ngày lớn | Thu lại | Đất | Ghi chú |
|---|---|---|---|---|
| Lúa hè thu | 24 | | Ruộng nước, đất lợ | Ruộng lợ: luân canh sau vụ tôm (mục 4.6) |
| Khoai môn | 18 | | Phù sa ẩm | |
| Bí đao | 14 | | Mọi loại | Cần giàn tre |
| Đậu bắp | 8 | Mỗi 3 ngày | Vườn | |
| Rau dền | 5 | | Vườn | |
| Mồng tơi | 6 | Mỗi 3 ngày | Vườn | Cần giàn tre |
| Gừng | 20 | | Đất đỏ | |
| Nghệ | 22 | | Đất đỏ | |
| Bông súng | 10 | Mỗi 4 ngày | Ô ngập, đầm | |
| Cây trái vào mùa | | | | Mãng cầu, ổi, hồng xiêm, cam sành |

**Đông (tháng 7–8, khô, gió bấc)**

| Giống | Ngày lớn | Thu lại | Đất | Ghi chú |
|---|---|---|---|---|
| Tỏi đảo | 24 | | Cát pha phủ đất đỏ | Đặc sản của đảo, giá cao nhất mùa |
| Hành tím | 18 | | Cát pha | |
| Cải thảo | 10 | | Vườn | |
| Su hào | 12 | | Vườn | |
| Cà rốt | 12 | | Cát pha | |
| Cà chua | 14 | Mỗi 4 ngày | Vườn | |
| Dưa hấu Tết | 14 | | Cát pha | Gieo giữa tháng 8 để thu đúng dịp Tết |
| Hoa vạn thọ | 12 | | Vườn | Bán Tết, xua tuyến trùng cho ô bên cạnh |
| Hoa cúc mâm xôi | 16 | | Vườn | Bán Tết |
| Cây trái vào mùa | | | | Quýt, cam, mai vàng nở dịp Tết |

**Quanh năm:** rau thơm (húng quế, tía tô, ngò gai, rau răm), sả, chuối, dừa (trên cát ven biển).

### 4.3 Lúa nước: chuỗi canh tác đặc trưng
1. Làm đất: cuốc hoặc dùng trâu cày (khi đã nuôi trâu). Trâu cày nhanh gấp 4 lần.
2. Gieo mạ ở ô nhỏ, 3 ngày.
3. Cấy mạ ra ruộng.
4. Giữ nước ruộng: dẫn nước bằng mương từ suối hoặc giếng. Ruộng cạn thì lúa chậm lớn.
5. Lúa trổ bông. Chim ăn lúa nếu không có bù nhìn.
6. Gặt bằng liềm, phơi lúa 1 ngày nắng, xay bằng cối xay để được gạo.
7. Phụ phẩm: rơm (ủ nấm rơm, lợp mái, làm thức ăn cho trâu), trấu (làm phân).

### 4.4 Ghép cây, giữ giống, xen canh
- **Ghép cây:** cây ăn trái trồng từ hạt chỉ cho trái hạng thường. Ghép cành lấy từ cây đầu dòng thì cho trái đặc sản. Cây đầu dòng tìm thấy ở các vùng có sức sống từ 75, hoặc nhận qua thư đất liền. Ghép hai giống với nhau ra giống lai có một đặc tính mới, ví dụ ra trái hai mùa, chịu mặn, hoặc trái to.
- **Giữ giống:** mỗi vụ chừa một cây cho già để lấy hạt. Hạt mang hạng chất lượng của cây mẹ. Chọn lọc qua 3 vụ liên tiếp thì thành **giống nhà**: tăng một hạng, và người chơi được đặt tên giống.
- **Xen canh:** đậu (đậu que, đậu phộng) làm tăng chất lượng các ô kề bên. Sả, hành, tỏi trồng quanh luống giảm sâu. Hoa vạn thọ xua tuyến trùng. Trồng một loại liền nhau trên 12 ô thì dễ bị sâu bệnh.

### 4.5 Phân bón, sâu bệnh, thiên địch
- **Phân bón:** phân cá (từ cá vụn khi làm cá, nối với câu cá), phân chuồng (gà, dê, trâu), rong biển ủ (từ bãi triều), tro bếp, bùn ao, trấu. Mỗi loại tăng chất lượng hoặc tốc độ lớn.
- **Sâu bệnh:** sâu xanh, rầy, ốc bươu. Dễ xuất hiện ở vùng có sức sống thấp hoặc khi trồng độc canh.
- **Trị bằng cách tự nhiên:** nước ngâm tỏi ớt, tro bếp, bắt tay. Không có thuốc hóa học.
- **Thiên địch:** khi vùng đạt sức sống 50 thì chim, ếch, ong xuất hiện. Sâu giảm, cây được thụ phấn, sản lượng dưa, bí và cây trái tăng 10%.

### 4.6 Ngăn mặn và luân canh lúa tôm
- Ruộng có mặt đất thấp hơn mực nước rong sẽ bị nước mặn tràn vào khi nước rong, nếu chưa đắp **bờ bao** (đất + đá) hoặc làm **cống** chặn nước.
- Ruộng bị mặn: cây chết, ô đất mặn trong 7 ngày (rửa mặn bằng nước ngọt thì nhanh hơn).
- Ở đất lợ có thể **luân canh**: mùa Hạ mở cống cho nước lợ vào nuôi tôm, mùa Thu đóng cống, rửa mặn rồi cấy lúa chịu mặn.

### 4.7 Hạng nông sản và đặc sản
- Ba hạng: **thường**, **ngon**, **đặc sản**. Hạng phụ thuộc giống, phân bón, tưới đều, xen canh và sức sống vùng.
- **Đặc sản đảo:** tỏi đảo, dưa hấu cát, gạo thơm Bình Yên, xoài ghép, dừa xiêm, nấm rơm, mật ong rừng đước. Bán ở chợ phiên được giá cao nhất, và là nguyên liệu cho các món ăn lễ hội.

### 4.8 Nguồn hạt giống
- **Sạp hạt giống của Cô Sáu Vườn** ở chợ phiên: 6–8 loại đúng mùa, đổi mỗi phiên. Mỗi tháng có một giống hiếm.
- Thư đất liền gửi hạt giống và cành ghép.
- Hạt giống dại nhặt được ở các vùng đã hồi sinh (rau dại, cây thuốc).
- Giữ giống từ vụ trước.
- Đổi giống, tặng giống qua lại với dân làng.
- **Nhà lưới** (nghề vườn bậc 3): một khu nhỏ trồng được giống trái mùa.

## 5. Trụ cột 3: Hồi sinh hòn đảo

### Bối cảnh khởi đầu
Một trận bão vừa đi qua. Người chơi ra đảo theo lời nhờ của gia đình ở đất liền (mục 7.6). Ông Bảy giữ hải đăng ở cầu tàu kể lại trận bão và chỉ chỗ dựng lều, rồi để người chơi tự do.

### Sức sống vùng
- Đảo chia theo các vùng sinh thái của bản sinh địa hình. Tên vùng gợi ý ở mục 9. Mỗi vùng có chỉ số **sức sống 0–100**, khởi đầu 20–40.
- **Sức sống đảo** = trung bình các vùng. Đây là thước đo tiến độ chính, hiện trên bản đồ lớn.

| Việc làm | Sức sống vùng |
|---|---|
| Nhặt rác dạt bờ | +1 mỗi món |
| Trồng cây non hoặc cây đước | +3 (khi cây sống qua 3 ngày) |
| Cấy san hô khi lặn | +5 |
| Khơi lại suối, dọn cây đổ chắn dòng | +8 |
| Duy trì vườn xen canh, không sâu bệnh qua một tuần | +2 |
| Chặt cây trưởng thành | −1 |
| Chặt cây non, đào hang thú | −4 |
| Câu, bắt, lặn bắt sinh vật đang vào mùa cấm | −10 (xem mục 8) |

### Ngưỡng mở khóa mỗi vùng

| Ngưỡng | Điều xảy ra |
|---|---|
| 25 | Cỏ hoa mọc lại, màu cảnh vật tươi hơn (tăng độ bão hòa màu đất và cây) |
| 50 | Chim, bướm, đom đóm, ếch, ong, cá của vùng xuất hiện. Thiên địch có tác dụng cho vườn |
| 75 | Một người dân mới chuyển đến (mỗi vùng một người, mang theo một nghề hoặc một cửa hàng). Có cây đầu dòng để ghép |
| 100 | Lễ mừng vùng hồi sinh, mở một khoảnh khắc bình yên đặc biệt |

### Khai thác bền vững
- Cây chặt mọc lại sau 3–7 ngày nếu vùng trên 50, chậm gấp đôi nếu dưới 50.
- Mỗi loài cá, bọ và sinh vật lặn có mùa sinh sản 1 tuần mỗi năm. Bắt trúng loài đang trong mùa thì có lựa chọn thả lại để được +2 bình yên.
- Đá và quặng chỉ có ở mỏ trên đồi. Mỏ tự lấp lại mỗi tháng, không đào vô hạn trên mặt đảo.

### Gắn với chỉ số bình yên
Thanh **bình yên** (đã có trong đặc tả) hồi nhanh hơn khi đứng ở vùng có sức sống cao. Ở vùng dưới 25, thanh không hồi quá 70%.

## 6. Trụ cột 4: Nghề làng biển và chợ phiên

### Học nghề (thay giấy phép)
Mỗi nghề do một thợ cả dạy, có 3 bậc. Muốn lên bậc thì mang đủ vật liệu và làm bài thử tay nghề (một trò chơi nhỏ). Thợ cả xuất hiện dần khi các vùng hồi sinh (ngưỡng 75). Ông Bảy, Bác Chín và Cô Sáu Vườn có mặt từ đầu.

| Nghề | Thợ cả | Bậc 1 mở | Bậc 2 mở | Bậc 3 mở |
|---|---|---|---|---|
| Làm vườn | Cô Sáu Vườn | Giàn tre, giữ giống | Ghép cây, ruộng lúa, mương dẫn nước | Nhà lưới, giống lai |
| Lặn biển | O Mận | Kính lặn: đầm nông | Chân vịt: rạn gần bờ | Lặn rạn ngoài khơi, lặn đêm |
| Đan lưới | Bà Tư Lưới | Vợt bắt bọ và cá vũng | Lờ, đăng đặt bãi triều; lồng cấy san hô | Lưới quăng (bắt cả đàn cá) |
| Làm muối | Chú Năm Diêm | Ô ruộng muối nhỏ | Ruộng muối lớn, muối hạt to | Muối ớt, muối tôm (bán giá cao) |
| Làm nước mắm | Dì Ba Mắm | Chum nhỏ, ủ 7 ngày | Chum lớn, ủ 14–28 ngày, có hạng chất lượng | Nước mắm nhỉ hạng nhất |
| Gốm | Ông Hai Lò | Nồi đất, chậu cây | Chum ủ, đèn đất | Đồ trang trí nhà |
| Đóng ghe | Chú Út Ghe | Sửa ghe thúng cũ (đi được trong đầm) | Ghe thúng có đèn (câu mực đêm) | Ghe lớn ra các đảo xa |

### Các quy trình nhiều ngày
- **Ruộng muối:** dẫn nước biển vào ô lúc nước lớn, phơi 2 ngày nắng, cào muối. Mưa thì mất mẻ.
- **Nước mắm:** cá cơm + muối vào chum. Càng ủ lâu càng lên hạng (thường, ngon, nhỉ).
- **Cá khô, mực khô, củ cải muối, dưa cải:** phơi hoặc muối 1–3 ngày.

### Chợ phiên
- Họp ngày thứ 7 mỗi tuần. Phiên rơi vào mùng 1 hoặc rằm, hoặc phiên gần nhất với hai ngày đó, là phiên lớn, đông khách hơn.
- Người chơi dựng sạp 6 ô, đặt giá trong khoảng 60–160% giá gốc.
- Khách có sở thích riêng (ví dụ bà cụ thích nước mắm hạng cao, đám trẻ thích đồ chơi gốm, khách đất liền thích đặc sản đảo). Giá cao quá thì khách trả giá, người chơi chọn bớt hoặc giữ giá.
- **Uy tín sạp** tăng khi bán đồ chất lượng với giá hợp lý. Uy tín cao mở thêm ô sạp và khách từ đảo khác.
- Tiệm tạp hóa của Thím Tám thu mua mọi ngày nhưng chỉ trả 50% giá gốc.

## 7. Tính năng phụ

### 7.1 Lặn biển
- **Học từ O Mận** (nghề lặn, 3 bậc):

| Bậc | Đồ lặn | Vùng lặn | Hơi thở |
|---|---|---|---|
| 1 | Kính lặn | Đầm nông, sâu tới 3 m | 15 giây |
| 2 | Chân vịt + ống thở | Rạn gần bờ, sâu tới 6 m, bơi nhanh hơn | 30 giây |
| 3 | Đèn lặn | Rạn ngoài khơi, sâu tới 12 m, lặn đêm | 45 giây |

- **Thanh hơi thở** hiện khi xuống nước. Nổi lên mặt thì hồi. Mỗi lần lặn tốn năng lượng.
- **Dưới nước có:** san hô để cấy (hồi sinh), hải sâm, bào ngư, nhum, ốc hương, trai ngọc (ngọc trai có hạng), rong sụn, chai thư chìm, rương trong xác tàu.
- **Theo mùa và con nước:** mùa Đông nước lạnh, hơi thở giảm 30%. Mùa bão có ngày sóng lớn, cấm lặn ngoài rạn. Nước ròng kiệt thì rạn gần bờ cạn hơn, dễ lặn hơn.
- **Ghi chú kỹ thuật:** cần thêm chế độ bơi khi nước sâu quá ngưỡng, camera dưới nước (sương xanh, vệt sáng loang), và địa hình đáy biển có rạn. Đáy biển hiện là một mặt phẳng ở độ cao −1,65.

### 7.2 Ghe thúng và quần đảo nhỏ
3–5 đảo nhỏ quanh đảo chính, mỗi đảo là một cảnh làm sẵn: đảo yến, đảo đá có hang, bãi rùa đẻ trứng (chỉ xem, không bắt), đảo dừa. Ra đảo bằng ghe hoặc đi bộ theo cồn cát lúc ròng kiệt.

### 7.3 Câu mực đêm
Từ 19:00 đến 23:00, chèo ghe có đèn ra đầm. Ánh đèn kéo mực lại gần, người chơi giật mồi theo nhịp. Đêm trăng tròn ít mực, đêm không trăng nhiều mực.

### 7.4 Gió mùa và bão
- Mùa Thu có 1–2 cơn bão, báo trước 2 ngày qua radio của ông Bảy.
- Trước bão: chằng lều, kéo ghe lên bờ, đậy chum mắm, thu hoạch sớm, chống cây ăn trái.
- Hậu quả nếu bỏ sót: xem mục 8.
- Sau bão: biển dạt vào gỗ trôi, chai thư, đồ hiếm, và rác (cơ hội tăng sức sống).

### 7.5 Lễ hội theo lịch âm

| Lễ | Khi nào | Hoạt động |
|---|---|---|
| Tết | Mùng 1 tháng 1 | Dọn nhà, gói bánh chưng (nếp, đậu xanh, lá dong), chưng hoa và dưa hấu, lì xì cho trẻ trong làng |
| Rằm tháng giêng | Rằm tháng 1 | Thả hoa đăng trên đầm |
| Cầu ngư | Rằm tháng 3 (đầu mùa Hạ, vào mùa cá) | Đua ghe thúng, chợ phiên lớn nhất năm |
| Trung thu | Rằm tháng 6 (tháng thứ hai của mùa Thu) | Làm lồng đèn (nghề gốm hoặc đan), rước đèn đêm với trẻ con |
| Cúng cơm mới | Sau vụ lúa đông xuân | Nấu cơm gạo mới mời cả làng |

### 7.6 Khoảnh khắc bình yên
Bộ sưu tập những khoảnh khắc nhỏ, tự chụp một tấm ảnh vào nhật ký khi gặp. Ví dụ: ngắm hoàng hôn trên cầu tàu, ngồi bên lửa trại nghe mưa, thấy đom đóm ở rừng đước, ngắm trăng rằm khi nước lớn, thấy rùa con bò ra biển, gió thổi qua ruộng lúa trổ bông, đàn cá vây quanh khi lặn rạn. Mỗi khoảnh khắc +10 bình yên ở lần đầu.

### 7.7 Thư về đất liền
Mỗi tháng có ghe thư của Bác Chín ghé cầu tàu. Người chơi viết thư (chọn câu kể chuyện tháng qua), gửi kèm quà. Thư hồi âm dần kể vì sao gia đình nhờ người chơi ra đảo, và gửi lại hạt giống, cành ghép, công thức nấu ăn.

## 8. Hình phạt (mức nặng)

| Tình huống | Hậu quả |
|---|---|
| Còn ở ngoài lều lúc 23:00 | Ngất, mất toàn bộ đồ trong túi (như đặc tả hiện tại) |
| Kẹt trên cồn khi nước lên | Bác Chín chèo ra đón. Mất 30% tiền (không giới hạn), mọi thức ăn và hạt giống trong túi bị ướt hỏng |
| Hết hơi khi lặn | Ngất, tỉnh dậy ở cầu tàu. Mất toàn bộ đồ trong túi và 20% tiền, hết ngày |
| Không chuẩn bị trước bão | Cây ngắn ngày ngoài trời hư 70%, cây ăn trái rụng hết trái của mùa, ghe chưa kéo lên bị trôi mất (đóng lại tốn vật liệu), chum mắm không đậy hỏng, mẻ muối mất |
| Ruộng chưa đắp bờ bao khi nước rong | Cây trong ruộng chết, đất mặn 7 ngày |
| Quên tưới | Ngày đầu cây héo, ngày thứ hai cây chết |
| Bắt sinh vật trong mùa cấm | Sức sống vùng −10, phạt tiền gấp 3 giá trị con vật |
| Năng lượng về 0 | Đi chậm (×0,6, như đặc tả), không dùng được dụng cụ nặng |

Để mức phạt nặng mà vẫn công bằng, game luôn báo trước: đồng hồ con nước, radio báo bão, lịch thời vụ, thanh hơi thở nhấp nháy khi còn 5 giây.

## 9. Tên gọi

Tên nhân vật theo kiểu gọi thứ trong nhà của miền Nam và miền Tây (Hai, Ba, Tư, Năm, Sáu, Bảy, Chín, Út), kèm nghề làm biệt danh. Người miền Trung dùng cách xưng hô riêng như "O" (cô), "Mệ" (bà).

| Nhân vật | Vai trò | Giọng |
|---|---|---|
| Ông Bảy | Giữ hải đăng, kể chuyện bão, báo bão qua radio | Miền Trung |
| Bác Chín Đò | Lái đò, ghe thư hằng tháng, cứu người kẹt nước | Miền Tây |
| Cô Sáu Vườn | Thợ cả làm vườn, bán hạt giống ở chợ phiên | Miền Tây |
| O Mận | Thợ cả lặn biển | Miền Trung |
| Bà Tư Lưới | Thợ cả đan lưới | Miền Nam |
| Chú Năm Diêm | Thợ cả làm muối | Miền Trung |
| Dì Ba Mắm | Thợ cả làm nước mắm | Miền Nam |
| Ông Hai Lò | Thợ cả gốm | Miền Trung |
| Chú Út Ghe | Thợ cả đóng ghe | Miền Tây |
| Thím Tám | Chủ tiệm tạp hóa, thu mua hằng ngày | Miền Nam |
| Mệ Hường | Bà cụ ở xóm chài, thích nước mắm ngon, kể chuyện xưa | Miền Trung |
| Bé Bi, thằng Tèo, con Mít | Trẻ con trong làng, theo người chơi đi rước đèn, bắt dế | |

**Câu thoại mẫu**
- Ông Bảy: "Bữa ni nước ròng sớm, ra bãi mà lượm ốc, nhớ coi chừng nước lên nghe con."
- Bác Chín Đò: "Kẹt trên cồn hả? Lên ghe đi, bác chở dzô, mà lần sau coi giờ nước nghen."
- Cô Sáu Vườn: "Mùa này trồng dưa hấu trên cát là ngọt dữ lắm đó!"
- O Mận: "Lặn chi mà lâu rứa? Hụt hơi là chết đó nghe."

**Tên vùng gợi ý:** Xóm Chài, Bãi Ghềnh, Rạch Bần, Rừng Đước, Đầm Sen, Đồi Sim, Gò Cát, Mũi Gió.

## 10. Thay đổi so với đặc tả hiện tại

| Đặc tả hiện tại | Thay bằng |
|---|---|
| Giấy phép (licences) | Học nghề với thợ cả, 3 bậc, bài thử tay nghề |
| Ông lão trên cầu tàu | Ông Bảy giữ hải đăng: kể chuyện, báo bão, không phát nhiệm vụ lập làng |
| Thị trấn có sẵn, dân đông dần | Làng nhỏ có sẵn vài nhà; dân mới đến khi vùng đạt sức sống 75 |
| Trồng trọt cơ bản (cuốc, tưới, gieo, thu) | Vườn ruộng bốn mùa: loại đất, nước ngọt và nước mặn, lúa nước, ghép cây, giữ giống, xen canh, thiên địch |
| Bảo tàng | Sổ tay sinh vật + đầm nuôi ở rạn san hô đã hồi sinh (thả cá, san hô vào xem) |
| Chai thư, bản đồ kho báu | Giữ; chai thư dạt nhiều sau bão, kho báu thường nằm ở bãi lộ khi ròng kiệt hoặc dưới đáy rạn |
| Mùa, thời tiết | Giữ; thêm lịch âm 28 ngày, mùa 2 tháng, gió mùa, bão |
| Ngất lúc 23:00 mất đồ trong túi | Giữ, và thêm các hình phạt ở mục 8 |

## 11. Lộ trình gắn với các giai đoạn

| Giai đoạn | Thêm vào |
|---|---|
| 3 (công cụ, trồng trọt, câu cá, chế tạo, lưu game) | Lịch âm và con nước (mực nước động, bãi triều, đồng hồ con nước). Trồng trọt theo mùa: loại đất, dữ liệu giống 4 mùa (ScriptableObject), tưới, hạng nông sản, giàn tre, cây lâu năm. Sức sống vùng ở dạng dữ liệu |
| 4 (làng, NPC, cửa hàng, thú) | Ông Bảy, Bác Chín, Cô Sáu Vườn, Thím Tám. Học nghề thay giấy phép, chợ phiên. Lúa nước, mương, trâu cày, bờ bao ngăn mặn, phân chuồng. Ngưỡng sức sống và người dân mới |
| 5 (bản đồ, menu, tạo nhân vật) | Trang lịch âm, bảng con nước, lịch thời vụ và lớp sức sống vùng trong bản đồ lớn |
| 6 (mùa, thời tiết, bảo tàng, nấu ăn) | Bão và gió mùa, lễ hội, khoảnh khắc bình yên, thư đất liền. Lặn biển và địa hình đáy rạn, ghe thúng, đảo nhỏ, câu mực đêm. Ghép cây, giữ giống, xen canh, thiên địch, luân canh lúa tôm, nhà lưới. Các nghề còn lại |

## 12. Đã chốt và câu hỏi còn mở

**Đã chốt (20/11/2026)**
- Tháng 4 tuần, 28 ngày. Mùa 2 tháng. Năm 4 mùa (8 tháng, 224 ngày).
- Có lặn biển.
- Hình phạt mức nặng.
- Tên nhân vật thuần Việt, giọng miền Nam, miền Trung, miền Tây.
- Trồng trọt là một trụ cột, ngang hàng với câu cá.

**Còn mở**
1. Một năm 224 ngày (khoảng 30 giờ chơi thật) có quá dài với người chơi mới không? Nếu cần, có thể tăng tốc đồng hồ hoặc cho ngủ sớm nhiều hơn.
2. Có muốn trâu là vật nuôi kéo cày được (cần thêm hệ dắt thú) không?
3. Có làm luân canh lúa tôm trong bản đầu, hay để sau?

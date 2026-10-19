// Tạo Google Form khảo sát đặt chuyến đi theo nhóm (bản đầy đủ hơn, ~3 phút, ẩn danh, một mạch không chia vai trò).
// Cách dùng: script.google.com > dán toàn bộ file > chọn hàm taoForm > Chạy > xem link ở Nhật ký thực thi.
function taoForm() {
  var f = FormApp.create('Khảo sát: đi du lịch theo nhóm (3 phút)');
  f.setDescription('Khảo sát của sinh viên HUTECH cho đồ án chuyên ngành, chỉ dùng cho học tập. Ẩn danh, mất khoảng 3 phút. Cảm ơn bạn đã giúp mình!');
  f.setCollectEmail(false);

  function mc(t, c, req) { f.addMultipleChoiceItem().setTitle(t).setChoiceValues(c).setRequired(req !== false); }
  function cb(t, c) { f.addCheckboxItem().setTitle(t).setChoiceValues(c).setRequired(true); }
  function sc(t, a, b) { f.addScaleItem().setTitle(t).setBounds(1,5).setLabels(a,b).setRequired(true); }

  // --- Về bạn ---
  mc('1. Bạn thuộc nhóm tuổi nào?', ['Dưới 22','22–30','31–45','Trên 45']);
  mc('2. Trong 12 tháng qua, bạn có đi du lịch/tour cùng nhóm từ 4 người trở lên không?', ['Có','Không (nếu chọn Không, bạn vẫn trả lời tiếp các câu còn lại theo suy nghĩ của mình)']);
  mc('3. Bạn thường đi du lịch nhóm bao nhiêu lần một năm?', ['Chưa bao giờ','1–2 lần','3–4 lần','Trên 4 lần']);

  // --- Chuyến gần nhất ---
  cb('4. Bạn thường đi nhóm kiểu nào? (chọn nhiều)', ['Gia đình/họ hàng','Bạn bè','Đồng nghiệp/công ty','Lớp/CLB/hội nhóm','Khác']);
  mc('5. Nhóm đi gần nhất có bao nhiêu người?', ['4–6','7–10','11–20','Trên 20']);
  mc('6. Ai đứng ra đặt tour và trả tiền trước cho cả nhóm?', ['Mình','Người khác trong nhóm','Mỗi người tự đặt/trả phần mình']);
  cb('7. Nhóm bạn thường thu thông tin và bàn chuyện tiền bạc qua đâu? (chọn nhiều)', ['Nhóm chat Zalo/Messenger','Gọi điện/SMS','Google Form/Sheet','Gặp mặt, nói chuyện trực tiếp','Khác']);

  // --- Khó khăn ---
  cb('8. Việc thu thông tin/giấy tờ và thu tiền của nhóm bạn gặp khó khăn nào? (chọn nhiều; nếu bạn là người đứng ra đặt thì trả lời theo trải nghiệm của bạn, nếu là thành viên thì theo điều bạn thấy ở người đứng ra)', [
    'Phải nhắn từng người để xin thông tin/giấy tờ',
    'Thông tin gửi thiếu hoặc sai',
    'Người ứng tiền phải đi đòi lại',
    'Không biết ai đã trả, ai chưa',
    'Có người trả trễ hoặc bỏ nhóm giữa chừng',
    'Không có hạn chốt rõ ràng',
    'Ngại gửi CCCD/hộ chiếu qua tin nhắn nhóm',
    'Không gặp khó khăn']);
  mc('9. Từ lúc chốt đi đến khi thu đủ tiền mất bao lâu?', ['Trong 1 ngày','2–3 ngày','4–7 ngày','Hơn 1 tuần']);
  mc('10. Bạn thường phải nhắc nhở người chưa trả bao nhiêu lần?', ['0 lần','1–2 lần','3–5 lần','Trên 5 lần']);
  mc('11. Có khi nào trong nhóm có người báo bỏ đi sát ngày khởi hành, làm cả nhóm lúng túng chuyện tiền/chỗ không?', ['Chưa từng','Thỉnh thoảng','Thường xuyên']);
  sc('12. Mức độ phiền phức của việc thu thông tin và thu tiền nhóm', 'Không phiền', 'Rất phiền');

  // --- Giải pháp ---
  f.addSectionHeaderItem().setTitle('Ý tưởng của mình').setHelpText('Mỗi người trong nhóm nhận một "ghế" riêng qua link mời, tự điền thông tin của mình và tự trả phần của mình bằng QR chuyển khoản. Trưởng nhóm xem được ai đã trả, ai chưa nhưng không xem được giấy tờ của người khác. Ghế quá hạn mà chưa trả sẽ được nhả ra.');
  sc('13. Bạn có muốn dùng cách này khi đi nhóm không?', 'Không muốn', 'Rất muốn');
  cb('14. Tính năng nào bạn thấy cần nhất? (chọn tối đa 3)', [
    'Mỗi người tự trả phần của mình',
    'Link mời, nhận ghế không cần tạo tài khoản phức tạp',
    'Xem tiến độ ai đã trả/chưa trả',
    'Nhắc tự động người chưa trả',
    'Hạn giữ chỗ rõ ràng, quá hạn tự nhả ghế',
    'Giấy tờ chỉ mình và hệ thống thấy, trưởng nhóm không thấy']);
  mc('15. Nên giữ chỗ cho cả nhóm tối đa bao lâu để mọi người kịp trả?', ['12 giờ','24 giờ','48 giờ','72 giờ']);
  mc('16. Bạn thích cách trả tiền nào nhất?', ['Quét QR chuyển khoản ngân hàng','Ví điện tử (MoMo, ZaloPay...)','Thẻ ATM/Visa','Tiền mặt cho trưởng nhóm']);
  mc('17. Nếu một người trong nhóm không trả đúng hạn, bạn thấy nên xử lý thế nào?', ['Nhả ghế đó cho người khác','Nhắc thêm một lần rồi mới nhả','Trưởng nhóm tự quyết','Hủy cả đơn nhóm']);
  mc('18. Bạn đã từng đặt tour qua website/app nào chưa?', ['Chưa','Có, đặt cho riêng mình','Có, đặt cho cả nhóm']);

  f.addParagraphTextItem().setTitle('19. Điều gì làm bạn khó chịu nhất khi đi du lịch nhóm? Góp ý thêm (không bắt buộc)').setRequired(false);

  Logger.log('Sửa: ' + f.getEditUrl());
  Logger.log('Gửi người khảo sát: ' + f.getPublishedUrl());
}

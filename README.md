# NEO-MART — Quản lý Siêu thị (WinForms C#)

## Cách mở và chạy nhanh
1. Giải nén thư mục `NeoMartWinForms`.
2. Mở file **`NeoMartWinForms.csproj`** bằng **Visual Studio 2022** (hoặc chạy lệnh `dotnet run` trong thư mục dự án).
3. Dữ liệu được tự động lưu và đọc từ file **`sieuthi.txt`** (định dạng `MÃ|TÊN|NHÓM|GIÁ|SL`) nằm cùng thư mục chạy của ứng dụng.

## Tính năng chính
- **KPI Dashboard trực quan:** Tổng giá trị tồn kho, tổng số lượng, mặt hàng giá cao/thấp nhất, cảnh báo sắp hết hàng.
- **CRUD & Kiểm tra hợp lệ:** Thêm, sửa, xoá mặt hàng; chặn để trống, chặn ký tự phân cách `|`, chặn trùng mã sản phẩm.
- **Tìm kiếm & Lọc đa điều kiện:** Tìm nhanh theo mã hoặc tên (debounce 150ms), lọc theo nhóm hàng, trạng thái tồn kho (`Còn hàng`, `Sắp hết`, `Hết hàng`) và ngưỡng tồn kho tuỳ chỉnh.
- **Sắp xếp & Lưu file:** Bấm trực tiếp vào tiêu đề cột để sắp xếp tăng/giảm; tự động đồng bộ `sieuthi.txt`.


## Tính năng thao tác hàng loạt và tối ưu

- Chọn nhiều mặt hàng bằng checkbox ở đầu mỗi dòng.
- Nút **Chọn tất cả / Bỏ chọn** áp dụng trên danh sách đang hiển thị (sau khi lọc/tìm kiếm).
- Xoá hàng loạt các mặt hàng đã chọn, có hộp thoại xác nhận và chỉ ghi dữ liệu một lần.
- Phím tắt trong bảng: `Ctrl + A` chọn/bỏ chọn tất cả; `Delete` xoá các mặt hàng đã đánh dấu.
- Tìm kiếm không phân biệt chữ hoa/chữ thường, tránh tạo chuỗi thường mới cho từng sản phẩm.
- Ghi dữ liệu qua tệp tạm và thay thế tệp chính để giảm nguy cơ hỏng dữ liệu khi đang lưu.
- Các nút thao tác chọn nhiều tự ẩn phần thống kê phụ khi cửa sổ hẹp để tránh chồng lấn giao diện.

Dữ liệu vẫn được lưu trong `sieuthi.txt` cạnh file chạy ứng dụng; định dạng dữ liệu cũ được giữ nguyên.


## Hoàn tác khi xoá nhầm

- Sau khi xoá một sản phẩm hoặc xoá hàng loạt, bấm **↶ Hoàn tác xoá** ở thanh dưới cùng để khôi phục lần xoá gần nhất.
- Có thể dùng phím tắt **Ctrl + Z** khi bảng sản phẩm đang được chọn.
- Tính năng hoàn tác giữ một lần xoá gần nhất trong phiên chạy hiện tại; lần xoá tiếp theo sẽ thay thế lịch sử hoàn tác trước đó.
- Nếu mã sản phẩm đã được tạo lại trước khi hoàn tác, sản phẩm trùng mã sẽ được bỏ qua để tránh dữ liệu trùng lặp.

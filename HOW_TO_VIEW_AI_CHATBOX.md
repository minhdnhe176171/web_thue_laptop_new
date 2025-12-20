# Hướng dẫn xem AI Chatbox

## Vị trí hiển thị

AI Chatbox widget sẽ hiển thị ở:
- **Góc dưới bên phải** của trang web
- Nút tròn màu tím với gradient (#667eea → #764ba2)
- Có animation pulse (nhấp nháy) để thu hút sự chú ý
- Kích thước: 60x60px

## Cách kiểm tra

### 1. Mở trình duyệt và truy cập trang web
- Chạy ứng dụng (F5 hoặc `dotnet run`)
- Mở trình duyệt và vào `http://localhost:5000` (hoặc port của bạn)

### 2. Kiểm tra góc dưới bên phải
- Scroll xuống cuối trang
- Nhìn vào góc dưới bên phải
- Bạn sẽ thấy nút tròn màu tím với icon chat

### 3. Mở Developer Console (F12)
- Nhấn F12 để mở DevTools
- Vào tab Console
- Kiểm tra có message: "AI Chat Widget: Initialization complete"

### 4. Nếu không thấy widget

**Kiểm tra các điều sau:**

#### a) Kiểm tra HTML có được render không
- Mở DevTools (F12)
- Vào tab Elements/Inspector
- Tìm element có id `aiChatWidget`
- Kiểm tra xem nó có trong DOM không

#### b) Kiểm tra CSS
- Kiểm tra xem có CSS nào override `display: none` không
- Widget có `z-index: 10000` - đảm bảo không có element nào có z-index cao hơn

#### c) Kiểm tra Console errors
- Mở Console (F12)
- Xem có lỗi JavaScript nào không
- Nếu có lỗi, copy và báo lại

#### d) Clear cache và reload
- Nhấn Ctrl + Shift + R (hard reload)
- Hoặc Ctrl + F5

## Tính năng

1. **Click vào nút** → Mở cửa sổ chat
2. **Gửi tin nhắn** → AI sẽ tư vấn laptop
3. **Tự động mở lần đầu** → Widget tự động mở sau 2 giây khi lần đầu truy cập

## Vị trí CSS

Widget được định vị với:
```css
position: fixed;
bottom: 20px;
right: 20px;
z-index: 10000;
```

## Troubleshooting

### Widget không hiển thị
1. Kiểm tra file `_Layout.cshtml` có include `@await Html.PartialAsync("_AIChatWidget")` không
2. Kiểm tra file `_AIChatWidget.cshtml` có tồn tại không
3. Clear browser cache
4. Kiểm tra Console có lỗi không

### Widget bị che khuất
- Widget có `z-index: 10000` - nên luôn ở trên cùng
- Nếu vẫn bị che, kiểm tra element nào có z-index cao hơn

### Click không hoạt động
- Mở Console (F12) và kiểm tra lỗi JavaScript
- Đảm bảo SignalR đã load (có thể chat vẫn hoạt động dù SignalR fail)

## Liên hệ hỗ trợ

Nếu vẫn không thấy widget, vui lòng:
1. Mở Console (F12)
2. Copy tất cả errors
3. Chụp screenshot
4. Báo lại với thông tin trên



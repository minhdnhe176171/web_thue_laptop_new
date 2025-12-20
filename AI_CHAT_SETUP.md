# Hướng dẫn cấu hình AI Chatbox với RAG

## Tổng quan

Đã tạo hệ thống AI Chatbox sử dụng RAG (Retrieval-Augmented Generation) để tư vấn laptop cho khách hàng. Hệ thống bao gồm:

1. **RAG Service** - Truy xuất và so khớp laptop từ database
2. **AI Chat Service** - Xử lý cuộc trò chuyện với LLM (OpenAI)
3. **AI Chat Controller & Hub** - API và SignalR cho real-time chat
4. **Floating Chat Widget** - Giao diện chat widget nổi trên trang web

## Tính năng

- 🤖 AI tư vấn tự động dựa trên nhu cầu khách hàng
- 🔍 Phân tích nhu cầu: văn phòng, đồ họa, gaming, lập trình
- 💰 Nhận diện ngân sách từ tin nhắn khách hàng
- 📱 Giao diện chat widget đẹp, responsive
- 🔄 Real-time chat sử dụng SignalR
- 💾 Lưu trữ lịch sử hội thoại trong Redis
- 🎯 Đề xuất tối đa 3 laptop phù hợp nhất

## Cấu hình

### 1. Cấu hình OpenAI API Key

Mở file `appsettings.json` và thêm API key của bạn:

```json
"OpenAI": {
    "ApiKey": "sk-your-api-key-here",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-3.5-turbo"
}
```

**Lưu ý:**
- Cần có tài khoản OpenAI và API key
- Có thể sử dụng Azure OpenAI bằng cách thay đổi `BaseUrl`
- Nếu không có API key, hệ thống sẽ sử dụng fallback responses (không cần LLM)

### 2. Cấu hình tên shop

Cập nhật tên shop trong `appsettings.json`:

```json
"ShopSettings": {
    "Name": "Tên Shop Của Bạn"
}
```

### 3. Cấu hình Redis (tùy chọn)

Nếu bạn muốn lưu lịch sử chat, cấu hình Redis:

```json
"ConnectionStrings": {
    "Redis": "localhost:6379"
}
```

**Lưu ý:** Nếu không có Redis, chat vẫn hoạt động nhưng không lưu lịch sử.

## Cách hoạt động

### 1. RAG (Retrieval-Augmented Generation)

- **Retrieval**: Hệ thống truy vấn database để lấy laptop có sẵn (status = "available", không có người đang thuê)
- **Analysis**: Phân tích tin nhắn khách hàng để xác định:
  - Loại sử dụng (văn phòng, đồ họa, gaming, lập trình)
  - Ngân sách (từ các từ khóa như "5 triệu", "dưới 3 triệu", v.v.)
  - Yêu cầu CPU, RAM, GPU
- **Matching**: So khớp và tính điểm phù hợp cho từng laptop
- **Generation**: Gửi laptop phù hợp cùng với context cho LLM để tạo phản hồi tư vấn

### 2. Quy trình tư vấn

1. Khách hàng mở chat widget (nút chat ở góc dưới bên phải)
2. AI chào hỏi và hỏi nhu cầu sử dụng
3. Khách hàng trả lời về mục đích sử dụng và ngân sách
4. AI phân tích và tìm laptop phù hợp
5. AI đề xuất tối đa 3 laptop kèm lý do
6. Khách hàng có thể xem chi tiết laptop bằng cách click vào đề xuất

### 3. Prompt mẫu

AI được cấu hình với prompt:
- Vai trò: Chuyên gia tư vấn cho thuê laptop
- Nhiệm vụ: Tư vấn dựa trên danh sách laptop có sẵn
- Quy trình: Chào hỏi → Hỏi nhu cầu → Hỏi ngân sách → Đề xuất laptop
- Phong cách: Ngắn gọn, thân thiện, chuyên nghiệp

## Các file đã tạo

### Services
- `Services/RagService.cs` - Service xử lý RAG
- `Services/AIChatService.cs` - Service xử lý AI chat

### Controllers
- `Controllers/AIChatController.cs` - API endpoints cho AI chat

### Hubs
- `Hubs/AIChatHub.cs` - SignalR hub cho real-time chat

### Views
- `Views/Shared/_AIChatWidget.cshtml` - Floating chat widget

### Configuration
- `appsettings.json` - Đã thêm cấu hình OpenAI và ShopSettings

### Program.cs
- Đã đăng ký services và SignalR hub

## Sử dụng

### Hiển thị Widget

Widget được tự động hiển thị trên tất cả các trang thông qua `_Layout.cshtml`.

### API Endpoints

#### Gửi tin nhắn
```
POST /AIChat/SendMessage
Body: {
    "message": "Tôi cần laptop để chơi game",
    "sessionId": "optional-session-id"
}
```

#### Lấy lịch sử
```
GET /AIChat/GetHistory?sessionId=optional-session-id
```

### SignalR Hub

Connect đến: `/aichathub`
- Method: `JoinConversation(string sessionId)`
- Event: `ReceiveMessage` - Nhận tin nhắn từ AI

## Tùy chỉnh

### Thay đổi prompt AI

Sửa `SystemPromptTemplate` trong `Services/AIChatService.cs`

### Thay đổi logic matching

Sửa method `FindMatchingLaptopsAsync` trong `Services/RagService.cs`

### Thay đổi giao diện

Sửa CSS và HTML trong `Views/Shared/_AIChatWidget.cshtml`

## Xử lý lỗi

- Nếu không có OpenAI API key: Hệ thống sử dụng fallback responses (vẫn hoạt động)
- Nếu không có Redis: Chat vẫn hoạt động nhưng không lưu lịch sử
- Nếu database lỗi: Kiểm tra connection string trong `appsettings.json`

## Testing

1. Chạy ứng dụng
2. Mở trình duyệt và truy cập trang web
3. Click vào nút chat ở góc dưới bên phải
4. Thử các câu hỏi:
   - "Tôi cần laptop để chơi game"
   - "Laptop cho văn phòng dưới 5 triệu"
   - "Máy tính cho lập trình viên"

## Ghi chú

- Widget tự động mở lần đầu khi khách truy cập (sau 2 giây)
- Lịch sử chat được lưu theo session ID
- Có thể chỉnh sửa tên shop trong prompt để phù hợp với cửa hàng của bạn



# Flow Kiến Trúc AI Studio — Phú Cường Group

Web app local giúp tạo video kiến trúc bằng Google Flow theo chế độ bán tự động. App **không bypass Google Flow**, **không lấy token Google**, **không gọi API nội bộ Google**, **không spam nút generate**. App chỉ dùng OpenAI API ở backend để phân tích ảnh tham chiếu và tạo prompt JSON chuẩn, sau đó hỗ trợ copy/điền prompt thủ công qua Chrome Extension Manifest V3.

## 1. Cài app

```bash
npm install
```

## 2. Tạo `.env.local`

Sao chép file mẫu:

```bash
cp .env.example .env.local
```

## 3. Thêm `OPENAI_API_KEY`

Mở `.env.local` và điền key ở backend:

```env
OPENAI_API_KEY=sk-...
OPENAI_MODEL=gpt-5.5
BRAND_COMPANY=Phú Cường Group
BRAND_HOTLINE=0905263048
BRAND_WEBSITE=https://phucuonggroups.com/thiet-ke-thi-cong-tron-goi-khanh-hoa-2026/
FLOW_PROJECT_URL=https://labs.google/fx/tools/flow/project/732480f4-d40a-4750-a507-05cb09d6f997
```

> Bảo mật: API key chỉ nằm trong `.env.local`, không lưu ở frontend, extension, localStorage hoặc ZIP export.

## 4. Chạy app

```bash
npm run dev
```

Mở `http://localhost:3000`.

## 5. Cài Chrome Extension

1. Mở Chrome: `chrome://extensions`.
2. Bật **Developer mode**.
3. Chọn **Load unpacked**.
4. Chọn thư mục `extension/` trong dự án.

Extension chỉ chạy trên `https://labs.google/fx/tools/flow/*`, không chứa OpenAI API key và không gọi OpenAI.

## 6. Mở Google Flow project

Trong tab **Tổng quan**, bấm **Mở Google Flow Project** hoặc mở URL:

```text
https://labs.google/fx/tools/flow/project/732480f4-d40a-4750-a507-05cb09d6f997
```

## 7. Upload ảnh vào app

Vào tab **Ảnh tham chiếu**:

- Upload nhiều ảnh.
- Gán nhãn: `@ẢNH_KIẾN_TRÚC`, `@ẢNH_NHÀ_BÊ_TÔNG`, `@ẢNH_NỘI_THẤT`, `@ẢNH_CẢNH_ĐÊM`, `@ẢNH_VẬT_LIỆU`, `@ẢNH_NHÂN_VẬT`.
- Ghi chú từng ảnh.
- Nếu ảnh quá lớn, app cảnh báo và chỉ lưu metadata để tránh vượt giới hạn localStorage.
- App không nhận diện danh tính người thật; nếu ảnh có người, prompt chỉ mô tả đặc điểm thị giác.

## 8. Tạo prompt

Vào tab **Tạo prompt Flow**:

1. Nhập lệnh ngắn, ví dụ: `Tạo video 40 giây nhà phố bê tông hiện đại, golden hour, sang trọng.`
2. Bấm **Tạo bộ prompt Flow**.
3. App gọi `/api/generate-flow-json`.
4. Kết quả được lưu vào localStorage gồm raw result và parsed result.

Nếu thiếu API key, app báo:

```text
Chưa cấu hình OpenAI API Key. Vui lòng thêm OPENAI_API_KEY vào file .env.local rồi khởi động lại app.
```

## 9. Copy/điền vào Flow

- Tab **Prompt IMAGE** hiển thị từng IMAGE JSON riêng.
- Tab **Prompt VIDEO** hiển thị từng CLIP JSON riêng.
- Bấm **Copy JSON** để copy thủ công.
- Bấm **Gửi sang extension** để lưu prompt vào clipboard/local buffer, sau đó mở extension và bấm **Điền prompt vào Google Flow**.

Extension sẽ tìm `textarea` hoặc vùng `contenteditable`. Nếu không tìm thấy, extension báo:

```text
Không tìm thấy ô nhập prompt. Vui lòng copy thủ công.
```

## 10. Xử lý lỗi unusual activity

Nếu Google Flow hiển thị:

- `Failed`
- `We noticed some unusual activity`

Extension sẽ báo:

```text
Google Flow đang lỗi hoặc giới hạn tạm thời. Hãy tải lại trang Flow rồi tiếp tục.
```

Hãy bấm **Reload Flow**, chờ Flow ổn định rồi tiếp tục thao tác thủ công.

## 11. Đặt tên ảnh `@IMAGE_X`

Sau khi Google Flow tạo ảnh xong:

1. Chọn ảnh đẹp.
2. Vào tab **Ảnh đã chọn @IMAGE_X**.
3. Thêm tên ảnh từ `@IMAGE_1` đến `@IMAGE_11`.
4. Không được trùng tên.
5. App hiển thị ma trận:
   - `@IMAGE_1 → @IMAGE_2 = CLIP 1`
   - `@IMAGE_3 → @IMAGE_4 = CLIP 2`
   - `@IMAGE_5 → @IMAGE_6 = CLIP 3`
   - `@IMAGE_7 → @IMAGE_8 = CLIP 4`
   - `@IMAGE_9 → @IMAGE_10 = CLIP 5`

Khi đủ start/end, clip tương ứng chuyển sang trạng thái **Sẵn sàng tạo video**.

## 12. Upload clip từ Flow

Sau khi Flow tạo video:

1. Tải clip `.mp4` về máy.
2. Vào tab **Upload clip từ Google Flow**.
3. Upload lần lượt `CLIP 1` đến `CLIP 5`.
4. App preview clip nếu localStorage đủ dung lượng, nếu clip lớn sẽ chỉ lưu metadata.

## 13. Tạo lệnh FFmpeg hậu kỳ

Vào tab **Hậu kỳ & thương hiệu**:

- Cài đặt tên công ty, hotline, website.
- Chọn màu chữ trắng hoặc vàng champagne.
- Chọn vị trí chữ dưới trái hoặc giữa cuối video.
- Bật/tắt overlay và end-card.
- Bấm **Tạo ffmpeg-commands.txt**.

Lệnh FFmpeg gồm:

- Ghép `CLIP 1 → CLIP 5`.
- Chèn text:
  - `Phú Cường Group`
  - `Thiết kế & thi công trọn gói`
  - `Hotline: 0905263048`
  - `phucuonggroups.com`

Nếu môi trường không chạy FFmpeg trực tiếp, hãy copy lệnh sang máy có FFmpeg và đặt file clip vào thư mục `clips/`.

## 14. Xuất ZIP

Vào tab **Xuất dự án**, bấm **Xuất ZIP**.

ZIP gồm:

- `project-settings.json`
- `prompts.json`
- `storyboard.txt`
- `selected-images.json`
- `ffmpeg-commands.txt`
- `uploaded-reference-images/`
- `selected-images/`
- `clips/`

ZIP không bao gồm `.env.local`, API key, token hoặc file bí mật.

## Build production

```bash
npm run build
npm run start
```

## Cấu trúc chính

```text
app/                         Next.js App Router UI và API routes
lib/                         OpenAI client, parser, storage, FFmpeg command, types
extension/                   Chrome Extension Manifest V3
.env.example                 Mẫu cấu hình an toàn
README.md                    Hướng dẫn tiếng Việt
```

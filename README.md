# PCG AI Marketing – Windows Desktop

Ứng dụng Windows native dành riêng cho **PHÚ CƯỜNG GROUP**.

## Chức năng phiên bản 1

- Trợ lý tạo 3, 5 hoặc 10 bài Facebook bằng OpenAI Responses API.
- Mỗi bài có tiêu đề, hook, nội dung, CTA, hashtag, footer và prompt ảnh.
- Tạo ảnh bằng AI ngay trong ứng dụng.
- Tự tải logo PCG từ nguồn đã cung cấp, xóa nền trắng và ghép logo thật lên ảnh.
- Chỉnh sửa, tạo ảnh, duyệt bài, copy content.
- Kho nội dung theo trạng thái: bản nháp, đã duyệt, đã lên lịch, đã đăng.
- Kết nối nhiều Facebook Page; token được mã hóa bằng Windows DPAPI.
- Tự xếp nhiều lần đăng mỗi ngày và lệch 20 phút giữa các fanpage.
- Bộ lập lịch tự thử lại tối đa 3 lần khi mất mạng hoặc Meta báo lỗi tạm thời.
- Nhật ký đăng thành công và thất bại.
- Dữ liệu lưu tại LocalAppData bằng cơ chế ghi file nguyên tử và có bản sao dự phòng.

## Thông tin thương hiệu đã cài sẵn

- PHÚ CƯỜNG GROUP DESIGN & BUILD
- CHUẨN TỪ MÓNG – VỮNG TỪ TÂM
- Website: phucuonggroups.com
- Hotline: 0905 233 978 – 0905 263 048
- Liên hệ: Thảo – 0903 570 014
- Địa chỉ: 1216 Lê Hồng Phong, Phường Nam Nha Trang, Khánh Hòa

## Build thủ công

```powershell
dotnet publish PCGAIMarketing/PCGAIMarketing.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Đầu ra: `publish/PCG_AI_Marketing.exe`.

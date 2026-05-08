import type { BrandSettings, UploadedClip } from './types';

function escapeDrawText(text: string): string {
  return text.replace(/\\/g, '\\\\').replace(/:/g, '\\:').replace(/'/g, "\\'").replace(/%/g, '\\%');
}

export function buildFfmpegCommands(clips: UploadedClip[], brand: BrandSettings, ratio = '16:9'): string {
  const ordered = [...clips].sort((a, b) => Number(a.clipName.replace('CLIP ', '')) - Number(b.clipName.replace('CLIP ', '')));
  const concatLines = Array.from({ length: 5 }, (_, index) => `file 'clips/CLIP_${index + 1}.mp4'`).join('\n');
  const color = brand.textColor === 'champagne' ? '#F2DFAD' : '#FFFFFF';
  const x = brand.textPosition === 'bottom-center' ? '(w-text_w)/2' : '48';
  const yCompany = brand.textPosition === 'bottom-center' ? 'h-170' : 'h-190';
  const yService = brand.textPosition === 'bottom-center' ? 'h-118' : 'h-138';
  const yHotline = brand.textPosition === 'bottom-center' ? 'h-84' : 'h-104';
  const yWebsite = brand.textPosition === 'bottom-center' ? 'h-52' : 'h-72';
  const overlayFilter = brand.overlayEnabled
    ? `drawtext=text='${escapeDrawText(brand.company)}':fontcolor=${color}:fontsize=42:x=${x}:y=${yCompany}:shadowcolor=black@0.6:shadowx=2:shadowy=2,drawtext=text='${escapeDrawText('Thiết kế & thi công trọn gói')}':fontcolor=white:fontsize=26:x=${x}:y=${yService}:shadowcolor=black@0.6:shadowx=2:shadowy=2,drawtext=text='${escapeDrawText(`Hotline: ${brand.hotline}`)}':fontcolor=white:fontsize=24:x=${x}:y=${yHotline}:shadowcolor=black@0.6:shadowx=2:shadowy=2,drawtext=text='${escapeDrawText(brand.websiteDisplay)}':fontcolor=${color}:fontsize=24:x=${x}:y=${yWebsite}:shadowcolor=black@0.6:shadowx=2:shadowy=2`
    : 'copy';
  const missing = ordered.length < 5 ? `\n# CẢNH BÁO: hiện mới có ${ordered.length}/5 clip. Hãy tải đủ CLIP 1 đến CLIP 5 trước khi ghép bản cuối.\n` : '';
  const endCard = brand.endCardEnabled
    ? `\n# 4) End-card tùy chọn: tạo thêm 3 giây cuối bằng màu tối và text thương hiệu:\nffmpeg -y -f lavfi -i color=c=0x07090f:s=1920x1080:d=3 -vf "drawtext=text='${escapeDrawText(brand.company)}':fontcolor=${color}:fontsize=58:x=(w-text_w)/2:y=(h-text_h)/2-80,drawtext=text='${escapeDrawText(`Hotline: ${brand.hotline}`)}':fontcolor=white:fontsize=34:x=(w-text_w)/2:y=(h-text_h)/2,drawtext=text='${escapeDrawText(brand.websiteDisplay)}':fontcolor=${color}:fontsize=34:x=(w-text_w)/2:y=(h-text_h)/2+52" -t 3 end-card.mp4\n`
    : '\n# 4) End-card đang tắt trong cài đặt brand.\n';

  return `# Flow Kiến Trúc AI Studio — FFmpeg commands
# Tỉ lệ xuất: ${ratio}
# 1) Tạo file danh sách clip theo đúng thứ tự:
cat > clips.txt <<'CLIPS'
${concatLines}
CLIPS

# 2) Ghép CLIP 1 → CLIP 5 thành bản nháp:
ffmpeg -y -f concat -safe 0 -i clips.txt -c copy merged-raw.mp4

# 3) Chèn thương hiệu Phú Cường Group:
ffmpeg -y -i merged-raw.mp4 -vf "${overlayFilter}" -c:a copy final-phu-cuong-group.mp4
${endCard}${missing}`;
}

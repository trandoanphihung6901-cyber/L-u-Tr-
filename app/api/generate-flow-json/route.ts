import { NextResponse } from 'next/server';
import { FLOW_SYSTEM_PROMPT } from '@/lib/flow-system-prompt';
import { getOpenAIClient, getOpenAIModel } from '@/lib/openai';
import { parseFlowResponse } from '@/lib/parse-flow-response';
import type { ProjectSettings, SelectedImage, UploadedImage } from '@/lib/types';

export const runtime = 'nodejs';

interface GenerateRequest {
  shortCommand: string;
  projectSettings: ProjectSettings;
  uploadedImages: UploadedImage[];
  selectedImages: SelectedImage[];
}

export async function POST(request: Request) {
  if (!process.env.OPENAI_API_KEY) {
    return NextResponse.json({ ok: false, message: 'Chưa cấu hình OpenAI API Key. Vui lòng thêm OPENAI_API_KEY vào file .env.local rồi khởi động lại app.' }, { status: 200 });
  }

  try {
    const body = (await request.json()) as Partial<GenerateRequest>;
    const uploadedImages = body.uploadedImages ?? [];
    const selectedImages = body.selectedImages ?? [];
    const imageContent = uploadedImages
      .filter((image) => image.base64 && image.mimeType.startsWith('image/'))
      .slice(0, 8)
      .map((image) => ({
        type: 'image_url' as const,
        image_url: { url: image.base64?.startsWith('data:') ? image.base64 : `data:${image.mimeType};base64,${image.base64}` }
      }));

    const userText = `Lệnh ngắn: ${body.shortCommand || 'Tạo video kiến trúc cao cấp 40 giây.'}\n\nCài đặt dự án: ${JSON.stringify(body.projectSettings ?? {})}\n\nẢnh tham chiếu đã upload (không nhận diện danh tính người thật, chỉ mô tả thị giác): ${JSON.stringify(uploadedImages.map(({ id, name, label, note, mimeType }) => ({ id, name, label, note, mimeType })))}\n\nẢnh đã chọn hiện có: ${JSON.stringify(selectedImages.map(({ imageName, note }) => ({ imageName, note })))}\n\nHãy tạo đúng 10 IMAGE JSON và 5 CLIP JSON theo quy tắc, ưu tiên prompt dùng trực tiếp được trong Google Flow.`;

    const client = getOpenAIClient();
    const completion = await client.chat.completions.create({
      model: getOpenAIModel(),
      messages: [
        { role: 'system', content: FLOW_SYSTEM_PROMPT },
        { role: 'user', content: [{ type: 'text', text: userText }, ...imageContent] }
      ],
      temperature: 0.7
    });

    const rawText = completion.choices[0]?.message?.content ?? '';
    const data = parseFlowResponse(rawText);
    return NextResponse.json({ ok: true, data });
  } catch (error) {
    const message = error instanceof Error ? error.message : 'OpenAI lỗi khi tạo prompt Flow.';
    return NextResponse.json({ ok: false, message }, { status: 200 });
  }
}

import { NextResponse } from 'next/server';
import { getOpenAIClient, getOpenAIModel } from '@/lib/openai';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

export async function GET() {
  if (!process.env.OPENAI_API_KEY) {
    return NextResponse.json({ ok: false, message: 'Chưa cấu hình OpenAI API Key.', model: getOpenAIModel() }, { status: 200 });
  }
  try {
    const client = getOpenAIClient();
    await client.chat.completions.create({
      model: getOpenAIModel(),
      messages: [{ role: 'user', content: 'Trả lời ngắn: OK' }],
      max_tokens: 10
    });
    return NextResponse.json({ ok: true, message: 'Kết nối OpenAI thành công. Flow Kiến Trúc AI đã sẵn sàng.', model: getOpenAIModel() });
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Không thể kết nối OpenAI.';
    return NextResponse.json({ ok: false, message, model: getOpenAIModel() }, { status: 200 });
  }
}

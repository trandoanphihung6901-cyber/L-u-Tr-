import { NextResponse } from 'next/server';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

export async function GET() {
  return NextResponse.json({
    ok: true,
    message: 'Export ZIP đang được xử lý client-side bằng JSZip để tránh gửi file dự án và dữ liệu nhạy cảm lên server.'
  });
}

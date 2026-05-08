import { NextResponse } from 'next/server';
import { buildFfmpegCommands } from '@/lib/ffmpeg-command';
import { defaultBrandSettings, type BrandSettings, type UploadedClip } from '@/lib/types';

export const runtime = 'nodejs';

export async function POST(request: Request) {
  const body = (await request.json()) as { clips?: UploadedClip[]; brandSettings?: BrandSettings; ratio?: string };
  const commands = buildFfmpegCommands(body.clips ?? [], { ...defaultBrandSettings, ...body.brandSettings }, body.ratio ?? '16:9');
  return NextResponse.json({ ok: true, commands });
}

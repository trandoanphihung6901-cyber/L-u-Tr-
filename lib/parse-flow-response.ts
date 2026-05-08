import type { FlowImagePrompt, FlowVideoPrompt, GeneratedPrompts } from './types';

type JsonRecord = Record<string, unknown>;

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isImagePrompt(value: unknown): value is FlowImagePrompt {
  return isRecord(value) && value.prompt_type === 'image_keyframe' && typeof value.image_name === 'string';
}

function isVideoPrompt(value: unknown): value is FlowVideoPrompt {
  return isRecord(value) && value.prompt_type === 'video_clip' && typeof value.clip_name === 'string' && isRecord(value.asset_references);
}

function extractJsonCandidates(text: string): string[] {
  const fenced = Array.from(text.matchAll(/```(?:json)?\s*([\s\S]*?)```/gi)).map((match) => match[1]?.trim()).filter(Boolean) as string[];
  const candidates = [...fenced];
  let depth = 0;
  let start = -1;
  let inString = false;
  let escaped = false;
  for (let index = 0; index < text.length; index += 1) {
    const char = text[index];
    if (inString) {
      if (escaped) escaped = false;
      else if (char === '\\') escaped = true;
      else if (char === '"') inString = false;
      continue;
    }
    if (char === '"') inString = true;
    if (char === '{') {
      if (depth === 0) start = index;
      depth += 1;
    }
    if (char === '}') {
      depth -= 1;
      if (depth === 0 && start >= 0) {
        candidates.push(text.slice(start, index + 1));
        start = -1;
      }
    }
  }
  return Array.from(new Set(candidates));
}

function sectionBetween(text: string, start: string, end?: string): string {
  const startIndex = text.indexOf(start);
  if (startIndex < 0) return '';
  const bodyStart = startIndex + start.length;
  const endIndex = end ? text.indexOf(end, bodyStart) : -1;
  return text.slice(bodyStart, endIndex > bodyStart ? endIndex : undefined).trim();
}

export function parseFlowResponse(rawText: string): GeneratedPrompts {
  const imagePrompts: FlowImagePrompt[] = [];
  const videoPrompts: FlowVideoPrompt[] = [];
  let failedJsonCount = 0;

  for (const candidate of extractJsonCandidates(rawText)) {
    try {
      const parsed = JSON.parse(candidate) as unknown;
      const objects = Array.isArray(parsed) ? parsed : [parsed];
      for (const object of objects) {
        if (isImagePrompt(object)) imagePrompts.push(object);
        if (isVideoPrompt(object)) videoPrompts.push(object);
      }
    } catch {
      failedJsonCount += 1;
    }
  }

  const uniqueImages = Array.from(new Map(imagePrompts.map((prompt) => [prompt.image_name, prompt])).values());
  const uniqueVideos = Array.from(new Map(videoPrompts.map((prompt) => [prompt.clip_name, prompt])).values());
  const script = sectionBetween(rawText, '1. KỊCH BẢN VIDEO FLOW', '2. STORYBOARD') || sectionBetween(rawText, 'KỊCH BẢN VIDEO FLOW', 'STORYBOARD');
  const storyboard = sectionBetween(rawText, '2. STORYBOARD', '3. CÂU LỆNH TẠO ẢNH KEYFRAME') || sectionBetween(rawText, 'STORYBOARD', 'CÂU LỆNH TẠO ẢNH KEYFRAME');

  return {
    script,
    storyboard,
    imagePrompts: uniqueImages,
    videoPrompts: uniqueVideos,
    rawText,
    parseWarning: failedJsonCount > 0 || (uniqueImages.length === 0 && uniqueVideos.length === 0)
      ? 'Một số JSON chưa tách được tự động. Bạn vẫn có thể copy bản đầy đủ.'
      : undefined
  };
}

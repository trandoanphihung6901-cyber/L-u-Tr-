import OpenAI from 'openai';

export function getOpenAIModel(): string {
  return process.env.OPENAI_MODEL || 'gpt-5.5';
}

export function getOpenAIClient(): OpenAI {
  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey) {
    throw new Error('Chưa cấu hình OpenAI API Key. Vui lòng thêm OPENAI_API_KEY vào file .env.local rồi khởi động lại app.');
  }
  return new OpenAI({ apiKey });
}

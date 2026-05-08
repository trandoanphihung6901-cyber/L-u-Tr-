import { defaultBrandSettings, defaultProjectSettings, type ProjectDraft } from './types';

export const STORAGE_KEY = 'flow-kien-truc-ai-studio-draft-v1';

export function createDefaultDraft(): ProjectDraft {
  return {
    projectSettings: defaultProjectSettings,
    uploadedImages: [],
    selectedImages: [],
    uploadedClips: [],
    brandSettings: defaultBrandSettings,
    ffmpegCommands: '',
    exportedZip: false,
    shortCommand: 'Tạo video 40 giây nhà phố bê tông hiện đại, golden hour, sang trọng.'
  };
}

export function loadDraft(): ProjectDraft {
  if (typeof window === 'undefined') return createDefaultDraft();
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) return createDefaultDraft();
  try {
    const parsed = JSON.parse(raw) as Partial<ProjectDraft>;
    return {
      ...createDefaultDraft(),
      ...parsed,
      projectSettings: { ...defaultProjectSettings, ...parsed.projectSettings },
      brandSettings: { ...defaultBrandSettings, ...parsed.brandSettings },
      uploadedImages: parsed.uploadedImages ?? [],
      selectedImages: parsed.selectedImages ?? [],
      uploadedClips: parsed.uploadedClips ?? []
    };
  } catch {
    return createDefaultDraft();
  }
}

export function saveDraft(draft: ProjectDraft): void {
  if (typeof window === 'undefined') return;
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(draft));
}

export function resetDraft(): void {
  if (typeof window === 'undefined') return;
  window.localStorage.removeItem(STORAGE_KEY);
}

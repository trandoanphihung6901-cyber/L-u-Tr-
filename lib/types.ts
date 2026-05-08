export const FLOW_PROJECT_URL = process.env.FLOW_PROJECT_URL ?? 'https://labs.google/fx/tools/flow/project/732480f4-d40a-4750-a507-05cb09d6f997';

export const IMAGE_LABELS = [
  '@ẢNH_KIẾN_TRÚC',
  '@ẢNH_NHÀ_BÊ_TÔNG',
  '@ẢNH_NỘI_THẤT',
  '@ẢNH_CẢNH_ĐÊM',
  '@ẢNH_VẬT_LIỆU',
  '@ẢNH_NHÂN_VẬT'
] as const;

export type ImageLabel = (typeof IMAGE_LABELS)[number];
export type BadgeStatus = 'pending' | 'ready' | 'copied' | 'selected' | 'missing' | 'done' | 'error';

export interface ProjectSettings {
  projectName: string;
  description: string;
  durationSeconds: number;
  clipCount: number;
  secondsPerClip: number;
  videoStyle: string;
  flowProjectUrl: string;
}

export interface UploadedImage {
  id: string;
  name: string;
  label: ImageLabel;
  note: string;
  mimeType: string;
  base64?: string;
  size: number;
  createdAt: string;
}

export interface SelectedImage {
  id: string;
  imageName: `@IMAGE_${number}`;
  note: string;
  fileName?: string;
  mimeType?: string;
  base64?: string;
  createdAt: string;
}

export interface UploadedClip {
  id: string;
  clipName: `CLIP ${number}`;
  fileName: string;
  mimeType: string;
  base64?: string;
  size: number;
  createdAt: string;
}

export interface BrandSettings {
  company: string;
  hotline: string;
  website: string;
  websiteDisplay: string;
  textColor: 'white' | 'champagne';
  textPosition: 'bottom-left' | 'bottom-center';
  overlayEnabled: boolean;
  endCardEnabled: boolean;
}

export interface FlowImagePrompt {
  prompt_type: 'image_keyframe';
  task: string;
  image_name: string;
  image_role: string;
  asset_references: Record<string, unknown>;
  'ảnh_đưa_vào_flow': string;
  composition_json: Record<string, unknown>;
  architecture_json: Record<string, unknown>;
  character_json?: Record<string, unknown>;
  scene_prompt: string;
  negative_prompt: string;
}

export interface FlowVideoPrompt {
  prompt_type: 'video_clip';
  task: string;
  clip_name: string;
  clip_role: string;
  asset_references: {
    start_frame: string;
    end_frame: string;
    [key: string]: unknown;
  };
  video_json: {
    duration: string;
    transition_type: string;
    camera_movement: string;
    scene_continuity: string;
    character_status: string;
    architecture_lock: string;
    motion_rules: string;
    [key: string]: unknown;
  };
  prompt: string;
  negative_prompt: string;
}

export interface GeneratedPrompts {
  script: string;
  storyboard: string;
  imagePrompts: FlowImagePrompt[];
  videoPrompts: FlowVideoPrompt[];
  rawText: string;
  parseWarning?: string;
}

export interface ProjectDraft {
  projectSettings: ProjectSettings;
  uploadedImages: UploadedImage[];
  selectedImages: SelectedImage[];
  uploadedClips: UploadedClip[];
  generatedPrompts?: GeneratedPrompts;
  brandSettings: BrandSettings;
  ffmpegCommands: string;
  exportedZip: boolean;
  shortCommand: string;
}

export const defaultProjectSettings: ProjectSettings = {
  projectName: 'Video kiến trúc Phú Cường Group',
  description: 'Dự án tạo video kiến trúc bán tự động bằng Google Flow.',
  durationSeconds: 40,
  clipCount: 5,
  secondsPerClip: 8,
  videoStyle: 'Quảng cáo kiến trúc cao cấp',
  flowProjectUrl: FLOW_PROJECT_URL
};

export const defaultBrandSettings: BrandSettings = {
  company: 'Phú Cường Group',
  hotline: '0905263048',
  website: 'https://phucuonggroups.com/thiet-ke-thi-cong-tron-goi-khanh-hoa-2026/',
  websiteDisplay: 'phucuonggroups.com',
  textColor: 'champagne',
  textPosition: 'bottom-left',
  overlayEnabled: true,
  endCardEnabled: true
};

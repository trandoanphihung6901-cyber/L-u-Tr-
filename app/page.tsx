'use client';

import JSZip from 'jszip';
import { useEffect, useMemo, useState } from 'react';
import { buildFfmpegCommands } from '@/lib/ffmpeg-command';
import { createDefaultDraft, loadDraft, resetDraft, saveDraft } from '@/lib/storage';
import {
  IMAGE_LABELS,
  type BadgeStatus,
  type FlowImagePrompt,
  type FlowVideoPrompt,
  type GeneratedPrompts,
  type ImageLabel,
  type ProjectDraft,
  type SelectedImage,
  type UploadedClip,
  type UploadedImage
} from '@/lib/types';

const tabs = ['Tổng quan', 'Ảnh tham chiếu', 'Tạo prompt Flow', 'Prompt IMAGE', 'Ảnh đã chọn @IMAGE_X', 'Prompt VIDEO', 'Upload clip từ Google Flow', 'Hậu kỳ & thương hiệu', 'Xuất dự án', 'Cài đặt nâng cao'];
const imagePairs = [[1, 2], [3, 4], [5, 6], [7, 8], [9, 10]] as const;
const maxLocalStorageImageBytes = 850_000;

type Toast = { type: 'success' | 'error' | 'info'; message: string };

function badgeClasses(status: BadgeStatus) {
  const map: Record<BadgeStatus, string> = {
    pending: 'border-slate-600 bg-slate-800 text-slate-300',
    ready: 'border-amber-400/60 bg-amber-400/10 text-amber-200',
    copied: 'border-cyan-400/60 bg-cyan-400/10 text-cyan-200',
    selected: 'border-emerald-400/60 bg-emerald-400/10 text-emerald-200',
    missing: 'border-orange-400/60 bg-orange-400/10 text-orange-200',
    done: 'border-green-400/60 bg-green-400/10 text-green-200',
    error: 'border-red-400/60 bg-red-400/10 text-red-200'
  };
  return `rounded-full border px-2.5 py-1 text-xs font-semibold uppercase tracking-wide ${map[status]}`;
}

function Badge({ status, children }: { status: BadgeStatus; children: React.ReactNode }) {
  return <span className={badgeClasses(status)}>{children}</span>;
}

function Panel({ title, children, action }: { title: string; children: React.ReactNode; action?: React.ReactNode }) {
  return <section className="rounded-3xl border border-studio-line bg-studio-panel/90 p-5 shadow-glow">
    <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <h2 className="text-xl font-bold text-white">{title}</h2>
      {action}
    </div>
    {children}
  </section>;
}

function Button({ children, onClick, disabled, variant = 'primary', type = 'button' }: { children: React.ReactNode; onClick?: () => void; disabled?: boolean; variant?: 'primary' | 'ghost' | 'danger'; type?: 'button' | 'submit' }) {
  const styles = variant === 'primary' ? 'bg-studio-gold text-slate-950 hover:bg-studio-champagne' : variant === 'danger' ? 'border border-red-400/50 bg-red-500/10 text-red-200 hover:bg-red-500/20' : 'border border-studio-line bg-white/5 text-slate-100 hover:bg-white/10';
  return <button type={type} disabled={disabled} onClick={onClick} className={`rounded-xl px-4 py-2 text-sm font-bold transition disabled:cursor-not-allowed disabled:opacity-45 ${styles}`}>{children}</button>;
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="grid gap-2 text-sm text-slate-300"><span>{label}</span>{children}</label>;
}

const inputClass = 'w-full rounded-xl border border-studio-line bg-slate-950/70 px-3 py-2 text-white outline-none focus:border-studio-gold';

function fileToDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(new Error('Không đọc được file.'));
    reader.readAsDataURL(file);
  });
}

function promptText(prompt: FlowImagePrompt | FlowVideoPrompt) {
  return JSON.stringify(prompt, null, 2);
}

function flowClipReady(selectedImages: SelectedImage[], clipIndex: number) {
  const [start, end] = imagePairs[clipIndex - 1];
  const names = new Set(selectedImages.map((image) => image.imageName));
  return names.has(`@IMAGE_${start}`) && names.has(`@IMAGE_${end}`);
}

async function safeClipboard(text: string): Promise<void> {
  await navigator.clipboard.writeText(text);
  window.localStorage.setItem('flowStudio:lastPrompt', text);
}

export default function Home() {
  const [draft, setDraft] = useState<ProjectDraft>(() => createDefaultDraft());
  const [activeTab, setActiveTab] = useState(0);
  const [toast, setToast] = useState<Toast | null>(null);
  const [loading, setLoading] = useState(false);
  const [health, setHealth] = useState<{ ok: boolean; message: string; model?: string } | null>(null);
  const [selectedForm, setSelectedForm] = useState({ imageName: '@IMAGE_1', note: '' });

  useEffect(() => setDraft(loadDraft()), []);
  useEffect(() => saveDraft(draft), [draft]);
  useEffect(() => {
    if (!toast) return;
    const timer = window.setTimeout(() => setToast(null), 3600);
    return () => window.clearTimeout(timer);
  }, [toast]);

  const checklist = useMemo(() => [
    ['Đã upload ảnh tham chiếu', draft.uploadedImages.length > 0],
    ['Đã tạo prompt IMAGE', Boolean(draft.generatedPrompts?.imagePrompts.length)],
    ['Đã chọn đủ @IMAGE_X', imagePairs.every((_, index) => flowClipReady(draft.selectedImages, index + 1))],
    ['Đã tạo prompt VIDEO', Boolean(draft.generatedPrompts?.videoPrompts.length)],
    ['Đã upload clip', draft.uploadedClips.length >= 5],
    ['Đã xuất ZIP', draft.exportedZip]
  ], [draft]);

  const updateDraft = (patch: Partial<ProjectDraft>) => setDraft((current) => ({ ...current, ...patch }));
  const show = (message: string, type: Toast['type'] = 'success') => setToast({ message, type });

  async function copy(text: string) {
    try {
      await safeClipboard(text);
      show('Đã copy prompt và lưu vào vùng đệm extension.', 'success');
    } catch {
      show('Không copy được tự động. Vui lòng bôi đen và copy thủ công.', 'error');
    }
  }

  async function handleReferenceUpload(files: FileList | null) {
    if (!files) return;
    const items: UploadedImage[] = [];
    for (const file of Array.from(files)) {
      const base64 = file.size <= maxLocalStorageImageBytes ? await fileToDataUrl(file) : undefined;
      if (!base64) show(`Ảnh ${file.name} quá lớn, chỉ lưu metadata để tránh vượt localStorage.`, 'error');
      items.push({ id: crypto.randomUUID(), name: file.name, label: '@ẢNH_KIẾN_TRÚC', note: '', mimeType: file.type, base64, size: file.size, createdAt: new Date().toISOString() });
    }
    updateDraft({ uploadedImages: [...draft.uploadedImages, ...items] });
  }

  async function generatePrompts() {
    setLoading(true);
    try {
      const response = await fetch('/api/generate-flow-json', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ shortCommand: draft.shortCommand, projectSettings: draft.projectSettings, uploadedImages: draft.uploadedImages, selectedImages: draft.selectedImages })
      });
      const result = (await response.json()) as { ok: boolean; message?: string; data?: GeneratedPrompts };
      if (!result.ok || !result.data) {
        show(result.message || 'Không tạo được prompt Flow.', 'error');
        return;
      }
      updateDraft({ generatedPrompts: result.data });
      show(result.data.parseWarning || 'Đã tạo bộ prompt Flow.', result.data.parseWarning ? 'info' : 'success');
    } catch {
      show('OpenAI lỗi hoặc backend chưa sẵn sàng. App vẫn an toàn và không crash.', 'error');
    } finally {
      setLoading(false);
    }
  }

  async function addSelectedImage(file?: File) {
    if (!/^@IMAGE_(?:[1-9]|1[01])$/.test(selectedForm.imageName)) {
      show('Tên ảnh phải đúng định dạng @IMAGE_1 đến @IMAGE_11.', 'error');
      return;
    }
    if (draft.selectedImages.some((image) => image.imageName === selectedForm.imageName)) {
      show('Tên ảnh đã tồn tại. Không được trùng @IMAGE_X.', 'error');
      return;
    }
    const item: SelectedImage = { id: crypto.randomUUID(), imageName: selectedForm.imageName as `@IMAGE_${number}`, note: selectedForm.note, createdAt: new Date().toISOString() };
    if (file) {
      item.fileName = file.name;
      item.mimeType = file.type;
      item.base64 = file.size <= maxLocalStorageImageBytes ? await fileToDataUrl(file) : undefined;
      if (!item.base64) show(`Ảnh ${file.name} quá lớn, chỉ lưu metadata.`, 'error');
    }
    updateDraft({ selectedImages: [...draft.selectedImages, item] });
    setSelectedForm({ imageName: '@IMAGE_1', note: '' });
    show(`Đã lưu ${item.imageName}.`, 'success');
  }

  async function handleClipUpload(files: FileList | null, clipName: `CLIP ${number}`) {
    const file = files?.[0];
    if (!file) return;
    const base64 = file.size <= 2_000_000 ? await fileToDataUrl(file) : undefined;
    if (!base64) show('Clip lớn nên chỉ lưu metadata trong localStorage/ZIP. Hãy đặt file thật vào thư mục clips khi chạy FFmpeg.', 'info');
    const withoutOld = draft.uploadedClips.filter((clip) => clip.clipName !== clipName);
    updateDraft({ uploadedClips: [...withoutOld, { id: crypto.randomUUID(), clipName, fileName: file.name, mimeType: file.type, base64, size: file.size, createdAt: new Date().toISOString() }] });
  }

  async function makeFfmpeg() {
    const response = await fetch('/api/postproduction/ffmpeg-command', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ clips: draft.uploadedClips, brandSettings: draft.brandSettings, ratio: '16:9' })
    });
    const result = (await response.json()) as { commands: string };
    updateDraft({ ffmpegCommands: result.commands });
    show('Đã tạo ffmpeg-commands.txt.', 'success');
  }

  async function exportZip() {
    const zip = new JSZip();
    zip.file('project-settings.json', JSON.stringify(draft.projectSettings, null, 2));
    zip.file('prompts.json', JSON.stringify(draft.generatedPrompts ?? {}, null, 2));
    zip.file('storyboard.txt', draft.generatedPrompts?.storyboard || 'Chưa có storyboard.');
    zip.file('selected-images.json', JSON.stringify(draft.selectedImages.map(({ base64, ...rest }) => rest), null, 2));
    zip.file('ffmpeg-commands.txt', draft.ffmpegCommands || buildFfmpegCommands(draft.uploadedClips, draft.brandSettings));
    const refFolder = zip.folder('uploaded-reference-images');
    draft.uploadedImages.forEach((image) => image.base64 && refFolder?.file(image.name, image.base64.split(',')[1] ?? image.base64, { base64: true }));
    const selectedFolder = zip.folder('selected-images');
    draft.selectedImages.forEach((image) => image.base64 && selectedFolder?.file(image.fileName || `${image.imageName}.png`, image.base64.split(',')[1] ?? image.base64, { base64: true }));
    const clipsFolder = zip.folder('clips');
    draft.uploadedClips.forEach((clip) => clip.base64 && clipsFolder?.file(`${clip.clipName.replace(' ', '_')}.mp4`, clip.base64.split(',')[1] ?? clip.base64, { base64: true }));
    const blob = await zip.generateAsync({ type: 'blob' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'flow-kien-truc-ai-studio-phu-cuong.zip';
    link.click();
    URL.revokeObjectURL(url);
    updateDraft({ exportedZip: true });
    show('Đã xuất ZIP dự án. Không bao gồm .env.local, API key hoặc token.', 'success');
  }

  async function checkOpenAI() {
    const response = await fetch('/api/health-openai');
    const result = (await response.json()) as { ok: boolean; message: string; model?: string };
    setHealth(result);
    show(result.message, result.ok ? 'success' : 'error');
  }

  const renderPromptBlock = (prompt: FlowImagePrompt | FlowVideoPrompt, disabled = false) => <div key={'image_name' in prompt ? prompt.image_name : prompt.clip_name} className="rounded-2xl border border-studio-line bg-slate-950/50 p-4">
    <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
      <div><h3 className="font-bold text-studio-champagne">{'image_name' in prompt ? prompt.image_name : prompt.clip_name}</h3><p className="text-sm text-slate-400">{'image_role' in prompt ? prompt.image_role : prompt.clip_role}</p></div>
      <div className="flex flex-wrap gap-2"><Button disabled={disabled} onClick={() => copy(promptText(prompt))}>Copy JSON</Button><Button disabled={disabled} variant="ghost" onClick={() => copy(promptText(prompt))}>Gửi sang extension</Button><Button variant="ghost" onClick={() => show('Đã đánh dấu đã dùng trên Flow.', 'success')}>Đánh dấu đã dùng trên Flow</Button></div>
    </div>
    {'ảnh_đưa_vào_flow' in prompt && <p className="mb-3 text-sm text-slate-300"><b>ảnh_đưa_vào_flow:</b> {prompt['ảnh_đưa_vào_flow']}</p>}
    {prompt.prompt_type === 'video_clip' && <p className="mb-3 text-sm text-slate-300"><b>Start/End:</b> {prompt.asset_references.start_frame} → {prompt.asset_references.end_frame} | <b>Camera:</b> {prompt.video_json.camera_movement}</p>}
    <pre className="max-h-96 overflow-auto rounded-xl bg-black/40 p-3 text-xs text-slate-200">{promptText(prompt)}</pre>
  </div>;

  return <main className="min-h-screen">
    <header className="border-b border-studio-line bg-black/20 px-5 py-5 backdrop-blur"><h1 className="text-2xl font-black tracking-tight text-white">Flow Kiến Trúc AI Studio — <span className="text-studio-champagne">Phú Cường Group</span></h1><p className="mt-1 text-sm text-slate-400">Studio local bán tự động: tạo prompt bằng OpenAI, copy/điền thủ công vào Google Flow. Không bypass, không token Google, không spam generate.</p></header>
    {toast && <div className={`fixed right-4 top-4 z-50 rounded-2xl border px-4 py-3 shadow-glow ${toast.type === 'error' ? 'border-red-400 bg-red-950 text-red-100' : toast.type === 'info' ? 'border-cyan-400 bg-cyan-950 text-cyan-100' : 'border-emerald-400 bg-emerald-950 text-emerald-100'}`}>{toast.message}</div>}
    <div className="grid gap-5 p-5 lg:grid-cols-[280px_1fr]">
      <aside className="rounded-3xl border border-studio-line bg-studio-panel/90 p-3 lg:sticky lg:top-5 lg:h-[calc(100vh-2.5rem)]">
        <nav className="grid gap-2">{tabs.map((tab, index) => <button key={tab} onClick={() => setActiveTab(index)} className={`rounded-2xl px-4 py-3 text-left text-sm font-bold transition ${activeTab === index ? 'bg-studio-gold text-slate-950' : 'bg-white/5 text-slate-300 hover:bg-white/10'}`}>{index + 1}. {tab}</button>)}</nav>
      </aside>
      <div className="space-y-5">
        {activeTab === 0 && <Panel title="Tổng quan" action={<Button onClick={() => window.open(draft.projectSettings.flowProjectUrl, '_blank')}>Mở Google Flow Project</Button>}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Tên dự án"><input className={inputClass} value={draft.projectSettings.projectName} onChange={(e) => updateDraft({ projectSettings: { ...draft.projectSettings, projectName: e.target.value } })} /></Field>
            <Field label="Kiểu video"><select className={inputClass} value={draft.projectSettings.videoStyle} onChange={(e) => updateDraft({ projectSettings: { ...draft.projectSettings, videoStyle: e.target.value } })}><option>Quảng cáo kiến trúc cao cấp</option><option>Real estate walkthrough</option><option>Nhà phố bê tông hiện đại</option></select></Field>
            <Field label="Mô tả ngắn"><textarea className={inputClass} value={draft.projectSettings.description} onChange={(e) => updateDraft({ projectSettings: { ...draft.projectSettings, description: e.target.value } })} /></Field>
            <div className="grid grid-cols-3 gap-3 text-center"><div className="rounded-2xl bg-white/5 p-4"><b>40s</b><p className="text-xs text-slate-400">Thời lượng</p></div><div className="rounded-2xl bg-white/5 p-4"><b>5</b><p className="text-xs text-slate-400">Số clip</p></div><div className="rounded-2xl bg-white/5 p-4"><b>8s</b><p className="text-xs text-slate-400">Mỗi clip</p></div></div>
          </div><div className="mt-5 grid gap-3 md:grid-cols-2">{checklist.map(([label, ok]) => <div key={String(label)} className="flex items-center justify-between rounded-2xl border border-studio-line bg-white/5 p-3"><span>{label}</span><Badge status={ok ? 'done' : 'pending'}>{ok ? 'done' : 'pending'}</Badge></div>)}</div>
        </Panel>}

        {activeTab === 1 && <Panel title="Ảnh tham chiếu" action={<input type="file" accept="image/*" multiple onChange={(e) => handleReferenceUpload(e.target.files)} className="text-sm" />}>
          <p className="mb-4 text-sm text-slate-400">Không nhận diện danh tính người thật trong ảnh. Nếu có người, AI chỉ mô tả đặc điểm thị giác.</p>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{draft.uploadedImages.map((image) => <div key={image.id} className="rounded-2xl border border-studio-line bg-white/5 p-3">{image.base64 ? <img src={image.base64} alt={image.name} className="mb-3 h-44 w-full rounded-xl object-cover" /> : <div className="mb-3 grid h-44 place-items-center rounded-xl bg-black/30 text-slate-500">Chỉ lưu metadata</div>}<Field label="Nhãn ảnh"><select className={inputClass} value={image.label} onChange={(e) => updateDraft({ uploadedImages: draft.uploadedImages.map((item) => item.id === image.id ? { ...item, label: e.target.value as ImageLabel } : item) })}>{IMAGE_LABELS.map((label) => <option key={label}>{label}</option>)}</select></Field><Field label="Ghi chú"><textarea className={inputClass} value={image.note} onChange={(e) => updateDraft({ uploadedImages: draft.uploadedImages.map((item) => item.id === image.id ? { ...item, note: e.target.value } : item) })} /></Field><div className="mt-3 flex items-center justify-between"><span className="text-xs text-slate-500">{image.name}</span><Button variant="danger" onClick={() => updateDraft({ uploadedImages: draft.uploadedImages.filter((item) => item.id !== image.id) })}>Xóa</Button></div></div>)}</div>
        </Panel>}

        {activeTab === 2 && <Panel title="Tạo prompt Flow" action={<Button disabled={loading} onClick={generatePrompts}>{loading ? 'Đang tạo...' : 'Tạo bộ prompt Flow'}</Button>}>
          <Field label="Lệnh ngắn"><textarea rows={5} className={inputClass} value={draft.shortCommand} onChange={(e) => updateDraft({ shortCommand: e.target.value })} /></Field>
          {draft.generatedPrompts?.parseWarning && <p className="mt-3 rounded-xl border border-orange-400/40 bg-orange-400/10 p-3 text-orange-100">{draft.generatedPrompts.parseWarning}</p>}
          {draft.generatedPrompts && <div className="mt-4"><Button variant="ghost" onClick={() => copy(draft.generatedPrompts?.rawText || '')}>Copy toàn bộ</Button><pre className="mt-3 max-h-96 overflow-auto rounded-xl bg-black/40 p-4 text-xs">{draft.generatedPrompts.rawText}</pre></div>}
        </Panel>}

        {activeTab === 3 && <Panel title="Prompt IMAGE"> <div className="grid gap-4">{draft.generatedPrompts?.imagePrompts.length ? draft.generatedPrompts.imagePrompts.map((prompt) => renderPromptBlock(prompt)) : <Badge status="missing">Chưa có IMAGE JSON</Badge>}</div></Panel>}

        {activeTab === 4 && <Panel title="Ảnh đã chọn @IMAGE_X">
          <form className="grid gap-3 md:grid-cols-[180px_1fr_220px_auto]" onSubmit={(e) => { e.preventDefault(); const file = (e.currentTarget.elements.namedItem('selectedFile') as HTMLInputElement).files?.[0]; void addSelectedImage(file); }}><input className={inputClass} value={selectedForm.imageName} onChange={(e) => setSelectedForm({ ...selectedForm, imageName: e.target.value })} /><input className={inputClass} placeholder="Ghi chú" value={selectedForm.note} onChange={(e) => setSelectedForm({ ...selectedForm, note: e.target.value })} /><input name="selectedFile" type="file" accept="image/*" className="text-sm" /><Button type="submit">Thêm ảnh</Button></form>
          <div className="mt-5 grid gap-3">{imagePairs.map(([start, end], index) => { const ready = flowClipReady(draft.selectedImages, index + 1); return <div key={start} className="flex items-center justify-between rounded-2xl border border-studio-line bg-white/5 p-3"><span>@IMAGE_{start} → @IMAGE_{end} = CLIP {index + 1}</span><Badge status={ready ? 'ready' : 'missing'}>{ready ? 'Sẵn sàng tạo video' : 'Chưa đủ ảnh start/end'}</Badge></div>; })}</div>
          <div className="mt-5 flex flex-wrap gap-2">{draft.selectedImages.map((image) => <span key={image.id} className="rounded-xl bg-emerald-400/10 px-3 py-2 text-sm text-emerald-100">{image.imageName} — {image.note}</span>)}</div>
        </Panel>}

        {activeTab === 5 && <Panel title="Prompt VIDEO"><div className="grid gap-4">{draft.generatedPrompts?.videoPrompts.length ? draft.generatedPrompts.videoPrompts.map((prompt) => { const clipNumber = Number(prompt.clip_name.replace(/\D/g, '')) || 1; return renderPromptBlock(prompt, !flowClipReady(draft.selectedImages, clipNumber)); }) : <Badge status="missing">Chưa có CLIP JSON</Badge>}</div></Panel>}

        {activeTab === 6 && <Panel title="Upload clip từ Google Flow"><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{[1, 2, 3, 4, 5].map((number) => { const clip = draft.uploadedClips.find((item) => item.clipName === `CLIP ${number}`); return <div key={number} className="rounded-2xl border border-studio-line bg-white/5 p-4"><h3 className="mb-3 font-bold">CLIP {number}</h3><input type="file" accept="video/mp4" onChange={(e) => handleClipUpload(e.target.files, `CLIP ${number}`)} />{clip?.base64 && <video className="mt-3 w-full rounded-xl" src={clip.base64} controls />}{clip && <p className="mt-2 text-xs text-slate-400">{clip.fileName}</p>}</div>; })}</div><p className="mt-4"><Badge status={draft.uploadedClips.length >= 5 ? 'done' : 'missing'}>{draft.uploadedClips.length >= 5 ? 'Đủ 5 clip' : `Thiếu ${5 - draft.uploadedClips.length} clip`}</Badge></p></Panel>}

        {activeTab === 7 && <Panel title="Hậu kỳ & thương hiệu" action={<Button onClick={makeFfmpeg}>Tạo ffmpeg-commands.txt</Button>}>
          <div className="grid gap-3 md:grid-cols-2"><Field label="Tên công ty"><input className={inputClass} value={draft.brandSettings.company} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, company: e.target.value } })} /></Field><Field label="Hotline"><input className={inputClass} value={draft.brandSettings.hotline} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, hotline: e.target.value } })} /></Field><Field label="Website"><input className={inputClass} value={draft.brandSettings.website} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, website: e.target.value } })} /></Field><Field label="Tên ngắn website"><input className={inputClass} value={draft.brandSettings.websiteDisplay} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, websiteDisplay: e.target.value } })} /></Field><Field label="Màu chữ"><select className={inputClass} value={draft.brandSettings.textColor} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, textColor: e.target.value as 'white' | 'champagne' } })}><option value="white">Trắng</option><option value="champagne">Vàng champagne</option></select></Field><Field label="Vị trí chữ"><select className={inputClass} value={draft.brandSettings.textPosition} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, textPosition: e.target.value as 'bottom-left' | 'bottom-center' } })}><option value="bottom-left">Dưới trái</option><option value="bottom-center">Giữa cuối video</option></select></Field></div>
          <div className="mt-4 flex gap-4"><label><input type="checkbox" checked={draft.brandSettings.overlayEnabled} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, overlayEnabled: e.target.checked } })} /> Overlay</label><label><input type="checkbox" checked={draft.brandSettings.endCardEnabled} onChange={(e) => updateDraft({ brandSettings: { ...draft.brandSettings, endCardEnabled: e.target.checked } })} /> End-card</label></div><pre className="mt-4 max-h-96 overflow-auto rounded-xl bg-black/40 p-4 text-xs">{draft.ffmpegCommands || buildFfmpegCommands(draft.uploadedClips, draft.brandSettings)}</pre>
        </Panel>}

        {activeTab === 8 && <Panel title="Xuất dự án" action={<Button onClick={exportZip}>Xuất ZIP</Button>}><p className="text-slate-300">ZIP gồm project-settings.json, prompts.json, storyboard.txt, selected-images.json, ffmpeg-commands.txt, uploaded-reference-images/, selected-images/, clips/. Không bao gồm .env.local, API key, token hoặc file bí mật.</p></Panel>}

        {activeTab === 9 && <Panel title="Cài đặt nâng cao" action={<Button onClick={checkOpenAI}>Kiểm tra kết nối OpenAI</Button>}>
          <div className="grid gap-4"><div className="rounded-2xl bg-white/5 p-4"><b>Trạng thái OpenAI API Key:</b> {health ? <Badge status={health.ok ? 'done' : 'error'}>{health.message}</Badge> : <Badge status="pending">Chưa kiểm tra</Badge>}<p className="mt-2 text-sm text-slate-400">Model đang dùng: {health?.model || 'OPENAI_MODEL trong .env.local (mặc định gpt-5.5)'}</p></div><Field label="URL Google Flow Project"><input className={inputClass} value={draft.projectSettings.flowProjectUrl} onChange={(e) => updateDraft({ projectSettings: { ...draft.projectSettings, flowProjectUrl: e.target.value } })} /></Field><p className="rounded-xl border border-amber-400/40 bg-amber-400/10 p-3 text-amber-100">API key chỉ được lưu trên backend qua file .env.local, không lưu trong trình duyệt.</p><Button variant="danger" onClick={() => { resetDraft(); setDraft(createDefaultDraft()); show('Đã reset project local.', 'success'); }}>Reset project local</Button></div>
        </Panel>}
      </div>
    </div>
    <footer className="border-t border-studio-line px-5 py-5 text-center text-sm text-slate-400">Phú Cường Group | Hotline: 0905263048 | phucuonggroups.com</footer>
  </main>;
}

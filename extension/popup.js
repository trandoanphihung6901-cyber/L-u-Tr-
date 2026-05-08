const promptEl = document.getElementById('prompt');
const statusEl = document.getElementById('status');
const imageNameEl = document.getElementById('imageName');

function setStatus(message) { statusEl.textContent = message; }

async function activeFlowTab() {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  return tab;
}

async function loadPrompt() {
  const data = await chrome.storage.local.get(['lastPrompt']);
  if (data.lastPrompt) promptEl.value = data.lastPrompt;
  try {
    const text = await navigator.clipboard.readText();
    if (text && !promptEl.value) promptEl.value = text;
  } catch (_) {}
}

promptEl.addEventListener('input', () => chrome.storage.local.set({ lastPrompt: promptEl.value }));
document.getElementById('copyPrompt').addEventListener('click', async () => {
  await navigator.clipboard.writeText(promptEl.value);
  await chrome.storage.local.set({ lastPrompt: promptEl.value });
  setStatus('Đã copy prompt.');
});
document.getElementById('fillPrompt').addEventListener('click', async () => {
  const tab = await activeFlowTab();
  if (!tab?.id || !tab.url?.startsWith('https://labs.google/fx/tools/flow/')) {
    setStatus('Vui lòng mở tab Google Flow project trước.');
    return;
  }
  chrome.tabs.sendMessage(tab.id, { type: 'FILL_PROMPT', prompt: promptEl.value }, (response) => setStatus(response?.message || 'Không tìm thấy ô nhập prompt. Vui lòng copy thủ công.'));
});
document.getElementById('reloadFlow').addEventListener('click', async () => {
  const tab = await activeFlowTab();
  if (tab?.id) chrome.tabs.reload(tab.id);
  setStatus('Đã reload Flow.');
});
document.getElementById('openApp').addEventListener('click', () => chrome.tabs.create({ url: 'http://localhost:3000' }));
document.getElementById('markImage').addEventListener('click', async () => {
  const imageName = imageNameEl.value.trim();
  if (!/^@IMAGE_(?:[1-9]|1[01])$/.test(imageName)) {
    setStatus('Tên ảnh phải là @IMAGE_1 đến @IMAGE_11.');
    return;
  }
  await chrome.storage.local.set({ selectedImageName: imageName });
  setStatus(`Đã đánh dấu ảnh đã chọn là ${imageName}. Hãy nhập lại trong app.`);
});
loadPrompt();

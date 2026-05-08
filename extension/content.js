function findPromptInput() {
  const textareas = Array.from(document.querySelectorAll('textarea'));
  if (textareas.length > 0) return textareas[0];
  const editables = Array.from(document.querySelectorAll('[contenteditable="true"]'));
  return editables[0] || null;
}

function detectFlowError() {
  const text = document.body.innerText || '';
  return text.includes('Failed') || text.includes('We noticed some unusual activity');
}

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (detectFlowError()) {
    sendResponse({ ok: false, message: 'Google Flow đang lỗi hoặc giới hạn tạm thời. Hãy tải lại trang Flow rồi tiếp tục.' });
    return true;
  }
  if (message.type === 'FILL_PROMPT') {
    const input = findPromptInput();
    if (!input) {
      sendResponse({ ok: false, message: 'Không tìm thấy ô nhập prompt. Vui lòng copy thủ công.' });
      return true;
    }
    if ('value' in input) {
      input.value = message.prompt || '';
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.dispatchEvent(new Event('change', { bubbles: true }));
    } else {
      input.textContent = message.prompt || '';
      input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: message.prompt || '' }));
    }
    sendResponse({ ok: true, message: 'Đã điền prompt vào Google Flow.' });
    return true;
  }
  if (message.type === 'CHECK_ERROR') {
    sendResponse({ ok: !detectFlowError(), message: detectFlowError() ? 'Google Flow đang lỗi hoặc giới hạn tạm thời. Hãy tải lại trang Flow rồi tiếp tục.' : 'Flow đang sẵn sàng.' });
    return true;
  }
  return false;
});

const status = document.getElementById('status');

document.getElementById('start').addEventListener('click', async () => {
  if (status) status.textContent = '実行中...';
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  await chrome.scripting.executeScript({
    target: { tabId: tab.id },
    files: ['scraper.js']
  });
  await chrome.scripting.executeScript({
    target: { tabId: tab.id },
    files: ['content.js']
  });
});

chrome.runtime.onMessage.addListener((msg) => {
  if (msg.action === 'complete' && status) {
    status.textContent = '完了';
  }
});

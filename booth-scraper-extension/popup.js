const progress = document.getElementById('progress');
const status = document.getElementById('status');

document.getElementById('start').addEventListener('click', async () => {
  if (progress) progress.value = 0;
  if (status) status.textContent = 'スクレイピング中...';
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  await chrome.scripting.executeScript({
    target: { tabId: tab.id },
    files: ['scraper.js'],
    world: 'MAIN'
  });
  await chrome.scripting.executeScript({
    target: { tabId: tab.id },
    files: ['content.js'],
    world: 'MAIN'
  });
});

chrome.runtime.onMessage.addListener((msg) => {
  if (msg.action === 'progress' && progress && status) {
    progress.max = msg.max;
    progress.value = msg.value;
    status.textContent = `${msg.text} (${msg.value}/${msg.max})`;
  } else if (msg.action === 'complete' && status) {
    status.textContent = '完了';
  }
});

document.getElementById('start').addEventListener('click', async () => {
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

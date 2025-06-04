chrome.action.onClicked.addListener((tab) => {
  if (tab.id) {
    chrome.tabs.sendMessage(tab.id, { action: 'start-scrape' });
  }
});

chrome.runtime.onMessage.addListener((msg, sender) => {
  if (msg.action === 'download-json') {
    const url = URL.createObjectURL(new Blob([msg.data], { type: 'application/json' }));
    chrome.downloads.download({ url, filename: 'booth_library.json', saveAs: true });
  }
});

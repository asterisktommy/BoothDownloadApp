chrome.action.onClicked.addListener((tab) => {
  if (tab.id) {
    chrome.tabs.sendMessage(tab.id, { action: 'start-scrape' }, () => {
      if (chrome.runtime.lastError) {
        chrome.notifications.create({
          type: 'basic',
          title: 'Booth Scraper Error',
          message: 'Please open https://booth.pm/library before starting.'
        });
      }
    });
  }
});

chrome.runtime.onMessage.addListener((msg, sender) => {
  if (msg.action === 'download-json') {
    const url = URL.createObjectURL(new Blob([msg.data], { type: 'application/json' }));
    chrome.downloads.download({ url, filename: 'booth_library.json', saveAs: true });
  } else if (msg.action === 'notify') {
    if (msg.type === 'complete') {
      chrome.notifications.create({
        type: 'basic',
        title: 'Booth Scraper',
        message: 'Scraping completed successfully.'
      });
    } else if (msg.type === 'error') {
      chrome.notifications.create({
        type: 'basic',
        title: 'Booth Scraper Error',
        message: msg.message || 'An error occurred during scraping.'
      });
    }
  }
});

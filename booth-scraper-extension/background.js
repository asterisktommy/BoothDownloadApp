const transparentIcon =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=';

chrome.action.onClicked.addListener((tab) => {
  if (!tab.id || !tab.url) return;

  const allowedPages = [
    'https://accounts.booth.pm/library',
    'https://booth.pm/library'
  ];

  if (allowedPages.some(p => tab.url.startsWith(p))) {
    chrome.tabs.sendMessage(tab.id, { action: 'start-scrape' }, () => {
      if (chrome.runtime.lastError) {
        chrome.notifications.create({
          type: 'basic',
          iconUrl: transparentIcon,
          title: 'Booth Scraper Error',
          message: 'Please open your Booth library page after logging in.'
        });
      }
    });
  } else {
    chrome.notifications.create({
      type: 'basic',
      iconUrl: transparentIcon,
      title: 'Booth Scraper Error',
      message: 'Please open your Booth library page after logging in.'
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
        iconUrl: transparentIcon,
        title: 'Booth Scraper',
        message: 'Scraping completed successfully.'
      });
    } else if (msg.type === 'error') {
      chrome.notifications.create({
        type: 'basic',
        iconUrl: transparentIcon,
        title: 'Booth Scraper Error',
        message: msg.message || 'An error occurred during scraping.'
      });
    }
  }
});

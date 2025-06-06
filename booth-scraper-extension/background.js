const transparentIcon =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=';

chrome.action.onClicked.addListener((tab) => {
  if (!tab.id || !tab.url) return;

  const start = () =>
    chrome.tabs.sendMessage(tab.id, { action: 'start-scrape' }, () => {
      if (chrome.runtime.lastError) {
        chrome.notifications.create({
          type: 'basic',
          iconUrl: transparentIcon,
          title: 'Booth Scraper Error',
          message: 'Could not start scraping. Ensure you are logged in and on the library page.'
        });
      }
    });

  chrome.tabs.sendMessage(tab.id, { action: 'ping' }, () => {
    if (chrome.runtime.lastError) {
      chrome.scripting.executeScript({ target: { tabId: tab.id }, files: ['content.js'] }, () => {
        if (chrome.runtime.lastError) {
          chrome.notifications.create({
            type: 'basic',
            iconUrl: transparentIcon,
            title: 'Booth Scraper Error',
            message: 'Could not inject scraper script. Ensure you are logged in.'
          });
        } else {
          start();
        }
      });
    } else {
      start();
    }
  });
});

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.action === 'fetch-html') {
    fetch(msg.url, { credentials: 'include' })
      .then(r => r.text())
      .then(text => sendResponse({ success: true, text }))
      .catch(e => sendResponse({ success: false, error: e.message }));
    return true;
  } else if (msg.action === 'download-json') {
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

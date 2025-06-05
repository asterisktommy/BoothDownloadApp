async function fetchTags(url) {
  try {
    const html = await fetch(url, { credentials: 'include' }).then(r => r.text());
    const doc = new DOMParser().parseFromString(html, 'text/html');
    return Array.from(doc.querySelectorAll('#js-item-tag-list a div'))
      .map(el => el.textContent.trim())
      .filter(t => t.length > 0);
  } catch (e) {
    console.error('Failed to fetch tags', e);
    return [];
  }
}

async function parsePage(doc) {
  const items = [];
  const cards = doc.querySelectorAll('div.mb-16.bg-white');
  for (const card of cards) {
    const productName = card.querySelector('a[target="_blank"] div.font-bold')?.textContent.trim() || '';
    const shopName = card.querySelector('a[target="_blank"] + a div.typography-14')?.textContent.trim() || '';
    const thumbnail = card.querySelector('a[target="_blank"] img')?.src || '';
    const productLink = card.querySelector('a[target="_blank"]')?.href || '';
    const downloads = [];
    card.querySelectorAll('div.mt-16.desktop\\:flex').forEach(row => {
      const fileName = row.querySelector('div.typography-14')?.textContent.trim() || '';
      const link = row.querySelector('a[href*="downloadables"]')?.href || '';
      if (fileName && link) downloads.push({ fileName, downloadLink: link });
    });
    let tags = [];
    if (productLink) {
      tags = await fetchTags(productLink);
    }
    if (productName) items.push({ productName, shopName, thumbnail, downloads, tags });
  }
  return items;
}

async function scrapeSection(path) {
  let page = 1;
  let all = [];
  while (true) {
    const url = `${path}?page=${page}`;
    let html;
    if (page === 1 && location.pathname.startsWith(path)) {
      html = document.documentElement.outerHTML;
    } else {
      html = await fetch(url, { credentials: 'include' }).then(r => r.text());
    }
    const doc = page === 1 && location.pathname.startsWith(path) ? document : new DOMParser().parseFromString(html, 'text/html');
    const items = await parsePage(doc);
    all = all.concat(items);
    const next = doc.querySelector('.pager nav ul li a[rel="next"]');
    if (!next) break;
    page++;
  }
  return all;
}

async function scrapeAll() {
  try {
    const library = await scrapeSection('/library');
    const gifts = await scrapeSection('/library/gifts');
    const data = JSON.stringify({ library, gifts }, null, 2);
    chrome.runtime.sendMessage({ action: 'download-json', data });
    chrome.runtime.sendMessage({ action: 'notify', type: 'complete' });
  } catch (e) {
    console.error('Scrape failed', e);
    chrome.runtime.sendMessage({
      action: 'notify',
      type: 'error',
      message: e.message || 'Unexpected error'
    });
  }
}

chrome.runtime.onMessage.addListener((msg) => {
  if (msg.action === 'start-scrape') {
    scrapeAll();
  }
});

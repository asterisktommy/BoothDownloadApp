(() => {
  const sleep = ms => new Promise(res => setTimeout(res, ms));
  const base = 'https://booth.pm';

  const fetchHtml = async url =>
    new Promise((resolve, reject) => {
      chrome.runtime.sendMessage({ action: 'fetch-html', url }, res => {
        if (chrome.runtime.lastError) {
          reject(chrome.runtime.lastError);
        } else if (res && res.success) {
          resolve(res.text);
        } else {
          reject(new Error(res?.error || 'failed to fetch html'));
        }
      });
    });

  const updateProgress = (text, value, max) => {
    const progress = document.getElementById('progress');
    const status = document.getElementById('status');
    if (progress && status) {
      progress.max = max;
      progress.value = value;
      status.textContent = `${text} (${value}/${max})`;
    }
  };

  const extractProducts = html => {
    const doc = new DOMParser().parseFromString(html, 'text/html');
    const productBlocks = doc.querySelectorAll('div.mb-16.bg-white');
    const result = [];

    for (const block of productBlocks) {
      const title = block.querySelector('div.font-bold')?.innerText.trim() || '';
      const itemUrl = block.querySelector("a[href*='/items/']")?.href || '';
      const imageUrl = block.querySelector('img.l-library-item-thumbnail')?.src || '';
      const shopAnchor = block.querySelector("a[href*='.booth.pm']");
      const shopName = shopAnchor?.querySelector('div.typography-14')?.innerText.trim() || '';
      const shopUrl = shopAnchor?.href || '';

      const fileBlocks = block.querySelectorAll('div.desktop\\:justify-between');
      const files = Array.from(fileBlocks).map(fileBlock => {
        const fileName = fileBlock.querySelector('div.typography-14')?.innerText.trim() || '';
        const downloadUrl = fileBlock.querySelector("a[href*='/downloadables/']")?.href || '';
        return { fileName, downloadUrl };
      });

      result.push({ title, itemUrl, imageUrl, shopName, shopUrl, files, tags: [] });
    }
    return result;
  };

  const getAllPages = async path => {
    let page = 1;
    const all = [];
    while (true) {
      const url = `${base}${path}?page=${page}`;
      let html;
      try {
        html = await fetchHtml(url);
      } catch (e) {
        console.warn('ページ取得失敗', url, e);
        break;
      }
      const products = extractProducts(html);
      if (products.length === 0) break;
      all.push(...products);
      page++;
      await sleep(300);
    }
    return all;
  };

  const getTags = async (items, concurrency = 5) => {
    let index = 0;
    const total = items.length;
    const worker = async () => {
      while (true) {
        const i = index++;
        if (i >= total) break;
        updateProgress('タグ取得中', i + 1, total);
        try {
          const url = new URL(items[i].itemUrl);
          const pathMatch = url.pathname.match(/^\/(?:([a-z]{2})\/)?items\/(\d+)/);
          if (!pathMatch) continue;
          const lang = pathMatch[1] || 'ja';
          const apiUrl = `https://booth.pm/${lang}/items/${pathMatch[2]}.json`;
          const res = await fetch(apiUrl);
          const json = await res.json();
          if (Array.isArray(json.tags)) {
            items[i].tags = json.tags
              .map(t => (t && t.name ? t.name.trim() : ''))
              .filter(t => t);
          }
        } catch (e) {
          console.warn('タグ取得失敗', items[i].itemUrl, e);
        }
      }
    };
    const workers = Array.from({ length: concurrency }, worker);
    await Promise.all(workers);
  };

  const scrapeLibrary = async () => {
    const all = [
      ...(await getAllPages('/library')),
      ...(await getAllPages('/library/gifts'))
    ];
    updateProgress('商品タグ取得準備中', 0, all.length);
    await getTags(all);
    return all;
  };

  const downloadJson = data => {
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'booth_library_export.json';
    a.click();
  };

  window.boothScraper = { scrapeLibrary, downloadJson };
})();

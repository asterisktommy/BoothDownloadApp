(() => {
  const sleep = ms => new Promise(res => setTimeout(res, ms));
  const base = location.origin;

  const updateProgress = (text, value, max) => {
    if (typeof chrome !== 'undefined' && chrome.runtime) {
      chrome.runtime.sendMessage({ action: 'progress', text, value, max });
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
      const res = await fetch(url);
      const html = await res.text();
      const products = extractProducts(html);
      if (products.length === 0) {
        updateProgress('ページ取得中', page - 1, page - 1);
        break;
      }
      all.push(...products);
      updateProgress('ページ取得中', page, page + 1);
      page++;
      await sleep(300);
    }
    updateProgress('ページ取得完了', page - 1, page - 1);
    return all;
  };

  const scrapeLibrary = async () => {
    const all = [
      ...(await getAllPages('/library')),
      ...(await getAllPages('/library/gifts'))
    ];
    updateProgress('書き出し中', 0, 1);
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

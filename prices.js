const PRICE_LOAD_ERROR_TITLE = 'Не удалось загрузить прайс';
const PRICE_LOAD_ERROR_TEXT = 'Обновите страницу или позвоните нам — озвучим цены по телефону.';

function formatPriceValue(price) {
  const value = Number(price);
  if (Number.isNaN(value)) return String(price);
  return value.toLocaleString('ru-RU');
}

function renderPriceTable(items) {
  const rows = items.map(item => `
    <tr>
      <td>${escapeHtml(item.name)}</td>
      <td>${escapeHtml(item.unit)}</td>
      <td>${formatPriceValue(item.price)}</td>
    </tr>
  `).join('');

  return `
    <table class="price-table">
      <thead><tr><th>Наименование</th><th>Ед.</th><th>Цена, ₽</th></tr></thead>
      <tbody>${rows}</tbody>
    </table>
  `;
}

function initPriceTabs() {
  const tabs = document.querySelectorAll('.price-tab');
  const panels = document.querySelectorAll('.price-panel');

  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      const target = tab.dataset.tab;

      tabs.forEach(t => t.classList.remove('active'));
      panels.forEach(p => p.classList.remove('active'));

      tab.classList.add('active');
      document.getElementById(target)?.classList.add('active');
    });
  });
}

function renderPricesFromApi(data) {
  const tabsEl = document.getElementById('priceTabs');
  const panelsEl = document.getElementById('pricePanels');
  if (!tabsEl || !panelsEl) return;

  const categories = [...data.categories].sort((a, b) => a.sortOrder - b.sortOrder);
  const panels = [];

  categories.forEach((category, index) => {
    const tabId = `price-cat-${index}`;
    const active = index === 0 ? 'active' : '';
    const items = [...category.items].sort((a, b) => a.sortOrder - b.sortOrder);
    tabsEl.insertAdjacentHTML('beforeend', `
      <button type="button" class="price-tab ${active}" data-tab="${tabId}">${escapeHtml(category.name)}</button>
    `);

    panels.push(`
      <div class="price-panel ${active}" id="${tabId}">
        ${renderPriceTable(items)}
      </div>
    `);
  });

  if (Array.isArray(data.surcharges) && data.surcharges.length) {
    const surchargeItems = data.surcharges.map(s => ({
      name: s.label,
      unit: 'от стоимости работ',
      price: `+${s.percent}%`,
    }));

    if (typeof data.visitFee === 'number') {
      surchargeItems.push({
        name: 'Выезд на объект, оценка, разметка, консультация',
        unit: '—',
        price: formatPriceValue(data.visitFee),
      });
    }

    tabsEl.insertAdjacentHTML('beforeend', `
      <button type="button" class="price-tab" data-tab="price-surcharges">Доп. условия</button>
    `);

    panels.push(`
      <div class="price-panel" id="price-surcharges">
        ${renderPriceTable(surchargeItems)}
      </div>
    `);
  }

  panelsEl.innerHTML = panels.join('');
  tabsEl.hidden = false;
  initPriceTabs();
}

async function initPrices() {
  const tabsEl = document.getElementById('priceTabs');
  const panelsEl = document.getElementById('pricePanels');
  if (!tabsEl || !panelsEl) return;

  const data = await fetchPriceCatalog();
  if (!data?.categories?.length) {
    tabsEl.hidden = true;
    showServerError(panelsEl, PRICE_LOAD_ERROR_TITLE, PRICE_LOAD_ERROR_TEXT);
    appendRetryButton(panelsEl, async () => {
      if (typeof clearPriceCatalogCache === 'function') {
        clearPriceCatalogCache();
      }

      const retryData = await fetchPriceCatalog(true);
      if (retryData?.categories?.length) {
        renderPricesFromApi(retryData);
      }
    });
    return;
  }

  renderPricesFromApi(data);
}

document.addEventListener('DOMContentLoaded', initPrices);

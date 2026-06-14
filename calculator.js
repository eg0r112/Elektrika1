const state = {
  quantities: {},
  surcharges: new Set(),
  visit: false,
  category: 'all',
  search: '',
};

let PRICE_CATALOG = [];
let PRICE_CATEGORIES = [];
let SURCHARGES = [];
let VISIT_FEE = 1000;

function formatPrice(n) {
  return n.toLocaleString('ru-RU') + ' ₽';
}

function getCategories() {
  return ['all', ...PRICE_CATEGORIES];
}

function getFilteredItems() {
  return PRICE_CATALOG.filter(item => {
    const matchCategory = state.category === 'all' || item.category === state.category;
    const q = state.search.toLowerCase().trim();
    const matchSearch = !q || item.name.toLowerCase().includes(q) || item.category.toLowerCase().includes(q);
    return matchCategory && matchSearch;
  });
}

function getLineItems() {
  return PRICE_CATALOG
    .filter(item => (state.quantities[item.id] || 0) > 0)
    .map(item => {
      const qty = state.quantities[item.id];
      const sum = qty * item.price;
      return { ...item, qty, sum };
    });
}

function calculateTotals() {
  const lines = getLineItems();
  const subtotal = lines.reduce((acc, line) => acc + line.sum, 0);

  let surchargeTotal = 0;
  const surchargeLines = [];

  SURCHARGES.forEach(s => {
    if (state.surcharges.has(s.id) && subtotal > 0) {
      const amount = Math.round(subtotal * s.percent / 100);
      surchargeTotal += amount;
      surchargeLines.push({ label: s.label, percent: s.percent, amount });
    }
  });

  const visitAmount = state.visit ? VISIT_FEE : 0;
  const total = subtotal + surchargeTotal + visitAmount;

  return { lines, subtotal, surchargeLines, surchargeTotal, visitAmount, total };
}

function setQty(id, qty) {
  const value = Math.max(0, Math.min(9999, parseInt(qty, 10) || 0));
  if (value === 0) {
    delete state.quantities[id];
  } else {
    state.quantities[id] = value;
  }
  render();
}

function renderCategoryTabs() {
  const container = document.getElementById('calcCategories');
  if (!container) return;

  container.innerHTML = getCategories().map(cat => {
    const label = cat === 'all' ? 'Все' : escapeHtml(cat);
    const active = state.category === cat ? 'active' : '';
    const catValue = escapeHtml(cat);
    return `<button type="button" class="calc-cat ${active}" data-cat="${catValue}">${label}</button>`;
  }).join('');

  container.querySelectorAll('.calc-cat').forEach(btn => {
    btn.addEventListener('click', () => {
      state.category = btn.dataset.cat;
      render();
    });
  });
}

function renderCatalog() {
  const container = document.getElementById('calcCatalog');
  if (!container) return;

  const items = getFilteredItems();

  if (!items.length) {
    container.innerHTML = '<p class="calc-empty">Ничего не найдено. Попробуйте другой запрос.</p>';
    return;
  }

  container.innerHTML = items.map(item => {
    const qty = state.quantities[item.id] || 0;
    const active = qty > 0 ? 'calc-item--active' : '';
    return `
      <div class="calc-item ${active}" data-id="${escapeHtml(item.id)}">
        <div class="calc-item__info">
          <span class="calc-item__cat">${escapeHtml(item.category)}</span>
          <span class="calc-item__name">${escapeHtml(item.name)}</span>
          <span class="calc-item__price">${formatPrice(item.price)} / ${escapeHtml(item.unit)}</span>
        </div>
        <div class="calc-item__qty">
          <button type="button" class="calc-qty-btn" data-action="minus" aria-label="Уменьшить">−</button>
          <input type="number" class="calc-qty-input" value="${qty}" min="0" max="9999" inputmode="numeric">
          <button type="button" class="calc-qty-btn" data-action="plus" aria-label="Увеличить">+</button>
        </div>
      </div>
    `;
  }).join('');

  container.querySelectorAll('.calc-item').forEach(row => {
    const id = row.dataset.id;
    const input = row.querySelector('.calc-qty-input');

    row.querySelector('[data-action="minus"]').addEventListener('click', () => {
      setQty(id, (state.quantities[id] || 0) - 1);
    });

    row.querySelector('[data-action="plus"]').addEventListener('click', () => {
      setQty(id, (state.quantities[id] || 0) + 1);
    });

    input.addEventListener('change', () => setQty(id, input.value));
    input.addEventListener('focus', () => input.select());
  });
}

function renderSummary() {
  const { lines, subtotal, surchargeLines, visitAmount, total } = calculateTotals();
  const list = document.getElementById('calcSummaryList');
  const subtotalEl = document.getElementById('calcSubtotal');
  const extrasEl = document.getElementById('calcExtras');
  const totalEl = document.getElementById('calcTotal');
  const sendBtn = document.getElementById('calcSendBtn');
  const clearBtn = document.getElementById('calcClearBtn');

  if (!list) return;

  if (!lines.length) {
    list.innerHTML = '<p class="calc-summary__empty">Добавьте позиции из списка слева — здесь появится предварительная смета</p>';
  } else {
    list.innerHTML = lines.map(line => `
      <div class="calc-summary__row">
        <div class="calc-summary__name">${escapeHtml(line.name)}</div>
        <div class="calc-summary__meta">${line.qty} ${escapeHtml(line.unit)} × ${formatPrice(line.price)}</div>
        <div class="calc-summary__sum">${formatPrice(line.sum)}</div>
      </div>
    `).join('');
  }

  subtotalEl.textContent = formatPrice(subtotal);

  let extrasHtml = '';
  surchargeLines.forEach(s => {
    extrasHtml += `<div class="calc-summary__extra"><span>${s.label} (+${s.percent}%)</span><span>${formatPrice(s.amount)}</span></div>`;
  });
  if (visitAmount) {
    extrasHtml += `<div class="calc-summary__extra"><span>Выезд на объект</span><span>${formatPrice(visitAmount)}</span></div>`;
  }
  extrasEl.innerHTML = extrasHtml;
  extrasEl.style.display = extrasHtml ? 'block' : 'none';

  totalEl.textContent = formatPrice(total);

  const hasItems = lines.length > 0;
  sendBtn.disabled = !hasItems;
  clearBtn.disabled = !hasItems && !state.visit && !state.surcharges.size;
}

function renderSurcharges() {
  const container = document.getElementById('calcSurcharges');
  if (!container) return;

  container.innerHTML = SURCHARGES.map(s => `
    <label class="calc-check">
      <input type="checkbox" name="surcharge" value="${escapeHtml(s.id)}" ${state.surcharges.has(s.id) ? 'checked' : ''}>
      <span>${escapeHtml(s.label)} <em>+${s.percent}%</em></span>
    </label>
  `).join('');

  container.querySelectorAll('input').forEach(input => {
    input.addEventListener('change', () => {
      if (input.checked) {
        state.surcharges.add(input.value);
      } else {
        state.surcharges.delete(input.value);
      }
      renderSummary();
    });
  });
}

function render() {
  renderCategoryTabs();
  renderCatalog();
  renderSummary();
}

function clearCalculator() {
  state.quantities = {};
  state.surcharges.clear();
  state.visit = false;
  document.getElementById('calcVisit').checked = false;
  clearEstimateInForm();
  renderSurcharges();
  render();
}

function buildEstimateSummary() {
  const { lines, surchargeLines, visitAmount, total } = calculateTotals();

  const items = lines.map(line =>
    `${line.name} — ${line.qty} ${line.unit} — ${line.sum.toLocaleString('ru-RU')} ₽`
  );

  surchargeLines.forEach(s => {
    items.push(`${s.label} (+${s.percent}%) — ${s.amount.toLocaleString('ru-RU')} ₽`);
  });

  if (visitAmount) {
    items.push(`Выезд на объект — ${visitAmount.toLocaleString('ru-RU')} ₽`);
  }

  items.push(`Итого: ${total.toLocaleString('ru-RU')} ₽`);

  return items;
}

function showEstimateInForm() {
  const block = document.getElementById('orderEstimate');
  const body = document.getElementById('orderEstimateBody');
  if (!block || !body) return;

  const items = buildEstimateSummary();
  if (!items.length) {
    clearEstimateInForm();
    return;
  }

  body.innerHTML = items.map(text => `<div class="order-estimate__line">${escapeHtml(text)}</div>`).join('');
  block.hidden = false;
}

function clearEstimateInForm() {
  const block = document.getElementById('orderEstimate');
  const body = document.getElementById('orderEstimateBody');
  if (body) body.innerHTML = '';
  if (block) block.hidden = true;
}

function initCalculator() {
  const search = document.getElementById('calcSearch');
  const visit = document.getElementById('calcVisit');
  const sendBtn = document.getElementById('calcSendBtn');
  const clearBtn = document.getElementById('calcClearBtn');

  if (!search) return;

  search.addEventListener('input', () => {
    state.search = search.value;
    renderCatalog();
  });

  visit.addEventListener('change', () => {
    state.visit = visit.checked;
    renderSummary();
  });

  sendBtn.addEventListener('click', () => {
    const { lines } = calculateTotals();
    if (!lines.length) return;

    showEstimateInForm();

    const form = document.getElementById('contactForm');
    document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' });
    form?.querySelector('[name="name"]')?.focus();
  });

  clearBtn.addEventListener('click', clearCalculator);

  renderSurcharges();
  render();
}

async function loadCatalogFromApi() {
  if (typeof fetchPriceCatalog !== 'function') {
    return false;
  }

  const data = await fetchPriceCatalog();
  if (!data?.categories) {
    return false;
  }

  PRICE_CATALOG = [];
  PRICE_CATEGORIES = [];

  data.categories.forEach(category => {
    PRICE_CATEGORIES.push(category.name);
    category.items.forEach(item => {
      PRICE_CATALOG.push({
        id: item.legacyKey,
        category: category.name,
        name: item.name,
        unit: item.unit,
        price: item.price,
      });
    });
  });

  if (Array.isArray(data.surcharges)) {
    SURCHARGES.length = 0;
    data.surcharges.forEach(s => {
      SURCHARGES.push({ id: s.legacyKey, label: s.label, percent: s.percent });
    });
  }

  if (typeof data.visitFee === 'number') {
    VISIT_FEE = data.visitFee;
  }

  return true;
}

function getOrderPayloadFromCalculator() {
  const lines = getLineItems();
  if (!lines.length) {
    return null;
  }

  return {
    lines: lines.map(line => ({
      legacyKey: line.id,
      quantity: line.qty,
    })),
    surchargeKeys: [...state.surcharges],
    includeVisit: state.visit,
  };
}

function showCalculatorError() {
  const layout = document.querySelector('.calc-layout');
  if (!layout) return;

  showServerError(
    layout,
    'Не удалось загрузить калькулятор',
    'Обновите страницу или позвоните нам — поможем рассчитать стоимость.'
  );

  appendRetryButton(layout, async () => {
    if (typeof clearPriceCatalogCache === 'function') {
      clearPriceCatalogCache();
    }

    const loaded = await loadCatalogFromApi();
    if (loaded) {
      initCalculator();
    } else {
      showCalculatorError();
    }
  });
}

document.addEventListener('DOMContentLoaded', async () => {
  const loaded = await loadCatalogFromApi();
  if (!loaded) {
    showCalculatorError();
    return;
  }

  initCalculator();
});

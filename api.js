function getApiBaseUrl() {
  return window.ELEKTRIKA_CONFIG?.apiBaseUrl?.replace(/\/$/, '') || '';
}

let priceCatalogCache = null;
let priceCatalogPromise = null;

function clearPriceCatalogCache() {
  priceCatalogCache = null;
  priceCatalogPromise = null;
}

async function submitOrder(payload) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 20000);

  let response;

  try {
    response = await fetch(getOrdersUrl(), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
      signal: controller.signal,
    });
  } catch (error) {
    const message = error.name === 'AbortError'
      ? 'Не удалось подтвердить отправку. Заявка могла сохраниться — позвоните нам, прежде чем отправлять снова.'
      : 'Сервер недоступен. Попробуйте позже или позвоните нам.';
    throw new Error(message);
  } finally {
    clearTimeout(timeoutId);
  }

  if (!response.ok) {
    if (response.status === 429) {
      throw new Error('Слишком много попыток. Подождите минуту или позвоните нам.');
    }

    const body = await response.json().catch(() => ({}));
    throw new Error(body.error || body.detail || 'Не удалось отправить заявку');
  }

  return response.json();
}

function getPricesUrl() {
  const baseUrl = getApiBaseUrl();
  return baseUrl ? `${baseUrl}/api/prices` : '/api/prices';
}

function getOrdersUrl() {
  const baseUrl = getApiBaseUrl();
  return baseUrl ? `${baseUrl}/api/orders` : '/api/orders';
}

async function fetchPriceCatalog(forceRefresh = false) {
  if (!forceRefresh && priceCatalogCache) {
    return priceCatalogCache;
  }

  if (!forceRefresh && priceCatalogPromise) {
    return priceCatalogPromise;
  }

  priceCatalogPromise = (async () => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 15000);

    try {
      const response = await fetch(getPricesUrl(), { signal: controller.signal });
      if (!response.ok) {
        return null;
      }

      const data = await response.json();
      if (data?.categories?.length) {
        priceCatalogCache = data;
      }
      return data;
    } catch {
      return null;
    } finally {
      clearTimeout(timeoutId);
    }
  })();

  try {
    return await priceCatalogPromise;
  } finally {
    priceCatalogPromise = null;
  }
}

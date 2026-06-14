// Mobile menu
const burger = document.getElementById('burger');
const nav = document.getElementById('nav');

function setMenuOpen(open) {
  burger.classList.toggle('active', open);
  nav.classList.toggle('open', open);
  document.body.classList.toggle('menu-open', open);
  burger.setAttribute('aria-expanded', open ? 'true' : 'false');
}

burger.setAttribute('aria-expanded', 'false');
burger.setAttribute('aria-controls', 'nav');

burger.addEventListener('click', () => {
  setMenuOpen(!nav.classList.contains('open'));
});

nav.querySelectorAll('a').forEach(link => {
  link.addEventListener('click', () => setMenuOpen(false));
});

document.addEventListener('click', (e) => {
  if (nav.classList.contains('open') && !nav.contains(e.target) && !burger.contains(e.target)) {
    setMenuOpen(false);
  }
});

// Header scroll effect
const header = document.getElementById('header');

window.addEventListener('scroll', () => {
  const currentScroll = window.scrollY;
  if (currentScroll > 100) {
    header.style.background = 'rgba(10, 14, 23, 0.95)';
  } else {
    header.style.background = 'rgba(10, 14, 23, 0.85)';
  }
});

// Contact form
const form = document.getElementById('contactForm');
const formSuccess = document.getElementById('formSuccess');
const formError = document.getElementById('formError');

const CLIENT_REQUEST_ID_KEY = 'elektrika.pendingOrderId';

function createClientRequestId() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

function getOrCreateClientRequestId() {
  let id = sessionStorage.getItem(CLIENT_REQUEST_ID_KEY);
  if (!id) {
    id = createClientRequestId();
    sessionStorage.setItem(CLIENT_REQUEST_ID_KEY, id);
  }
  return id;
}

function clearClientRequestId() {
  sessionStorage.removeItem(CLIENT_REQUEST_ID_KEY);
}

function showFormError(message) {
  if (!formError) return;

  formError.innerHTML = serverErrorHtml(
    'Извините, что-то пошло не так',
    message || 'Не удалось отправить заявку. Попробуйте ещё раз или позвоните нам.'
  );
  formError.hidden = false;
  formSuccess.hidden = true;
}

function hideFormError() {
  if (!formError) return;
  formError.hidden = true;
  formError.innerHTML = '';
}

function resetFormAfterSubmit() {
  form.reset();

  if (typeof clearEstimateInForm === 'function') {
    clearEstimateInForm();
  }

  if (typeof clearCalculator === 'function') {
    clearCalculator();
  }
}

form.addEventListener('submit', async (e) => {
  e.preventDefault();

  const submitBtn = form.querySelector('[type="submit"]');
  const submitLabel = submitBtn.textContent;
  submitBtn.disabled = true;
  submitBtn.setAttribute('aria-busy', 'true');
  submitBtn.textContent = 'Отправка…';
  hideFormError();

  const { errors, name, phone, note } = validateContactForm(form);
  showFieldErrors(form, errors);

  if (Object.keys(errors).length > 0) {
    submitBtn.disabled = false;
    submitBtn.removeAttribute('aria-busy');
    submitBtn.textContent = submitLabel;
    return;
  }

  const clientRequestId = getOrCreateClientRequestId();
  const payload = {
    clientRequestId,
    customerName: name,
    phone,
    message: note || null,
    website: form.website.value,
    lines: [],
    surchargeKeys: [],
    includeVisit: false,
  };

  if (typeof getOrderPayloadFromCalculator === 'function') {
    const calculatorPayload = getOrderPayloadFromCalculator();
    if (calculatorPayload) {
      payload.lines = calculatorPayload.lines;
      payload.surchargeKeys = calculatorPayload.surchargeKeys;
      payload.includeVisit = calculatorPayload.includeVisit;
    }
  }

  if (!payload.lines.length && !payload.message) {
    showFormError('Добавьте позиции в калькулятор или напишите комментарий.');
    submitBtn.disabled = false;
    submitBtn.removeAttribute('aria-busy');
    submitBtn.textContent = submitLabel;
    return;
  }

  if (typeof submitOrder !== 'function') {
    showFormError('Форма недоступна. Позвоните нам — мы примем заявку по телефону.');
    submitBtn.disabled = false;
    submitBtn.removeAttribute('aria-busy');
    submitBtn.textContent = submitLabel;
    return;
  }

  try {
    await submitOrder(payload);

    clearClientRequestId();
    formSuccess.textContent = 'Заявка отправлена! Перезвоним в ближайшее время.';
    formSuccess.hidden = false;
    hideFormError();
    showFieldErrors(form, {});
    resetFormAfterSubmit();

    setTimeout(() => {
      formSuccess.hidden = true;
    }, 8000);
  } catch (error) {
    showFormError(error.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.removeAttribute('aria-busy');
    submitBtn.textContent = submitLabel;
  }
});

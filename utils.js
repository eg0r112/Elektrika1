function escapeHtml(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function normalizePhone(raw) {
  const digits = String(raw ?? '').replace(/\D/g, '');

  if (digits.length === 11 && digits.startsWith('8')) {
    return '+7' + digits.slice(1);
  }

  if (digits.length === 10) {
    return '+7' + digits;
  }

  if (digits.length === 11 && digits.startsWith('7')) {
    return '+' + digits;
  }

  return null;
}

function validateContactForm(form) {
  const errors = {};
  const name = form.name.value.trim();
  const phone = normalizePhone(form.phone.value);
  const note = form.userNote.value.trim();

  if (!name || name.length < 2) {
    errors.name = 'Укажите имя (минимум 2 символа).';
  } else if (name.length > 200) {
    errors.name = 'Имя слишком длинное.';
  }

  if (!phone) {
    errors.phone = 'Укажите номер в формате +7 (900) 000-00-00.';
  }

  if (note.length > 4000) {
    errors.userNote = 'Комментарий слишком длинный.';
  }

  return { errors, name, phone, note };
}

function showFieldErrors(form, errors) {
  form.querySelectorAll('[data-field-error]').forEach(el => {
    el.hidden = true;
    el.textContent = '';
  });

  Object.entries(errors).forEach(([field, message]) => {
    const errorEl = form.querySelector(`[data-field-error="${field}"]`);
    if (errorEl) {
      errorEl.textContent = message;
      errorEl.hidden = false;
    }
  });
}

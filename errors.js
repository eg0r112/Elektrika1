function serverErrorHtml(title, text) {
  const details = text
    ? `<p class="server-error__text">${escapeHtml(text)}</p>`
    : '';

  return `
    <div class="server-error">
      <p class="server-error__title">${escapeHtml(title)}</p>
      ${details}
    </div>
  `;
}

function showServerError(container, title, text) {
  if (!container) return;
  container.innerHTML = serverErrorHtml(title, text);
}

function appendRetryButton(container, onRetry) {
  if (!container || typeof onRetry !== 'function') return;

  const btn = document.createElement('button');
  btn.type = 'button';
  btn.className = 'btn btn--primary server-error__retry';
  btn.textContent = 'Повторить';
  btn.addEventListener('click', async () => {
    btn.disabled = true;
    btn.textContent = 'Загрузка…';
    try {
      await onRetry();
    } finally {
      btn.disabled = false;
      btn.textContent = 'Повторить';
    }
  });

  container.querySelector('.server-error')?.appendChild(btn);
}

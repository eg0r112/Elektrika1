// Mobile menu
const burger = document.getElementById('burger');
const nav = document.getElementById('nav');

burger.addEventListener('click', () => {
  burger.classList.toggle('active');
  nav.classList.toggle('open');
});

nav.querySelectorAll('a').forEach(link => {
  link.addEventListener('click', () => {
    burger.classList.remove('active');
    nav.classList.remove('open');
  });
});

// Price tabs
const tabs = document.querySelectorAll('.price-tab');
const panels = document.querySelectorAll('.price-panel');

tabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const target = tab.dataset.tab;

    tabs.forEach(t => t.classList.remove('active'));
    panels.forEach(p => p.classList.remove('active'));

    tab.classList.add('active');
    document.getElementById(target).classList.add('active');
  });
});

// Header scroll effect
const header = document.getElementById('header');
let lastScroll = 0;

window.addEventListener('scroll', () => {
  const currentScroll = window.scrollY;
  if (currentScroll > 100) {
    header.style.background = 'rgba(10, 14, 23, 0.95)';
  } else {
    header.style.background = 'rgba(10, 14, 23, 0.85)';
  }
  lastScroll = currentScroll;
});

// Contact form
const form = document.getElementById('contactForm');

const formSuccess = document.getElementById('formSuccess');

form.addEventListener('submit', (e) => {
  e.preventDefault();

  formSuccess.hidden = false;
  form.reset();

  setTimeout(() => {
    formSuccess.hidden = true;
  }, 5000);
});

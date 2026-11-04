(() => {
    const root = document.documentElement;
    const button = document.getElementById('theme-toggle');
    const label = document.getElementById('theme-label');
    const preference = window.matchMedia('(prefers-color-scheme: dark)');
    let timer;
    const update = () => {
        const dark = root.dataset.theme === 'dark';
        button.setAttribute('aria-pressed', String(dark));
        button.setAttribute('aria-label', dark ? 'Activar modo claro' : 'Activar modo oscuro');
        label.textContent = dark ? 'Modo claro' : 'Modo oscuro';
    };
    const apply = theme => {
        clearTimeout(timer);
        root.classList.add('theme-changing');
        // Commit the current colors before starting the transition.
        void root.offsetWidth;
        root.dataset.theme = theme;
        update();
        timer = setTimeout(() => root.classList.remove('theme-changing'), 600);
    };
    button.addEventListener('click', () => {
        const theme = root.dataset.theme === 'dark' ? 'light' : 'dark';
        apply(theme);
        try { localStorage.setItem('sakila-theme', theme); } catch { }
    });
    preference.addEventListener('change', event => {
        let saved;
        try { saved = localStorage.getItem('sakila-theme'); } catch { }
        if (saved !== 'light' && saved !== 'dark') apply(event.matches ? 'dark' : 'light');
    });
    window.addEventListener('storage', event => {
        if (event.key === 'sakila-theme') apply(event.newValue === 'dark' || event.newValue === 'light'
            ? event.newValue : (preference.matches ? 'dark' : 'light'));
    });
    document.querySelectorAll('.navbar-nav .nav-link').forEach(link => {
        if (link.pathname.toLowerCase() === window.location.pathname.toLowerCase()) link.setAttribute('aria-current', 'page');
    });
    update();
})();

document.addEventListener('DOMContentLoaded', () => {
 const container = document.getElementById('notifications');
 if (!container) return;
 container.querySelectorAll('[data-app-toast]').forEach(element => bootstrap.Toast.getOrCreateInstance(element, { autohide: element.dataset.appToast === 'success', delay: 6500 }).show());
 document.querySelectorAll('[data-valmsg-summary]').forEach(element => element.classList.add('visually-hidden'));
 if (!window.jQuery || !jQuery.validator) return;
 jQuery('form').on('invalid-form.sakila', (_event, validator) => {
  container.querySelectorAll('.toast-error').forEach(element => { bootstrap.Toast.getInstance(element)?.dispose(); element.remove(); });
  const toast = document.createElement('div');
  toast.className = 'toast app-toast toast-error';
  toast.setAttribute('role', 'alert'); toast.setAttribute('aria-live', 'assertive'); toast.setAttribute('aria-atomic', 'true');
  const header = document.createElement('div'); header.className = 'toast-header';
  const title = document.createElement('strong'); title.className = 'me-auto'; title.textContent = 'Revise la información';
  const close = document.createElement('button'); close.type = 'button'; close.className = 'toast-close'; close.dataset.bsDismiss = 'toast'; close.setAttribute('aria-label', 'Cerrar aviso'); close.textContent = '×';
  header.append(title, close);
  const body = document.createElement('div'); body.className = 'toast-body';
  const list = document.createElement('ul'); list.className = 'mb-0 ps-3';
  [...new Set(validator.errorList.map(error => error.message))].forEach(message => { const item = document.createElement('li'); item.textContent = message; list.append(item); });
  body.append(list); toast.append(header, body); container.append(toast);
  bootstrap.Toast.getOrCreateInstance(toast, { autohide: false }).show();
 });
});

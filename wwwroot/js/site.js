document.addEventListener('DOMContentLoaded', function () {
    const btn = document.getElementById('menuBtn');
    const menu = document.getElementById('mobileMenu');
    const iconOpen = document.getElementById('iconOpen');
    const iconClose = document.getElementById('iconClose');
    if (btn && menu) {
        btn.addEventListener('click', function () {
            const hidden = menu.classList.toggle('hidden');
            iconOpen.classList.toggle('hidden', !hidden);
            iconClose.classList.toggle('hidden', hidden);
        });
    }
});

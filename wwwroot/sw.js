const CACHE = 'mediremind-v1';
const SHELL = ['/Dashboard', '/Login', '/Medications', '/css/site.css', '/js/site.js', '/offline.html'];

// Install — cache app shell
self.addEventListener('install', e => {
    e.waitUntil(caches.open(CACHE).then(c => c.addAll(SHELL)));
    self.skipWaiting();
});

// Activate — clean old caches
self.addEventListener('activate', e => {
    e.waitUntil(caches.keys().then(keys =>
        Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))
    ));
    self.clients.claim();
});

// Fetch — cache-first for static assets, network-first for pages
self.addEventListener('fetch', e => {
    const url = new URL(e.request.url);
    if (e.request.method !== 'GET') return;

    if (url.pathname.match(/\.(css|js|png|jpg|svg|ico|woff2?)$/)) {
        // Cache-first for static assets
        e.respondWith(caches.match(e.request).then(r => r || fetch(e.request)));
    } else {
        // Network-first for navigations
        e.respondWith(
            fetch(e.request)
                .then(r => { caches.open(CACHE).then(c => c.put(e.request, r.clone())); return r; })
                .catch(() => caches.match(e.request).then(r => r || caches.match('/offline.html')))
        );
    }
});

// Push notification
self.addEventListener('push', e => {
    const data = e.data?.json() ?? { title: 'MediRemind', body: 'Dose reminder', url: '/Dashboard' };
    e.waitUntil(self.registration.showNotification(data.title, {
        body: data.body,
        icon: '/icons/icon-192.png',
        badge: '/icons/icon-192.png',
        data: { url: data.url }
    }));
});

// Notification click — open the app
self.addEventListener('notificationclick', e => {
    e.notification.close();
    e.waitUntil(clients.openWindow(e.notification.data?.url ?? '/Dashboard'));
});

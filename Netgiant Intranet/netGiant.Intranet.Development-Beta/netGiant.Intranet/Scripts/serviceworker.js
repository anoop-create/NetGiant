const cacheName = 'v1';
const cacheAssets = [
    '/',
    '~Content/cssBundle.css',
    '~bundles/jqueryBundle.js'
];

// Install event
self.addEventListener('install', (e) => {
    e.waitUntil(
        caches.open(cacheName)
            .then(cache => cache.addAll(cacheAssets))
            .then(() => self.skipWaiting())
    );
});

// Activate event
self.addEventListener('activate', (e) => {
    e.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cache => {
                    if (cache !== cacheName) {
                        return caches.delete(cache);
                    }
                })
            );
        })
    );
});

// Fetch event
self.addEventListener('fetch', (e) => {
    e.respondWith(
        fetch(e.request)
            .catch(() => caches.match(e.request))
    );
});

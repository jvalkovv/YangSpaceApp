import http from 'node:http';
import { randomUUID } from 'node:crypto';

const HOST = '127.0.0.1';
const PORT = 5090;
const ADMIN_EMAIL = 'qa@example.invalid';
const ADMIN_PASSWORD = 'DisposableTestPassword123!';
const ADMIN_TOKEN = 'local-h06-2-preview-only';
const now = Date.now();
const iso = offsetDays => new Date(now + offsetDays * 86400000).toISOString();

const locations = [
  {
    id: '11111111-1111-4111-8111-111111111111',
    slug: 'bike-demo',
    name: 'Вело сервиз „Педал“',
    city: 'Варна',
    neighborhood: 'Левски',
    category: 'Услуги',
    description: 'Сервиз и поддръжка на велосипеди. Настройка, профилактика и ремонт.',
    address: 'ул. Примерна 12, Варна',
    imageUrl: null,
    isVerified: true,
    phone: '+359 888 123 456',
    website: 'https://example.test/bike',
    openingHours: 'Понеделник – Петък, 09:00 – 18:00',
    updatedAtUtc: iso(-1),
    isPublished: true,
    offers: [{
      id: 'offer-bike-1',
      title: 'Пролетна профилактика',
      description: 'Проверка и настройка на основните компоненти.',
      startsAtUtc: iso(-2),
      endsAtUtc: iso(18),
      isPublished: true
    }],
    brochures: [{
      id: 'brochure-bike-1',
      title: 'Каталог с аксесоари',
      url: 'https://example.test/catalog.pdf',
      validFromUtc: iso(-2),
      validToUtc: iso(18),
      isPublished: true
    }],
    jobs: [{
      id: 'job-bike-1',
      title: 'Веломеханик',
      description: 'Търсим човек с интерес към велосипедите.',
      applyUrl: 'https://example.test/jobs',
      expiresAtUtc: iso(25),
      isPublished: true
    }]
  },
  {
    id: '22222222-2222-4222-8222-222222222222',
    slug: 'cafe-demo',
    name: 'Кафене „Ъгълът“',
    city: 'Варна',
    neighborhood: 'Левски',
    category: 'Заведения',
    description: 'Кафе, домашни десерти и спокойно място за срещи.',
    address: 'ул. Студентска 8, Варна',
    imageUrl: null,
    isVerified: true,
    phone: '+359 888 222 333',
    website: 'https://example.test/cafe',
    openingHours: 'Всеки ден, 08:00 – 20:00',
    updatedAtUtc: iso(-2),
    isPublished: true,
    offers: [{
      id: 'offer-cafe-1',
      title: 'Кафе и десерт',
      description: 'Демо предложение за посетители.',
      startsAtUtc: iso(-1),
      endsAtUtc: iso(12),
      isPublished: true
    }],
    brochures: [],
    jobs: []
  },
  {
    id: '33333333-3333-4333-8333-333333333333',
    slug: 'pet-demo',
    name: 'Зоомагазин „Мустаци“',
    city: 'Варна',
    neighborhood: 'Чайка',
    category: 'Магазини',
    description: 'Храна, аксесоари и грижа за домашни любимци.',
    address: 'бул. Княз Борис I 40, Варна',
    imageUrl: null,
    isVerified: false,
    phone: '+359 888 333 444',
    website: null,
    openingHours: 'Понеделник – Събота, 09:00 – 19:00',
    updatedAtUtc: iso(-3),
    isPublished: true,
    offers: [],
    brochures: [],
    jobs: [{
      id: 'job-pet-1',
      title: 'Консултант в зоомагазин',
      description: 'Примерна обява за работа.',
      applyUrl: 'https://example.test/pet-jobs',
      expiresAtUtc: iso(21),
      isPublished: true
    }]
  },
  {
    id: '44444444-4444-4444-8444-444444444444',
    slug: 'market-demo',
    name: 'Магазин „При нас“',
    city: 'Варна',
    neighborhood: 'Цветен квартал',
    category: 'Хранителни магазини',
    description: 'Ежедневни продукти и подбрани местни предложения.',
    address: 'ул. Мир 24, Варна',
    imageUrl: null,
    isVerified: false,
    phone: null,
    website: 'https://example.test/market',
    openingHours: 'Всеки ден, 07:30 – 21:00',
    updatedAtUtc: iso(-4),
    isPublished: true,
    offers: [],
    brochures: [{
      id: 'brochure-market-1',
      title: 'Седмична демо брошура',
      url: 'https://example.test/weekly.pdf',
      validFromUtc: iso(-1),
      validToUtc: iso(10),
      isPublished: true
    }],
    jobs: []
  },
  {
    id: '55555555-5555-4555-8555-555555555555',
    slug: 'bakery-demo',
    name: 'Пекарна „Сутрин“',
    city: 'Варна',
    neighborhood: 'Цветен квартал',
    category: 'Пекарни',
    description: 'Прясно изпечен хляб и закуски всеки ден.',
    address: 'ул. Подвис 15, Варна',
    imageUrl: null,
    isVerified: true,
    phone: '+359 888 555 666',
    website: null,
    openingHours: 'Всеки ден, 06:30 – 18:00',
    updatedAtUtc: iso(-2),
    isPublished: true,
    offers: [],
    brochures: [],
    jobs: []
  },
  {
    id: '66666666-6666-4666-8666-666666666666',
    slug: 'studio-demo',
    name: 'Студио „Линия“',
    city: 'Варна',
    neighborhood: 'Цветен квартал',
    category: 'Красота',
    description: 'Студио за красота и персонална грижа.',
    address: 'бул. Владислав Варненчик 90, Варна',
    imageUrl: null,
    isVerified: true,
    phone: '+359 888 666 777',
    website: 'https://example.test/studio',
    openingHours: 'Понеделник – Събота, 09:00 – 19:00',
    updatedAtUtc: iso(-1),
    isPublished: true,
    offers: [{
      id: 'offer-studio-1',
      title: 'Демо пакет за грижа',
      description: 'Примерно съдържание за визуален преглед.',
      startsAtUtc: iso(-1),
      endsAtUtc: iso(15),
      isPublished: true
    }],
    brochures: [],
    jobs: []
  }
];

function publicCard(location) {
  return {
    id: location.id,
    slug: location.slug,
    name: location.name,
    neighborhood: location.neighborhood,
    category: location.category,
    description: location.description,
    address: location.address,
    imageUrl: location.imageUrl,
    isVerified: location.isVerified,
    activeOffers: location.offers.filter(item => item.isPublished && Date.parse(item.endsAtUtc) > Date.now()).length,
    activeJobs: location.jobs.filter(item => item.isPublished && Date.parse(item.expiresAtUtc) > Date.now()).length
  };
}

function publishedLocations() {
  return locations.filter(location => location.isPublished);
}

function homepagePayload() {
  const visible = publishedLocations();
  const highlights = kind => visible.flatMap(location =>
    location[kind]
      .filter(item => item.isPublished)
      .filter(item => {
        const end = kind === 'offers' ? item.endsAtUtc
          : kind === 'brochures' ? item.validToUtc : item.expiresAtUtc;
        return Date.parse(end) > Date.now();
      })
      .map(item => ({
        slug: location.slug,
        locationName: location.name,
        neighborhood: location.neighborhood,
        category: location.category,
        title: item.title,
        expiresAtUtc: kind === 'offers' ? item.endsAtUtc
          : kind === 'brochures' ? item.validToUtc : item.expiresAtUtc
      }))
  );
  return {
    meta: {
      cities: ['Варна'],
      neighborhoods: [...new Set(visible.map(item => item.neighborhood))],
      categories: [...new Set(visible.map(item => item.category))]
    },
    directory: { items: visible.map(publicCard), page: 1, pageSize: 12, total: visible.length },
    offers: highlights('offers'),
    brochures: highlights('brochures'),
    jobs: highlights('jobs')
  };
}

function sendJson(response, status, value) {
  response.writeHead(status, {
    'content-type': 'application/json; charset=utf-8',
    'cache-control': 'no-store',
    'x-content-type-options': 'nosniff'
  });
  response.end(JSON.stringify(value));
}

function readJson(request) {
  return new Promise((resolve, reject) => {
    let body = '';
    request.on('data', chunk => {
      body += chunk;
      if (body.length > 262144) {
        reject(new Error('REQUEST_BODY_TOO_LARGE'));
        request.destroy();
      }
    });
    request.on('end', () => {
      if (!body) return resolve({});
      try { resolve(JSON.parse(body)); }
      catch { reject(new Error('INVALID_JSON')); }
    });
    request.on('error', reject);
  });
}

function authorized(request) {
  return request.headers.authorization === 'Bearer ' + ADMIN_TOKEN;
}

function notFound(response) {
  sendJson(response, 404, { title: 'Not Found', status: 404 });
}

function adminLocation(location) {
  const { offers, brochures, jobs, ...base } = location;
  return base;
}

const server = http.createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://' + HOST + ':' + PORT);
  const pathname = url.pathname;

  if (request.method === 'GET' && pathname === '/health') {
    return sendJson(response, 200, { status: 'ok', mode: 'local-qa-mock' });
  }

  if (request.method === 'GET' && pathname === '/api/v1/homepage') {
    return sendJson(response, 200, homepagePayload());
  }

  if (request.method === 'GET' && pathname === '/api/v1/meta') {
    return sendJson(response, 200, homepagePayload().meta);
  }

  if (request.method === 'GET' && pathname === '/api/v1/locations') {
    const query = (url.searchParams.get('q') || '').toLocaleLowerCase('bg-BG');
    const neighborhood = url.searchParams.get('neighborhood') || '';
    const category = url.searchParams.get('category') || '';
    const page = Math.max(1, Number(url.searchParams.get('page') || 1));
    const pageSize = Math.min(50, Math.max(1, Number(url.searchParams.get('pageSize') || 12)));
    const filtered = publishedLocations().filter(item =>
      (!query || [item.name, item.description, item.category].join(' ').toLocaleLowerCase('bg-BG').includes(query)) &&
      (!neighborhood || item.neighborhood === neighborhood) &&
      (!category || item.category === category)
    );
    const start = (page - 1) * pageSize;
    return sendJson(response, 200, {
      items: filtered.slice(start, start + pageSize).map(publicCard),
      page,
      pageSize,
      total: filtered.length
    });
  }

  const publicDetail = pathname.match(/^\/api\/v1\/locations\/([^/]+)$/);
  if (request.method === 'GET' && publicDetail) {
    const slug = decodeURIComponent(publicDetail[1]);
    const location = publishedLocations().find(item => item.slug === slug);
    return location ? sendJson(response, 200, location) : notFound(response);
  }

  if (request.method === 'POST' && pathname === '/api/auth/login') {
    const input = await readJson(request).catch(() => null);
    if (!input || input.email !== ADMIN_EMAIL || input.password !== ADMIN_PASSWORD) {
      return sendJson(response, 401, { title: 'Unauthorized', status: 401 });
    }
    return sendJson(response, 200, {
      accessToken: ADMIN_TOKEN,
      tokenType: 'Bearer',
      expiresIn: 3600,
      refreshToken: 'local-preview-only'
    });
  }

  if (pathname.startsWith('/api/v1/admin/')) {
    if (!authorized(request)) {
      return sendJson(response, 401, { title: 'Unauthorized', status: 401 });
    }

    if (request.method === 'GET' && pathname === '/api/v1/admin/me') {
      return sendJson(response, 200, { email: ADMIN_EMAIL, role: 'PlatformAdmin' });
    }

    if (request.method === 'GET' && pathname === '/api/v1/admin/locations') {
      return sendJson(response, 200, locations.map(adminLocation));
    }

    if (request.method === 'POST' && pathname === '/api/v1/admin/locations') {
      const input = await readJson(request);
      if (!input.slug || locations.some(item => item.slug === input.slug)) {
        return sendJson(response, 409, { title: 'Conflict', status: 409 });
      }
      const created = {
        ...input,
        id: randomUUID(),
        city: 'Варна',
        isVerified: false,
        updatedAtUtc: new Date().toISOString(),
        offers: [],
        brochures: [],
        jobs: []
      };
      locations.push(created);
      return sendJson(response, 201, adminLocation(created));
    }

    const contentMatch = pathname.match(/^\/api\/v1\/admin\/locations\/([^/]+)\/content$/);
    if (request.method === 'GET' && contentMatch) {
      const location = locations.find(item => item.id === decodeURIComponent(contentMatch[1]));
      return location
        ? sendJson(response, 200, { offers: location.offers, brochures: location.brochures, jobs: location.jobs })
        : notFound(response);
    }

    const publicationMatch = pathname.match(/^\/api\/v1\/admin\/locations\/([^/]+)\/(offers|brochures|jobs)(?:\/([^/]+))?(?:\/(hide|restore))?$/);
    if (publicationMatch) {
      const location = locations.find(item => item.id === decodeURIComponent(publicationMatch[1]));
      if (!location) return notFound(response);
      const kind = publicationMatch[2];
      const itemId = publicationMatch[3];
      const action = publicationMatch[4];

      if (request.method === 'POST' && !itemId) {
        const input = await readJson(request);
        const item = { ...input, id: randomUUID(), isPublished: input.isPublished !== false };
        location[kind].push(item);
        return sendJson(response, 201, item);
      }

      const item = location[kind].find(value => value.id === itemId);
      if (!item) return notFound(response);

      if (request.method === 'PUT' && !action) {
        Object.assign(item, await readJson(request));
        return sendJson(response, 200, item);
      }

      if (request.method === 'POST' && action) {
        item.isPublished = action === 'restore';
        return sendJson(response, 200, item);
      }
    }

    const locationAction = pathname.match(/^\/api\/v1\/admin\/locations\/([^/]+)\/(hide|restore)$/);
    if (request.method === 'POST' && locationAction) {
      const location = locations.find(item => item.id === decodeURIComponent(locationAction[1]));
      if (!location) return notFound(response);
      location.isPublished = locationAction[2] === 'restore';
      return sendJson(response, 200, adminLocation(location));
    }

    const adminItem = pathname.match(/^\/api\/v1\/admin\/locations\/([^/]+)$/);
    if (request.method === 'PUT' && adminItem) {
      const location = locations.find(item => item.id === decodeURIComponent(adminItem[1]));
      if (!location) return notFound(response);
      Object.assign(location, await readJson(request));
      location.updatedAtUtc = new Date().toISOString();
      return sendJson(response, 200, adminLocation(location));
    }

    return notFound(response);
  }

  if (pathname.startsWith('/api/public-account/')) {
    return sendJson(response, 403, { title: 'Public account flows are disabled in local QA preview.', status: 403 });
  }

  return notFound(response);
});

server.on('clientError', (_error, socket) => {
  socket.end('HTTP/1.1 400 Bad Request\r\n\r\n');
});

server.listen(PORT, HOST, () => {
  process.stdout.write('H06_2_LOCAL_MOCK_API=READY http://' + HOST + ':' + PORT + '\n');
});

function shutdown() {
  server.close(() => process.exit(0));
}
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

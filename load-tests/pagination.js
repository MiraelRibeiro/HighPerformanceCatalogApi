import http from 'k6/http';
import { check } from 'k6';
import { Counter, Trend } from 'k6/metrics';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const offsetDuration = new Trend('catalog_offset_duration', true);
const keysetDuration = new Trend('catalog_keyset_duration', true);
const offsetRequests = new Counter('catalog_offset_requests');
const keysetRequests = new Counter('catalog_keyset_requests');

export const options = {
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  scenarios: {
    deep_offset: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 10),
      duration: __ENV.DURATION || '30s',
      exec: 'offset',
    },
    equivalent_keyset: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 10),
      duration: __ENV.DURATION || '30s',
      exec: 'keyset',
    },
  },
};

export function offset() {
  const response = http.get(`${baseUrl}/api/products?page=1800&pageSize=50`, {
    tags: { pagination: 'offset' },
  });
  offsetDuration.add(response.timings.duration);
  offsetRequests.add(1);
  check(response, { 'offset is 200': result => result.status === 200 });
}

export function keyset() {
  const response = http.get(`${baseUrl}/api/products?afterId=89950&pageSize=50`, {
    tags: { pagination: 'keyset' },
  });
  keysetDuration.add(response.timings.duration);
  keysetRequests.add(1);
  check(response, { 'keyset is 200': result => result.status === 200 });
}

import http from 'k6/http';
import { check } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  scenarios: {
    product_cache: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 30),
      duration: __ENV.DURATION || '30s',
    },
  },
};

export function setup() {
  http.get(`${baseUrl}/api/products/1`);
}

export default function () {
  const productId = (__ITER % 20) + 1;
  const response = http.get(`${baseUrl}/api/products/${productId}`, {
    tags: { endpoint: 'product-by-id' },
  });

  check(response, { 'product is 200': result => result.status === 200 });
}

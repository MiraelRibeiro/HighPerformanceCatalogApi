import http from 'k6/http';
import { check } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  scenarios: {
    catalog_reads: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 20),
      duration: __ENV.DURATION || '30s',
    },
  },
};

export default function () {
  const page = (__ITER % 20) + 1;
  const categoryId = (__ITER % 10) + 1;
  const responses = http.batch([
    ['GET', `${baseUrl}/api/products?page=${page}&pageSize=50`, null, { tags: { endpoint: 'list' } }],
    ['GET', `${baseUrl}/api/products/search?categoryId=${categoryId}&minPrice=10&maxPrice=5000&pageSize=50`, null, { tags: { endpoint: 'search' } }],
  ]);

  check(responses[0], { 'list is 200': response => response.status === 200 });
  check(responses[1], { 'search is 200': response => response.status === 200 });
}

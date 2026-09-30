import http from 'k6/http';
import { check } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  scenarios: {
    top_products: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 30),
      duration: __ENV.DURATION || '30s',
    },
  },
};

export default function () {
  const response = http.get(`${baseUrl}/api/products/top?take=20`, {
    tags: { endpoint: 'top-output-cache' },
  });

  check(response, { 'top products is 200': result => result.status === 200 });
}

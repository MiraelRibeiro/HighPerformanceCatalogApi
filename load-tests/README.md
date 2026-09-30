# Load tests

Start the stack and wait for `/health` to return HTTP 200. Run each scenario from the repository root:

```bash
k6 run load-tests/baseline.js
k6 run load-tests/cached.js
k6 run load-tests/output-cache.js
k6 run load-tests/pagination.js
```

Use `BASE_URL`, `VUS`, and `DURATION` to make an execution explicit and repeatable:

```bash
BASE_URL=http://localhost:8080 VUS=30 DURATION=60s k6 run load-tests/baseline.js
```

Record the environment, dataset size, exact command, RPS, average latency, p50, p95, p99, and error rate in `docs/PERFORMANCE-RESULTS.md`. Do not compare runs made under different conditions.

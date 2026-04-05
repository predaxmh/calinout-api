import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ── Custom metrics ────────────────────────────────────────────────────────────
// k6 tracks these automatically across all virtual users and produces
// a summary at the end. Trend tracks response time distribution (p95, p99).
// Rate tracks what percentage of requests meet your success criteria.
const errorRate = new Rate('errors');
const createFoodDuration = new Trend('create_food_duration', true);

// ── Test configuration ────────────────────────────────────────────────────────
// stages defines how virtual users ramp up and down over time.
// This is a realistic load profile — you don't go from 0 to 100 users instantly
// in production either.
export const options = {
    stages: [
        { duration: '30s', target: 10 }, // ramp up to 10 users over 30 seconds
        { duration: '1m', target: 50 }, // ramp to 50 users, hold for 1 minute
        { duration: '30s', target: 100 }, // ramp to 100 users
        { duration: '1m', target: 100 }, // hold at 100 users for 1 minute
        { duration: '30s', target: 0 }, // ramp back down to 0
    ],

    // ── Thresholds — your SLA ─────────────────────────────────────────────────
    // These are PASS/FAIL criteria. If any threshold is breached,
    // k6 exits with a non-zero code — useful in CI pipelines.
    thresholds: {
        // 95% of all requests must complete in under 500ms
        'http_req_duration': ['p(95)<500'],

        // 99% of all requests must complete in under 1000ms
        'http_req_duration': ['p(99)<1000'],

        // Error rate must stay below 1%
        'errors': ['rate<0.01'],

        // The create food endpoint specifically must be under 500ms at p95
        'create_food_duration': ['p(95)<500'],
    },
    //vus: 1,
    //iterations: 1
};

// ── Setup — runs ONCE before all virtual users start ─────────────────────────
// Use this for expensive one-time operations: get a token, seed data, etc.
// The return value is passed to the default function as `data`.
export function setup() {
    // Register a test user
    const registerRes = http.post(
        'https://localhost:7026/api/v1/auth/register',
        JSON.stringify({
            email: 'loadtest@test.com',
            password: 'Test1234!',
            confirmPassword: 'Test1234!'
        }),
        { headers: { 'Content-Type': 'application/json' } }
    );

    // Login to get a token
    const loginRes = http.post(
        'https://localhost:7026/api/v1/auth/login',
        JSON.stringify({
            email: 'loadtest@test.com',
            password: 'Test1234!'
        }),
        { headers: { 'Content-Type': 'application/json' } }
    );

    const token = JSON.parse(loginRes.body).accessToken;

    // Return the token — every virtual user receives this in their `data` param
    return { token };
}

// ── Default function — runs repeatedly for each virtual user ─────────────────
// Each virtual user calls this function in a loop for the test duration.
// Think of each iteration as one user performing one action on your app.
export default function (data) {
    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${data.token}`,
    };

    // ── Scenario 1: Create food ───────────────────────────────────────────────
    const createStart = Date.now();

    const createRes = http.post(
        'https://localhost:7026/api/v1/foods',
        JSON.stringify({
            foodTypeId: 193011,
            weightInGrams: 150,
            consumedAt: new Date().toISOString(),
            isTemplate: false
        }),
        { headers }
    );

    createFoodDuration.add(Date.now() - createStart);



    // check() returns true/false — failures are counted in errorRate
    const createOk = check(createRes, {
        'create food: status 201': (r) => r.status === 201,
        'create food: has id': (r) => JSON.parse(r.body).id > 0,
        'create food: has macros': (r) => JSON.parse(r.body).calories > 0,
    });

    if (createRes.status !== 201) {
        console.log(createRes.status);
        console.log(createRes.body);
    }
    errorRate.add(!createOk);

    // ── Scenario 2: Get foods list ────────────────────────────────────────────
    const listRes = http.get(
        'https://localhost:7026/api/v1/foods/GetUserFoods',
        { headers }
    );


    check(listRes, {
        'list foods: status 200': (r) => r.status === 200,
    });


    if (listRes.status !== 200) {
        console.log(listRes.status);
        console.log(listRes.body);
    }
    // ── Think time ────────────────────────────────────────────────────────────
    // Real users don't hammer the API continuously — they read, think, act.
    // sleep() simulates that pause. Without it your test generates
    // unrealistically high request rates that no real app would see.
    sleep(1);
}

// ── Teardown — runs ONCE after all virtual users finish ───────────────────────
// Clean up test data, log final state, etc.
export function teardown(data) {
    console.log('Load test complete');
}
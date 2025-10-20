import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const webhookDeliveryTime = new Trend('webhook_delivery_time');
const apiResponseTime = new Trend('api_response_time');
const webhooksSent = new Counter('webhooks_sent');
const webhooksDelivered = new Counter('webhooks_delivered');
const webhooksFailed = new Counter('webhooks_failed');

// Test configuration
export const options = {
  stages: [
    // Ramp-up phase
    { duration: '2m', target: 1000 },   // Ramp up to 1k req/s
    { duration: '2m', target: 5000 },   // Ramp up to 5k req/s
    { duration: '3m', target: 10000 },  // Ramp up to 10k req/s (target)
    
    // Sustained load phase
    { duration: '10m', target: 10000 }, // Stay at 10k req/s for 10 minutes
    
    // Peak load test
    { duration: '2m', target: 15000 },  // Spike to 15k req/s
    { duration: '3m', target: 15000 },  // Sustain spike
    
    // Recovery phase
    { duration: '2m', target: 10000 },  // Return to 10k req/s
    { duration: '3m', target: 5000 },   // Ramp down to 5k req/s
    { duration: '2m', target: 1000 },   // Ramp down to 1k req/s
    { duration: '1m', target: 0 },      // Graceful shutdown
  ],
  
  thresholds: {
    // 95% of requests should complete within 500ms
    'http_req_duration': ['p(95)<500'],
    
    // Error rate should be below 1%
    'errors': ['rate<0.01'],
    
    // 99% of requests should succeed
    'http_req_failed': ['rate<0.01'],
    
    // API response time
    'api_response_time': ['p(95)<200', 'p(99)<500'],
    
    // Webhook delivery time (end-to-end)
    'webhook_delivery_time': ['p(95)<2000', 'p(99)<5000'],
  },
  
  // System under test configuration
  ext: {
    loadimpact: {
      projectID: 3571919,
      name: 'HookVerse Load Test - 10K req/s'
    }
  }
};

// Base URL for API
const BASE_URL = __ENV.API_URL || 'http://localhost:5000';
const API_KEY = __ENV.API_KEY || 'test-api-key';

// Test data
const eventTypes = ['order.created', 'order.updated', 'order.cancelled', 'payment.received'];
const tenantIds = generateTenantIds(10); // 10 test tenants

function generateTenantIds(count) {
  const ids = [];
  for (let i = 0; i < count; i++) {
    ids.push(`tenant-${i + 1}`);
  }
  return ids;
}

// Setup function - runs once per VU at the start
export function setup() {
  console.log('Starting load test setup...');
  
  // Create test event types
  const headers = {
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY,
  };
  
  for (const eventType of eventTypes) {
    const payload = JSON.stringify({
      name: eventType,
      description: `Load test event type: ${eventType}`,
      version: '1.0.0',
    });
    
    const response = http.post(`${BASE_URL}/api/v1/event-types`, payload, { headers });
    
    if (response.status !== 201 && response.status !== 409) {
      console.error(`Failed to create event type ${eventType}: ${response.status}`);
    }
  }
  
  console.log('Setup complete. Starting load test...');
  
  return {
    eventTypes,
    tenantIds,
  };
}

// Main test function - runs for each VU iteration
export default function (data) {
  const headers = {
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY,
  };
  
  // Select random event type and tenant
  const eventType = data.eventTypes[Math.floor(Math.random() * data.eventTypes.length)];
  const tenantId = data.tenantIds[Math.floor(Math.random() * data.tenantIds.length)];
  
  // Create webhook payload
  const webhookPayload = {
    eventType: eventType,
    tenantId: tenantId,
    payload: {
      id: `order-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      amount: Math.floor(Math.random() * 10000) / 100,
      currency: 'USD',
      timestamp: new Date().toISOString(),
      items: [
        {
          sku: `SKU-${Math.floor(Math.random() * 1000)}`,
          quantity: Math.floor(Math.random() * 10) + 1,
          price: Math.floor(Math.random() * 10000) / 100,
        }
      ],
    },
    metadata: {
      source: 'k6-load-test',
      iteration: __ITER,
      vu: __VU,
    },
  };
  
  // Send webhook
  const startTime = Date.now();
  const response = http.post(
    `${BASE_URL}/api/v1/webhooks`,
    JSON.stringify(webhookPayload),
    { headers, tags: { name: 'SendWebhook' } }
  );
  
  const responseTime = Date.now() - startTime;
  apiResponseTime.add(responseTime);
  
  // Check response
  const success = check(response, {
    'status is 202': (r) => r.status === 202,
    'response has id': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.id !== undefined;
      } catch {
        return false;
      }
    },
    'response time < 500ms': () => responseTime < 500,
  });
  
  if (success) {
    webhooksSent.add(1);
    
    // Extract webhook event ID
    try {
      const body = JSON.parse(response.body);
      const eventId = body.id;
      
      // Optional: Check delivery status after delay
      // Note: This simulates end-to-end monitoring
      if (Math.random() < 0.01) { // Sample 1% of requests
        sleep(2); // Wait for processing
        
        const statusResponse = http.get(
          `${BASE_URL}/api/v1/webhook-events/${eventId}`,
          { headers, tags: { name: 'CheckStatus' } }
        );
        
        if (statusResponse.status === 200) {
          const statusBody = JSON.parse(statusResponse.body);
          const deliveryTime = Date.now() - startTime;
          
          webhookDeliveryTime.add(deliveryTime);
          
          if (statusBody.status === 'Delivered') {
            webhooksDelivered.add(1);
          } else if (statusBody.status === 'Failed') {
            webhooksFailed.add(1);
          }
        }
      }
    } catch (error) {
      console.error(`Error parsing response: ${error}`);
      errorRate.add(1);
    }
  } else {
    errorRate.add(1);
    webhooksFailed.add(1);
  }
  
  // Small random sleep to simulate realistic traffic pattern
  sleep(Math.random() * 0.1);
}

// Teardown function - runs once at the end
export function teardown(data) {
  console.log('Load test complete. Cleaning up...');
  
  // Optional: Generate summary report
  console.log('=== Load Test Summary ===');
  console.log(`Event types tested: ${data.eventTypes.join(', ')}`);
  console.log(`Tenants simulated: ${data.tenantIds.length}`);
  console.log('Check k6 output for detailed metrics.');
}

// Helper function for handling errors
export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: ' ', enableColors: true }),
    'summary.json': JSON.stringify(data),
    'summary.html': htmlReport(data),
  };
}

function textSummary(data, options) {
  // Generate text summary of results
  const indent = options.indent || '';
  const lines = [];
  
  lines.push(`${indent}Load Test Results:`);
  lines.push(`${indent}==================`);
  lines.push(`${indent}Duration: ${data.state.testRunDurationMs / 1000}s`);
  lines.push(`${indent}VUs: ${data.metrics.vus.values.max}`);
  lines.push(`${indent}Iterations: ${data.metrics.iterations.values.count}`);
  lines.push(`${indent}Requests: ${data.metrics.http_reqs.values.count}`);
  lines.push(`${indent}Request Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s`);
  lines.push(`${indent}Errors: ${(data.metrics.errors?.values.rate * 100 || 0).toFixed(2)}%`);
  lines.push(`${indent}P95 Response Time: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms`);
  lines.push(`${indent}P99 Response Time: ${data.metrics.http_req_duration.values['p(99)'].toFixed(2)}ms`);
  
  return lines.join('\n');
}

function htmlReport(data) {
  // Generate HTML report (simplified)
  return `<!DOCTYPE html>
<html>
<head>
  <title>HookVerse Load Test Report</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; }
    h1 { color: #333; }
    table { border-collapse: collapse; width: 100%; margin-top: 20px; }
    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
    th { background-color: #4CAF50; color: white; }
    .pass { color: green; }
    .fail { color: red; }
  </style>
</head>
<body>
  <h1>HookVerse Load Test Report</h1>
  <p>Generated: ${new Date().toISOString()}</p>
  
  <h2>Summary</h2>
  <table>
    <tr><th>Metric</th><th>Value</th></tr>
    <tr><td>Duration</td><td>${(data.state.testRunDurationMs / 1000).toFixed(2)}s</td></tr>
    <tr><td>Max VUs</td><td>${data.metrics.vus.values.max}</td></tr>
    <tr><td>Total Requests</td><td>${data.metrics.http_reqs.values.count}</td></tr>
    <tr><td>Request Rate</td><td>${data.metrics.http_reqs.values.rate.toFixed(2)} req/s</td></tr>
    <tr><td>Error Rate</td><td class="${(data.metrics.errors?.values.rate || 0) < 0.01 ? 'pass' : 'fail'}">${((data.metrics.errors?.values.rate || 0) * 100).toFixed(2)}%</td></tr>
    <tr><td>P95 Response Time</td><td>${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms</td></tr>
    <tr><td>P99 Response Time</td><td>${data.metrics.http_req_duration.values['p(99)'].toFixed(2)}ms</td></tr>
  </table>
  
  <h2>Thresholds</h2>
  <table>
    <tr><th>Threshold</th><th>Status</th></tr>
    ${Object.entries(data.thresholds || {}).map(([name, threshold]) => 
      `<tr><td>${name}</td><td class="${threshold.ok ? 'pass' : 'fail'}">${threshold.ok ? 'PASS' : 'FAIL'}</td></tr>`
    ).join('\n')}
  </table>
</body>
</html>`;
}

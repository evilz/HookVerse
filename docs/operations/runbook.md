# Operations Runbook

Operational procedures and troubleshooting guide for HookVerse webhook delivery platform.

## Table of Contents

1. [System Overview](#system-overview)
2. [Common Operations](#common-operations)
3. [Troubleshooting](#troubleshooting)
4. [Incident Response](#incident-response)
5. [Maintenance Procedures](#maintenance-procedures)
6. [Emergency Contacts](#emergency-contacts)

---

## System Overview

### Architecture Components

| Component | Purpose | Critical? | Replicas |
|-----------|---------|-----------|----------|
| API Pods | REST API endpoints | ✅ Yes | 3-5 |
| Worker Pods | Webhook delivery | ✅ Yes | 5-10 |
| PostgreSQL | Data storage | ✅ Yes | 1 primary |
| RabbitMQ | Message queue | ✅ Yes | 1-3 |
| Redis | Caching layer | ⚠️ Optional | 1 |

### Service Dependencies

```
Client → API → PostgreSQL
           ↓
       RabbitMQ → Worker → Target URLs
           ↓
        Redis (cache)
```

### Access Information

| System | URL/Endpoint | Authentication |
|--------|--------------|----------------|
| API | https://api.hookverse.example.com | API Key |
| Grafana | https://grafana.hookverse.example.com | SSO |
| Prometheus | https://prometheus.hookverse.example.com | Basic Auth |
| RabbitMQ Management | https://rabbitmq.hookverse.example.com | Basic Auth |
| Kubernetes Dashboard | https://k8s.hookverse.example.com | Bearer Token |

---

## Common Operations

### 1. Check System Health

#### Quick Health Check

```bash
# Check API health
curl https://api.hookverse.example.com/health

# Check all pods
kubectl get pods -n hookverse

# Check services
kubectl get svc -n hookverse

# Check ingress
kubectl get ingress -n hookverse
```

#### Detailed Health Check

```bash
# Pod status with resource usage
kubectl top pods -n hookverse

# Recent events
kubectl get events -n hookverse --sort-by='.lastTimestamp' | tail -20

# Check pod logs
kubectl logs -n hookverse deployment/hookverse-api --tail=50

# Database connection
kubectl exec -n hookverse deployment/hookverse-api -- \
  psql -h postgres -U hookverse -d hookverse -c "SELECT 1"
```

### 2. Scale Services

#### Manual Scaling

```bash
# Scale API pods
kubectl scale deployment hookverse-api -n hookverse --replicas=5

# Scale worker pods
kubectl scale deployment hookverse-worker -n hookverse --replicas=10

# Verify scaling
kubectl get deployments -n hookverse
```

#### Auto-scaling Configuration

```yaml
# Check HPA status
kubectl get hpa -n hookverse

# Describe HPA
kubectl describe hpa hookverse-api-hpa -n hookverse
```

### 3. Deploy New Version

#### Blue-Green Deployment

```bash
# 1. Deploy new version (green)
kubectl apply -f deployment-green.yaml

# 2. Wait for pods to be ready
kubectl wait --for=condition=ready pod \
  -l app=hookverse-api,version=green \
  -n hookverse --timeout=300s

# 3. Test new version
curl -H "Host: api-green.hookverse.local" https://api.hookverse.example.com/health

# 4. Switch traffic
kubectl patch service hookverse-api -n hookverse \
  -p '{"spec":{"selector":{"version":"green"}}}'

# 5. Monitor for issues
kubectl logs -n hookverse -l app=hookverse-api,version=green --tail=100 -f

# 6. Rollback if needed
kubectl patch service hookverse-api -n hookverse \
  -p '{"spec":{"selector":{"version":"blue"}}}'
```

#### Rolling Update

```bash
# Update image
kubectl set image deployment/hookverse-api \
  hookverse-api=hookverse/api:v2.0.0 \
  -n hookverse

# Watch rollout
kubectl rollout status deployment/hookverse-api -n hookverse

# Rollback if needed
kubectl rollout undo deployment/hookverse-api -n hookverse

# Check rollout history
kubectl rollout history deployment/hookverse-api -n hookverse
```

### 4. View Logs

#### Application Logs

```bash
# API logs (last 100 lines)
kubectl logs -n hookverse deployment/hookverse-api --tail=100

# Worker logs (last 100 lines)
kubectl logs -n hookverse deployment/hookverse-worker --tail=100

# Follow logs in real-time
kubectl logs -n hookverse deployment/hookverse-api -f

# Logs from specific pod
kubectl logs -n hookverse hookverse-api-7d4f8b9c6-abcde

# Previous pod instance (after crash)
kubectl logs -n hookverse hookverse-api-7d4f8b9c6-abcde --previous
```

#### Filtered Logs

```bash
# Error logs only
kubectl logs -n hookverse deployment/hookverse-api --tail=500 | grep ERROR

# Specific correlation ID
kubectl logs -n hookverse deployment/hookverse-api --tail=1000 | \
  grep "CorrelationId=abc-123"

# Specific time range
kubectl logs -n hookverse deployment/hookverse-api \
  --since=1h --until=30m
```

### 5. Database Operations

#### Check Database Status

```bash
# Connect to database
kubectl exec -n hookverse deployment/hookverse-api -it -- \
  psql -h postgres -U hookverse -d hookverse

# Check connections
SELECT count(*), state FROM pg_stat_activity 
GROUP BY state;

# Check database size
SELECT pg_size_pretty(pg_database_size('hookverse'));

# Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 10;
```

#### Run Migrations

```bash
# Check migration status
kubectl exec -n hookverse deployment/hookverse-api -- \
  dotnet ef migrations list

# Apply pending migrations
kubectl exec -n hookverse deployment/hookverse-api -- \
  dotnet ef database update

# Rollback to specific migration
kubectl exec -n hookverse deployment/hookverse-api -- \
  dotnet ef database update <MigrationName>
```

### 6. Cache Operations

#### Redis Operations

```bash
# Connect to Redis
kubectl exec -n hookverse deployment/redis -it -- redis-cli

# Check cache statistics
INFO stats

# Check memory usage
INFO memory

# List keys (careful in production!)
KEYS hookverse:*

# Get cached value
GET hookverse:subscription:tenant-123:order.created

# Flush all cache (use with caution!)
FLUSHALL

# Flush specific database
FLUSHDB
```

---

## Troubleshooting

### Scenario 1: High Error Rate

**Symptoms**:
- Increased 5xx errors in API
- Failed webhook deliveries
- Elevated error rate in Grafana

**Diagnosis**:

```bash
# 1. Check API pod status
kubectl get pods -n hookverse -l app=hookverse-api

# 2. Check recent errors in logs
kubectl logs -n hookverse deployment/hookverse-api --tail=500 | \
  grep -i "error\|exception\|fail"

# 3. Check pod resource usage
kubectl top pods -n hookverse -l app=hookverse-api

# 4. Check database connections
kubectl exec -n hookverse deployment/hookverse-api -- \
  psql -h postgres -U hookverse -d hookverse \
  -c "SELECT count(*) FROM pg_stat_activity WHERE state='active'"
```

**Resolution**:

1. **If pods are crashing**:
   ```bash
   # Check pod events
   kubectl describe pod <pod-name> -n hookverse
   
   # Increase resources if OOM
   kubectl patch deployment hookverse-api -n hookverse \
     -p '{"spec":{"template":{"spec":{"containers":[{"name":"api","resources":{"requests":{"memory":"1Gi"},"limits":{"memory":"2Gi"}}}]}}}}'
   ```

2. **If database is overloaded**:
   ```bash
   # Scale up API pods to distribute load
   kubectl scale deployment hookverse-api -n hookverse --replicas=7
   
   # Check slow queries
   SELECT query, mean_exec_time, calls 
   FROM pg_stat_statements 
   ORDER BY mean_exec_time DESC 
   LIMIT 10;
   ```

3. **If external target unreachable**:
   ```bash
   # Check worker logs for specific target
   kubectl logs -n hookverse deployment/hookverse-worker --tail=1000 | \
     grep "target-url.com"
   
   # Mark subscriptions as temporarily disabled if needed
   # (via API or direct database update)
   ```

### Scenario 2: Message Queue Backlog

**Symptoms**:
- Growing RabbitMQ queue depth
- Delayed webhook deliveries
- High worker CPU usage

**Diagnosis**:

```bash
# 1. Check queue depth
kubectl exec -n hookverse deployment/rabbitmq -- \
  rabbitmqctl list_queues name messages consumers

# 2. Check consumer performance
kubectl logs -n hookverse deployment/hookverse-worker --tail=100

# 3. Check worker pod count
kubectl get pods -n hookverse -l app=hookverse-worker
```

**Resolution**:

1. **Scale workers**:
   ```bash
   # Increase worker count
   kubectl scale deployment hookverse-worker -n hookverse --replicas=15
   
   # Monitor queue depth
   watch -n 5 'kubectl exec -n hookverse deployment/rabbitmq -- \
     rabbitmqctl list_queues name messages'
   ```

2. **Purge queue if needed** (data loss!):
   ```bash
   # Purge specific queue (CAUTION!)
   kubectl exec -n hookverse deployment/rabbitmq -- \
     rabbitmqctl purge_queue webhooks.pending
   ```

3. **Check for poison messages**:
   ```bash
   # Peek at dead letter queue
   kubectl exec -n hookverse deployment/rabbitmq -- \
     rabbitmqctl list_queues name messages | grep dead
   ```

### Scenario 3: Database Connection Exhaustion

**Symptoms**:
- "Connection pool exhausted" errors
- Timeout errors in API logs
- Unable to establish new connections

**Diagnosis**:

```bash
# 1. Check current connections
kubectl exec -n hookverse deployment/hookverse-api -- \
  psql -h postgres -U hookverse -d hookverse \
  -c "SELECT count(*), state FROM pg_stat_activity GROUP BY state"

# 2. Check PostgreSQL max_connections
kubectl exec -n hookverse deployment/postgres -- \
  psql -U postgres -c "SHOW max_connections"

# 3. Check application connection pool settings
kubectl get configmap -n hookverse hookverse-config -o yaml | \
  grep -i "pool"
```

**Resolution**:

1. **Increase max_connections** (if safe):
   ```bash
   # Edit PostgreSQL config
   kubectl edit configmap postgres-config -n hookverse
   
   # Set max_connections = 400
   
   # Restart PostgreSQL
   kubectl rollout restart statefulset postgres -n hookverse
   ```

2. **Optimize application pool**:
   ```bash
   # Reduce pool size per instance
   kubectl set env deployment/hookverse-api \
     -n hookverse \
     ConnectionPooling__MaxPoolSize=50
   
   # Or scale down API pods temporarily
   kubectl scale deployment hookverse-api -n hookverse --replicas=3
   ```

3. **Find connection leaks**:
   ```sql
   -- Long-running queries
   SELECT pid, usename, state, query_start, query
   FROM pg_stat_activity
   WHERE state != 'idle'
     AND query_start < NOW() - INTERVAL '5 minutes'
   ORDER BY query_start;
   
   -- Kill specific connection if needed
   SELECT pg_terminate_backend(<pid>);
   ```

### Scenario 4: High Memory Usage

**Symptoms**:
- Pods being OOM killed
- Increasing memory usage over time
- Performance degradation

**Diagnosis**:

```bash
# 1. Check memory usage
kubectl top pods -n hookverse

# 2. Check pod events for OOM
kubectl get events -n hookverse --field-selector reason=OOMKilling

# 3. Check memory limits
kubectl describe deployment hookverse-api -n hookverse | grep -A 5 resources
```

**Resolution**:

1. **Increase memory limits**:
   ```bash
   kubectl patch deployment hookverse-api -n hookverse \
     -p '{"spec":{"template":{"spec":{"containers":[{"name":"api","resources":{"limits":{"memory":"2Gi"}}}]}}}}'
   ```

2. **Check for memory leaks**:
   ```bash
   # Enable detailed GC logging
   kubectl set env deployment/hookverse-api \
     -n hookverse \
     DOTNET_EnableEventLog=true
   
   # Analyze GC behavior in logs
   kubectl logs -n hookverse deployment/hookverse-api | grep "GC"
   ```

3. **Restart affected pods**:
   ```bash
   kubectl rollout restart deployment hookverse-api -n hookverse
   ```

### Scenario 5: Failed Deployments

**Symptoms**:
- Pods stuck in CrashLoopBackOff
- ImagePullBackOff errors
- Deployment doesn't progress

**Diagnosis**:

```bash
# 1. Check deployment status
kubectl rollout status deployment/hookverse-api -n hookverse

# 2. Check pod status
kubectl get pods -n hookverse -l app=hookverse-api

# 3. Describe problematic pod
kubectl describe pod <pod-name> -n hookverse

# 4. Check logs
kubectl logs <pod-name> -n hookverse
```

**Resolution**:

1. **Image pull issues**:
   ```bash
   # Check image exists
   docker pull hookverse/api:v2.0.0
   
   # Check image pull secret
   kubectl get secret -n hookverse regcred -o yaml
   
   # Recreate if needed
   kubectl create secret docker-registry regcred \
     --docker-server=<registry> \
     --docker-username=<username> \
     --docker-password=<password> \
     -n hookverse
   ```

2. **Application startup failures**:
   ```bash
   # Check configuration
   kubectl get configmap hookverse-config -n hookverse -o yaml
   
   # Check secrets
   kubectl get secrets -n hookverse
   
   # Verify environment variables
   kubectl describe deployment hookverse-api -n hookverse | grep -A 10 Environment
   ```

3. **Rollback deployment**:
   ```bash
   kubectl rollout undo deployment/hookverse-api -n hookverse
   
   # Or rollback to specific revision
   kubectl rollout undo deployment/hookverse-api -n hookverse --to-revision=3
   ```

---

## Incident Response

### Severity Levels

| Level | Definition | Response Time | Example |
|-------|------------|---------------|---------|
| P1 | Service Down | 15 minutes | Complete API outage |
| P2 | Service Degraded | 1 hour | High error rate (>5%) |
| P3 | Non-Critical Issue | 4 hours | Single feature broken |
| P4 | Minor Issue | 24 hours | UI glitch, typo |

### Incident Response Process

#### 1. Detection

**Automated**:
- Prometheus alerts
- Health check failures
- Synthetic monitoring

**Manual**:
- User reports
- Internal testing
- Monitoring dashboards

#### 2. Triage

```bash
# Quick assessment checklist
1. Verify issue is real (not false positive)
2. Determine severity level
3. Check affected components
4. Estimate customer impact
5. Assign incident commander
```

#### 3. Response

**P1 - Service Down**:
```bash
# Immediate actions (first 15 minutes)
1. Page on-call engineer
2. Create incident channel (#incident-YYYY-MM-DD)
3. Start incident log
4. Check system status:
   kubectl get pods -n hookverse
   kubectl get svc -n hookverse
   kubectl get pvc -n hookverse

# If database down
5. Check database:
   kubectl logs -n hookverse statefulset/postgres --tail=100
   kubectl describe statefulset postgres -n hookverse

# If API down
6. Check API pods:
   kubectl logs -n hookverse deployment/hookverse-api --tail=100
   kubectl describe deployment hookverse-api -n hookverse

# Rollback if recent deployment
7. kubectl rollout undo deployment/hookverse-api -n hookverse
```

**P2 - Service Degraded**:
```bash
# Within 1 hour
1. Gather metrics from Grafana
2. Check recent deployments/changes
3. Review error logs
4. Scale resources if needed
5. Identify root cause
6. Implement fix or workaround
```

#### 4. Communication

**Status Page Update**:
```bash
# Update status page
curl -X POST https://status.hookverse.com/api/incidents \
  -H "Authorization: Bearer $STATUS_API_KEY" \
  -d '{
    "title": "Increased API latency",
    "status": "investigating",
    "message": "We are investigating increased API response times."
  }'
```

**Customer Communication**:
- Initial notification: Within 30 minutes of detection
- Updates: Every 1 hour for P1, every 4 hours for P2
- Resolution notification: Within 1 hour of resolution

#### 5. Resolution

```bash
# Verify fix
1. Check metrics returning to normal
2. Test affected functionality
3. Monitor for 30 minutes
4. Update status page to resolved
5. Notify customers
```

#### 6. Post-Mortem

Within 48 hours of resolution:

1. Document timeline
2. Identify root cause
3. List contributing factors
4. Action items to prevent recurrence
5. Share learnings with team

---

## Maintenance Procedures

### 1. Database Maintenance

#### Vacuum and Analyze

```bash
# Schedule during low-traffic hours

# Connect to database
kubectl exec -n hookverse deployment/hookverse-api -it -- \
  psql -h postgres -U hookverse -d hookverse

# Vacuum analyze all tables
VACUUM ANALYZE;

# Vacuum specific table
VACUUM ANALYZE "WebhookEvents";

# Full vacuum (locks table)
VACUUM FULL "WebhookDeliveryAttempts";
```

#### Reindex

```bash
# Reindex specific table
REINDEX TABLE "Subscriptions";

# Reindex database
REINDEX DATABASE hookverse;
```

#### Update Statistics

```bash
# Update query planner statistics
ANALYZE;

# For specific table
ANALYZE "WebhookEvents";
```

### 2. Certificate Rotation

```bash
# Check certificate expiry
kubectl get certificate -n hookverse

# Describe certificate
kubectl describe certificate hookverse-tls -n hookverse

# Force renewal (cert-manager)
kubectl delete certificate hookverse-tls -n hookverse
kubectl apply -f certificate.yaml
```

### 3. Secret Rotation

```bash
# Rotate database password
1. Generate new password
2. Update secret:
   kubectl create secret generic postgres-secret \
     --from-literal=password=<new-password> \
     --dry-run=client -o yaml | kubectl apply -f -

3. Restart pods:
   kubectl rollout restart deployment hookverse-api -n hookverse
   kubectl rollout restart deployment hookverse-worker -n hookverse

4. Update database password:
   ALTER USER hookverse WITH PASSWORD '<new-password>';
```

### 4. Node Maintenance

```bash
# Drain node for maintenance
kubectl drain <node-name> \
  --ignore-daemonsets \
  --delete-emptydir-data \
  --force

# Perform maintenance (OS updates, etc.)

# Uncordon node
kubectl uncordon <node-name>

# Verify pods redistributed
kubectl get pods -n hookverse -o wide
```

### 5. Backup Verification

```bash
# Run monthly restore test
.\.specify\scripts\powershell\validate-backup-restore.ps1

# Verify backup age
find /var/backups/hookverse -name "*.sql.gz" -mtime -1 | wc -l

# Test backup download from cloud
aws s3 cp s3://hookverse-backups/latest.sql.gz /tmp/test-restore.sql.gz
```

---

## Emergency Contacts

### On-Call Rotation

| Role | Primary | Secondary | Contact |
|------|---------|-----------|---------|
| Engineering | John Doe | Jane Smith | +1-555-0100 |
| DevOps | Bob Johnson | Alice Brown | +1-555-0200 |
| Database | Charlie Davis | Eve Wilson | +1-555-0300 |
| Manager | Frank Moore | Grace Lee | +1-555-0400 |

### Escalation Path

```
Level 1: On-call Engineer (15 min response)
   ↓
Level 2: Team Lead (30 min response)
   ↓
Level 3: Engineering Manager (1 hour response)
   ↓
Level 4: CTO (2 hour response)
```

### External Contacts

| Service | Contact | Purpose |
|---------|---------|---------|
| Cloud Provider | support@cloud.com | Infrastructure issues |
| DNS Provider | support@dns.com | DNS issues |
| CDN Provider | support@cdn.com | CDN issues |
| Certificate Authority | support@ca.com | SSL certificate issues |

---

## Appendix

### Useful Commands Cheat Sheet

```bash
# Pod operations
kubectl get pods -n hookverse
kubectl describe pod <pod-name> -n hookverse
kubectl logs <pod-name> -n hookverse
kubectl exec -it <pod-name> -n hookverse -- /bin/bash

# Deployment operations
kubectl get deployments -n hookverse
kubectl scale deployment <name> -n hookverse --replicas=<count>
kubectl rollout status deployment/<name> -n hookverse
kubectl rollout undo deployment/<name> -n hookverse

# Service operations
kubectl get svc -n hookverse
kubectl describe svc <service-name> -n hookverse
kubectl port-forward svc/<service-name> -n hookverse 8080:80

# ConfigMap and Secret operations
kubectl get configmap -n hookverse
kubectl get secrets -n hookverse
kubectl describe configmap <name> -n hookverse

# Resource usage
kubectl top nodes
kubectl top pods -n hookverse

# Events
kubectl get events -n hookverse --sort-by='.lastTimestamp'

# Logs
kubectl logs -n hookverse deployment/<name> --tail=100
kubectl logs -n hookverse deployment/<name> -f
kubectl logs -n hookverse <pod-name> --previous
```

### Monitoring Dashboards

| Dashboard | URL | Purpose |
|-----------|-----|---------|
| System Overview | /d/hookverse-overview | High-level metrics |
| API Performance | /d/hookverse-api | API-specific metrics |
| Worker Performance | /d/hookverse-worker | Worker-specific metrics |
| Database | /d/hookverse-db | Database metrics |
| Message Queue | /d/hookverse-mq | RabbitMQ metrics |

### Alert Runbooks

| Alert | Runbook |
|-------|---------|
| HighErrorRate | [Link to detailed runbook] |
| DatabaseConnectionPoolExhausted | [Link to detailed runbook] |
| MessageQueueBacklog | [Link to detailed runbook] |
| PodCrashLooping | [Link to detailed runbook] |
| HighMemoryUsage | [Link to detailed runbook] |

---

**Last Updated**: 2025-10-20  
**Version**: 1.0  
**Maintained By**: DevOps Team

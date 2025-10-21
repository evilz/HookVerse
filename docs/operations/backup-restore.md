# Database Backup and Restore

Comprehensive backup and restore procedures for HookVerse PostgreSQL database.

## Overview

Database backups are critical for:

- **Disaster recovery**: Restore after hardware failures or data corruption
- **Compliance**: Meet data retention and recovery requirements  
- **Migration**: Move data between environments
- **Testing**: Create staging/development databases from production
- **Auditing**: Maintain historical snapshots for compliance

## Backup Strategy

### Backup Types

#### 1. Logical Backups (pg_dump)

**Use for**:
- Small to medium databases (< 100GB)
- Schema and data portability
- Selective table backups
- Cross-version migrations

**Pros**:
- Platform-independent
- Easy to restore specific tables
- Human-readable SQL format

**Cons**:
- Slower for large databases
- Requires more storage
- No point-in-time recovery

#### 2. Physical Backups (pg_basebackup)

**Use for**:
- Large databases (> 100GB)
- Point-in-time recovery
- Fast backup and restore
- Streaming replication setup

**Pros**:
- Faster backup/restore
- Enables PITR
- Binary-level backup

**Cons**:
- Same PostgreSQL version required
- All-or-nothing restore
- More complex setup

### Recommended Schedule

| Backup Type | Frequency | Retention | Purpose |
|-------------|-----------|-----------|---------|
| Full logical | Daily (2 AM) | 7 days | Daily snapshots |
| Full physical | Daily (3 AM) | 7 days | Fast restore |
| Incremental | Hourly | 24 hours | Point-in-time recovery |
| Weekly archive | Weekly (Sunday) | 4 weeks | Long-term retention |
| Monthly archive | Monthly (1st) | 12 months | Compliance/audit |

## Creating Backups

### Manual Logical Backup

#### Basic pg_dump

```bash
# Full database backup
pg_dump -h localhost -p 5432 -U hookverse -d hookverse \
  -F p -f hookverse-backup-$(date +%Y%m%d-%H%M%S).sql

# Compressed backup
pg_dump -h localhost -p 5432 -U hookverse -d hookverse \
  -F c -f hookverse-backup-$(date +%Y%m%d-%H%M%S).dump

# Plain SQL with compression
pg_dump -h localhost -p 5432 -U hookverse -d hookverse \
  -F p | gzip > hookverse-backup-$(date +%Y%m%d-%H%M%S).sql.gz
```

#### Selective Backup

```bash
# Schema only
pg_dump -h localhost -p 5432 -U hookverse -d hookverse \
  -F p -s -f hookverse-schema-only.sql

# Specific tables
pg_dump -h localhost -p 5432 -U hookverse -d hookverse \
  -t Subscriptions -t WebhookEvents \
  -f hookverse-webhooks-only.sql

# Exclude tables
pg_dump -h localhost -p 5432 -U hookverse -d hookverse \
  -T WebhookDeliveryAttempts \
  -f hookverse-without-attempts.sql
```

### Automated Backup Script

PowerShell script for Windows:

```powershell
# backup-database.ps1
param(
    [string]$Host = "localhost",
    [string]$Database = "hookverse",
    [string]$User = "hookverse",
    [string]$BackupDir = "C:\backups\hookverse"
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFile = Join-Path $BackupDir "hookverse-$timestamp.sql"

# Create backup directory if it doesn't exist
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
}

# Create backup
pg_dump -h $Host -U $User -d $Database -F p -f $backupFile

# Compress
gzip $backupFile

# Delete backups older than 7 days
Get-ChildItem $BackupDir -Filter "*.sql.gz" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    Remove-Item

Write-Host "Backup completed: $backupFile.gz"
```

Bash script for Linux:

```bash
#!/bin/bash
# backup-database.sh

BACKUP_DIR="/var/backups/hookverse"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="$BACKUP_DIR/hookverse-$TIMESTAMP.sql"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create backup
pg_dump -h localhost -U hookverse -d hookverse -F p -f "$BACKUP_FILE"

# Compress
gzip "$BACKUP_FILE"

# Delete old backups (keep last 7 days)
find $BACKUP_DIR -name "*.sql.gz" -mtime +7 -delete

echo "Backup completed: $BACKUP_FILE.gz"
```

### Kubernetes CronJob

Deploy automated backups in Kubernetes:

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: hookverse-db-backup
  namespace: hookverse
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  successfulJobsHistoryLimit: 3
  failedJobsHistoryLimit: 1
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: postgres:15
            env:
            - name: PGHOST
              value: "postgres.hookverse.svc.cluster.local"
            - name: PGDATABASE
              value: "hookverse"
            - name: PGUSER
              value: "hookverse"
            - name: PGPASSWORD
              valueFrom:
                secretKeyRef:
                  name: postgres-secret
                  key: password
            command:
            - /bin/bash
            - -c
            - |
              TIMESTAMP=$(date +%Y%m%d-%H%M%S)
              BACKUP_FILE="/backups/hookverse-$TIMESTAMP.sql"
              
              # Create backup
              pg_dump -F p -f "$BACKUP_FILE"
              
              # Compress
              gzip "$BACKUP_FILE"
              
              # Upload to cloud storage (example: AWS S3)
              # aws s3 cp "$BACKUP_FILE.gz" s3://hookverse-backups/
              
              # Delete local file after upload
              # rm "$BACKUP_FILE.gz"
              
              echo "Backup completed: $BACKUP_FILE.gz"
            volumeMounts:
            - name: backup-storage
              mountPath: /backups
          restartPolicy: OnFailure
          volumes:
          - name: backup-storage
            persistentVolumeClaim:
              claimName: backup-pvc
```

## Restoring Backups

### Restore to Same Database

```bash
# Drop existing database (CAUTION!)
dropdb -h localhost -U hookverse hookverse

# Create new database
createdb -h localhost -U hookverse hookverse

# Restore from backup
psql -h localhost -U hookverse -d hookverse -f hookverse-backup.sql

# Or restore compressed backup
gunzip -c hookverse-backup.sql.gz | psql -h localhost -U hookverse -d hookverse
```

### Restore to Different Database

```bash
# Create test database
createdb -h localhost -U hookverse hookverse_restore_test

# Restore backup
psql -h localhost -U hookverse -d hookverse_restore_test -f hookverse-backup.sql

# Verify data
psql -h localhost -U hookverse -d hookverse_restore_test -c "SELECT COUNT(*) FROM \"Subscriptions\""

# Drop when done
dropdb -h localhost -U hookverse hookverse_restore_test
```

### Restore Specific Tables

```bash
# Extract specific table from backup
pg_restore -h localhost -U hookverse -d hookverse \
  -t Subscriptions \
  hookverse-backup.dump

# Or with SQL backup
sed -n '/CREATE TABLE.*Subscriptions/,/COPY.*Subscriptions/p' hookverse-backup.sql | \
  psql -h localhost -U hookverse -d hookverse
```

## Point-in-Time Recovery (PITR)

### Enable WAL Archiving

Configure PostgreSQL for PITR in `postgresql.conf`:

```ini
# Enable WAL archiving
wal_level = replica
archive_mode = on
archive_command = 'cp %p /var/lib/postgresql/wal_archive/%f'

# WAL configuration
max_wal_senders = 3
wal_keep_size = 1GB
```

### Create Base Backup

```bash
# Create base backup with WAL
pg_basebackup -h localhost -U hookverse -D /backups/base \
  -Ft -z -P -X stream

# Backup includes:
# - base.tar.gz: Database files
# - pg_wal.tar.gz: WAL files
```

### Restore to Specific Time

```bash
# Extract base backup
cd /var/lib/postgresql/data
tar -xzf /backups/base/base.tar.gz

# Create recovery.conf
cat > recovery.conf <<EOF
restore_command = 'cp /var/lib/postgresql/wal_archive/%f %p'
recovery_target_time = '2025-10-20 14:30:00'
EOF

# Start PostgreSQL
# It will replay WAL files until target time
systemctl start postgresql
```

## Validation

### Automated Validation

Run validation script:

```powershell
.\.specify\scripts\powershell\validate-backup-restore.ps1 -DatabasePassword "your_password"
```

### Manual Validation Checklist

After restore, verify:

- [ ] All tables present
- [ ] Row counts match
- [ ] Indexes exist
- [ ] Constraints intact
- [ ] Sequences correct
- [ ] Permissions set
- [ ] Extensions loaded

### Validation Queries

```sql
-- Check table counts
SELECT 
    schemaname,
    tablename,
    n_live_tup as row_count
FROM pg_stat_user_tables
ORDER BY schemaname, tablename;

-- Check database size
SELECT pg_size_pretty(pg_database_size('hookverse'));

-- Check indexes
SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- Check constraints
SELECT 
    conname as constraint_name,
    contype as constraint_type,
    conrelid::regclass as table_name
FROM pg_constraint
WHERE connamespace = 'public'::regnamespace
ORDER BY table_name, constraint_name;

-- Check sequences
SELECT 
    schemaname,
    sequencename,
    last_value
FROM pg_sequences
WHERE schemaname = 'public';
```

## Backup Storage

### Local Storage

```bash
# Create backup directory
sudo mkdir -p /var/backups/hookverse
sudo chown postgres:postgres /var/backups/hookverse

# Set retention policy
find /var/backups/hookverse -name "*.sql.gz" -mtime +30 -delete
```

### Cloud Storage

#### AWS S3

```bash
# Install AWS CLI
pip install awscli

# Configure credentials
aws configure

# Upload backup
aws s3 cp hookverse-backup.sql.gz s3://hookverse-backups/$(date +%Y/%m/%d)/

# Set lifecycle policy
aws s3api put-bucket-lifecycle-configuration \
  --bucket hookverse-backups \
  --lifecycle-configuration file://lifecycle.json
```

`lifecycle.json`:
```json
{
  "Rules": [
    {
      "Id": "DeleteOldBackups",
      "Status": "Enabled",
      "Expiration": {
        "Days": 90
      },
      "Transitions": [
        {
          "Days": 30,
          "StorageClass": "GLACIER"
        }
      ]
    }
  ]
}
```

#### Azure Blob Storage

```bash
# Install Azure CLI
pip install azure-cli

# Upload backup
az storage blob upload \
  --account-name hookversebackups \
  --container-name backups \
  --name "$(date +%Y/%m/%d)/hookverse-backup.sql.gz" \
  --file hookverse-backup.sql.gz
```

## Disaster Recovery

### Recovery Time Objective (RTO)

Target time to restore service:

- **Critical**: 1 hour (use physical backup + PITR)
- **Standard**: 4 hours (use logical backup)
- **Non-critical**: 24 hours

### Recovery Point Objective (RPO)

Maximum acceptable data loss:

- **Critical**: 5 minutes (continuous WAL archiving)
- **Standard**: 1 hour (hourly incremental backups)
- **Non-critical**: 24 hours (daily backups)

### Disaster Recovery Steps

1. **Assess situation**
   - Determine cause of failure
   - Estimate data loss
   - Identify last good backup

2. **Prepare recovery environment**
   - Provision new database server if needed
   - Ensure network connectivity
   - Verify backup accessibility

3. **Restore database**
   - Use most recent backup
   - Apply WAL files if using PITR
   - Validate data integrity

4. **Reconnect applications**
   - Update connection strings
   - Test application connectivity
   - Verify functionality

5. **Monitor and verify**
   - Check application logs
   - Monitor database performance
   - Validate business operations

## Testing Backups

### Monthly Restore Test

Schedule monthly backup restore tests:

```bash
#!/bin/bash
# monthly-restore-test.sh

# Get latest backup
LATEST_BACKUP=$(ls -t /var/backups/hookverse/*.sql.gz | head -1)

# Create test database
createdb hookverse_test

# Restore
gunzip -c $LATEST_BACKUP | psql -d hookverse_test

# Run validation queries
psql -d hookverse_test -c "SELECT COUNT(*) FROM \"Subscriptions\""
psql -d hookverse_test -c "SELECT COUNT(*) FROM \"WebhookEvents\""

# Cleanup
dropdb hookverse_test

echo "Restore test completed successfully"
```

### Automated Testing in CI/CD

```yaml
name: Backup Validation

on:
  schedule:
    - cron: '0 3 1 * *'  # Monthly on 1st at 3 AM

jobs:
  validate-backup:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup PostgreSQL
        uses: ikalnytskyi/action-setup-postgres@v4
      
      - name: Run backup validation
        run: |
          pwsh ./.specify/scripts/powershell/validate-backup-restore.ps1
```

## Monitoring

### Backup Success Metrics

Track in Grafana:

```promql
# Backup age (hours)
(time() - hookverse_last_backup_timestamp) / 3600

# Backup size trend
hookverse_backup_size_bytes

# Backup duration
hookverse_backup_duration_seconds

# Failed backups
rate(hookverse_backup_failures_total[24h])
```

### Alerts

```yaml
groups:
- name: backups
  rules:
  - alert: BackupTooOld
    expr: (time() - hookverse_last_backup_timestamp) > 172800  # 48 hours
    annotations:
      summary: "Database backup is more than 48 hours old"
      
  - alert: BackupFailed
    expr: hookverse_last_backup_status != 0
    annotations:
      summary: "Last database backup failed"
```

## Best Practices

### 1. Test Restores Regularly

- Monthly restore tests
- Annual disaster recovery drills
- Document restore procedures

### 2. Multiple Backup Locations

- Local storage for quick recovery
- Cloud storage for disaster recovery
- Off-site storage for compliance

### 3. Encrypt Backups

```bash
# Encrypt with GPG
pg_dump hookverse | gzip | gpg --encrypt --recipient admin@hookverse.com > backup.sql.gz.gpg

# Decrypt
gpg --decrypt backup.sql.gz.gpg | gunzip | psql hookverse
```

### 4. Monitor Backup Health

- Track backup age
- Verify backup size trends
- Alert on failures

### 5. Document Procedures

- Backup schedule
- Restore steps
- Contact information
- Troubleshooting guide

## Troubleshooting

### Issue: Backup taking too long

**Solutions**:
- Use pg_basebackup for large databases
- Compress during backup: `-F c`
- Exclude large tables if acceptable
- Increase checkpoint_completion_target

### Issue: Restore fails with errors

**Solutions**:
```bash
# Ignore errors and continue
psql -d hookverse -f backup.sql --single-transaction

# Skip problematic objects
pg_restore --no-owner --no-privileges backup.dump
```

### Issue: Insufficient disk space

**Solutions**:
- Use compression: `gzip`, `-F c`
- Stream to cloud storage
- Clean old backups
- Increase disk space

### Issue: Backup doesn't include all data

**Check**:
```sql
-- Verify all tables backed up
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename))
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

## Related Documentation

- [Quickstart Validation](./quickstart-validation.md)
- [Operations Runbook](./runbook.md)
- [Monitoring](../observability/README.md)
- [Kubernetes Deployment](./kubernetes-deployment.md)

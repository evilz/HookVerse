# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) documenting significant technical decisions made during the development of HookVerse.

## ADR Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [ADR-001](./001-dotnet-technology-stack.md) | .NET Technology Stack Selection | Accepted | 2025-10-18 |
| [ADR-002](./002-message-bus-abstraction.md) | Message Bus Abstraction with MassTransit | Accepted | 2025-10-18 |
| [ADR-003](./003-ef-core-data-layer.md) | EF Core for Data Abstraction | Accepted | 2025-10-18 |
| [ADR-004](./004-webhook-delivery-patterns.md) | Webhook Delivery Patterns | Accepted | 2025-10-18 |
| [ADR-005](./005-schema-validation-multi-format.md) | Multi-Format Schema Validation | Accepted | 2025-10-18 |

## ADR Template

When creating a new ADR, use the following template:

```markdown
# ADR-XXX: [Title]

**Status**: Proposed | Accepted | Deprecated | Superseded by ADR-YYY  
**Date**: YYYY-MM-DD  
**Deciders**: [List of people involved in the decision]  
**Tags**: [technology, architecture, security, etc.]

## Context

[Describe the problem or requirement that necessitates this decision]

## Decision

[Describe the decision that was made]

## Consequences

### Positive
- [Benefit 1]
- [Benefit 2]

### Negative
- [Trade-off 1]
- [Trade-off 2]

### Neutral
- [Neutral consequence 1]

## Alternatives Considered

### Alternative 1: [Name]
- **Pros**: [...]
- **Cons**: [...]
- **Why rejected**: [...]

### Alternative 2: [Name]
- **Pros**: [...]
- **Cons**: [...]
- **Why rejected**: [...]

## References
- [Link to relevant documentation]
- [Link to discussions]
```

## Contributing

When making significant architectural decisions:

1. Create a new ADR file with the next sequential number
2. Use the template above
3. Get review from team leads
4. Update this index
5. Reference the ADR in code comments where relevant

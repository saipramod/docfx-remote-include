# Service Runbook Standards

A runbook is an operational document that tells an on-call engineer how to keep a
service healthy and how to recover it when something goes wrong. This guidance is
the source of truth for what every runbook should contain.

## Required Sections

Every runbook must include the following sections:

1. **Service Overview** — what the service does and who depends on it.
2. **Health Checks** — how to tell, at a glance, whether the service is healthy.
3. **Common Alerts** — each alert, what it means, and the first action to take.
4. **Recovery Procedures** — step-by-step actions to restore the service.
5. **Dependencies** — upstream and downstream services, with health endpoints.
6. **Escalation** — who to contact and when, plus the relevant communication channel.

## Quality Bar

- Procedures must be executable by an engineer unfamiliar with the service.
- Each recovery step should include its expected duration and how to roll back.
- Reference automation by exact command or link; never describe it vaguely.
- Keep contact details pointing at rotations or channels, never individuals.

## Review Cadence

- Review each runbook at least once per quarter.
- Update the runbook within two weeks of any architecture change.

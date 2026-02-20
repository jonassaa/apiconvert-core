---
name: apiconvert-production-consumer-review
description: Review apiconvert-core from a paying production consumer perspective focused on adoption risk, observability, resilience, backward compatibility, and long-term maintainability. Use when asked for production-readiness feedback and prioritized improvements for either TARGET=.NET or TARGET=Node.
---

# Apiconvert Production Consumer Review

Review as a customer integrating into a mission-critical production system, not as the library author.

## Inputs

- Require `TARGET` as `.NET` or `Node`.
- If `TARGET=.NET`, review only the .NET package.
- If `TARGET=Node`, review only the Node/TypeScript package.

## Mindset

- Assume real customer data and CI/CD operation.
- Prioritize stability, ergonomics, debugging, logging, and maintainability over years.
- Expect messy payloads, evolving rules, and strict uptime/SLA concerns.

## Workflow

1. First-time consumer experience:
   - Installation/setup clarity, getting-started quality, mental model fit in under 10 minutes.
   - Capture confusion points, clean aspects, and hidden assumptions.
2. Developer experience:
   - API ergonomics, naming clarity, discoverability, configuration clarity.
   - Ease of defining rules, running conversion, inspecting output, debugging failures.
   - Propose preferred API shape where current experience is awkward.
3. Production readiness:
   - Error handling quality (actionability, structure, context).
   - Observability support (logging, rule tracing, telemetry integration).
   - Determinism/safety clarity and documented behavior.
   - Performance risk for memory, large payloads, repeated rule execution.
4. Maintainability over time:
   - Rule evolution experience, upgrade fear, breaking-change risk, API stability.
   - Recommend maintainer guarantees, SemVer discipline, and consumer contract tests.
5. Edge case simulation:
   - Invalid input structure, partial rule matches, conflicting rules.
   - Deep nesting and very large payloads.
   - Describe current outcome, desired outcome, and required safety nets.

## Deliverable format

Produce these sections:

- A) Overall Production Confidence Score (1-10) + explanation
- B) What I Loved as a Consumer
- C) What Makes Me Nervous
- D) API Friction Points
- E) Production Gaps
- F) Recommended Improvements (ranked P0/P1/P2)
- G) "If I were adopting this tomorrow..." summary

Include at least 3 ideal usage snippets for the selected target runtime.

## Rules

- Be critical but fair and think like an SLA owner.
- Focus on consumer impact, not implementation aesthetics.
- Reference exact file paths and symbols when claiming concrete issues.

## Findings persistence

Always append the full report to `.codex/review-findings.md` (Git-ignored):

1. Create the file if missing.
2. Append, do not overwrite.
3. Add a header with timestamp, target, and scope.

Use this template:

```markdown
## [apiconvert-production-consumer-review][TARGET={.NET|Node}] YYYY-MM-DD HH:mm:ss

{report}
```

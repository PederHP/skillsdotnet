# Code Review Checklist

## Security
- [ ] No hardcoded secrets or credentials
- [ ] Input is validated at system boundaries
- [ ] No SQL injection, XSS, or command injection vectors
- [ ] Authentication and authorization checks are present

## Correctness
- [ ] Edge cases are handled (nulls, empty collections, boundaries)
- [ ] Error handling is appropriate (no swallowed exceptions)
- [ ] Concurrency is handled correctly (races, deadlocks)

## Performance
- [ ] No unnecessary allocations in hot paths
- [ ] Database queries are efficient (no N+1)
- [ ] Caching is used where appropriate

## Maintainability
- [ ] Code is readable and intent is clear
- [ ] No duplication that should be extracted
- [ ] Tests cover the new/changed behavior

# Security policy

## Supported versions

Only the latest release is supported. ReadSide is a learning project in early development (0.x).

## Reporting a vulnerability

Please report security problems privately, using the "Report a vulnerability" button on the repository's Security tab. Please do not open a public issue for a vulnerability.

This is a side project, so I cannot promise a response time, but I will reply when I can.

## What counts as a security problem

ReadSide builds SQL text and parameters. A security problem is anything that lets a value end up inside the SQL text instead of a parameter (SQL injection), or that produces SQL that does something different from what the lambda says.
# FSH.MarketIntelligence

# Development Guide

Version: 1.0

Last Updated: 2026-07-16

---

# Purpose

This document defines the development practices for FSH.MarketIntelligence.

The primary objective is to keep the project consistent with FullStackHero conventions while allowing the MarketIntelligence module to evolve independently.

---

# Core Principle

Follow framework conventions before introducing new patterns.

Existing FullStackHero conventions should be preferred unless there is a clear technical reason to introduce a different approach.

---

# Development Workflow

The development cycle is:

1. Understand the requirement.
2. Identify the affected module.
3. Identify the affected feature.
4. Implement the smallest useful change.
5. Build the solution.
6. Run relevant tests.
7. Verify the API when applicable.
8. Update documentation when the decision is significant.
9. Commit the change.

---

# Feature Development

Features are organized vertically.

Example:

```text
Features
└── v1
    └── Disclosures
        └── SearchDisclosures
            ├── SearchDisclosuresEndpoint.cs
            └── SearchDisclosuresQueryHandler.cs
# ProjectContext.md

Important Emails :
Hotmail --------> Ebrahimian_4585@hotmail.com           Pass:Khashayar76  -->GitHub, Microsoft, Every General 
Gmail   --------> Ebrahimian.Behzad@gmail.com           Pass:Khashayar76  -->ChatGpt
Gmail   --------> mebrahimian4585@gmail.com             Pass:Sadaf4585    -->Alternate Ebrahimian_4585@hotmail.com 
==========================
upgrade .net 
PS D:\myappfsh.marketintelligence\src> dotnet tool update dotnet-ef --version 10.0.8
===========================
Migration List 
dotnet ef migrations list --context "FSH.Modules.Identity.Data.IdentityDbContext" 
                          --project ".\Host\FSH.Starter.Migrations.MSSQL\FSH.Starter.Migrations.MSSQL.csproj" 
                          --startup-project ".\Host\FSH.Starter.Api\FSH.Starter.Api.csproj"
==========================
Update Database
dotnet ef database update --context "FSH.Modules.Identity.Data.IdentityDbContext" 
                          --project ".\Host\FSH.Starter.Migrations.MSSQL\FSH.Starter.Migrations.MSSQL.csproj" 
                          --startup-project ".\Host\FSH.Starter.Api\FSH.Starter.Api.csproj"
=========================

# FSH.MarketIntelligence

**Version:** 1.0  
**Last Updated:** 2026-07-15

---

# 1. Project Vision

FSH.MarketIntelligence is a long-term Market Intelligence platform built as a native module of the FullStackHero (.NET Starter Kit).

The goal is **not** to build a simple Codal scraper.

The goal is to build a complete Iranian Stock Market Intelligence Platform capable of:

- collecting data
- storing historical information
- analyzing disclosures
- generating insights
- serving ERP and external SaaS customers

MarketIntelligence is considered a first-class FSH module.

---

# 2. Long-Term Goals

The module will eventually support:

- Codal integration
- TSETMC integration
- Financial Statements
- Monthly Production & Sales reports
- Capital Increase reports
- Shareholder changes
- Board decisions
- News aggregation
- AI-based analysis
- Financial ratios
- Screening
- Dashboards
- Alerts
- SaaS APIs

---

# 3. Technology Stack

Framework:

- .NET 10
- FullStackHero Starter Kit
- Clean Architecture
- Vertical Slice Architecture
- Mediator
- EF Core
- SQL Server

UI:

- Blazor
- MudBlazor

---

# 4. Solution Structure

src/

    BuildingBlocks/

    Host/

        FSH.Starter.Api

        FSH.Starter.DbMigrator

        FSH.Starter.AppHost

    Modules/

        Auditing

        Billing

        Catalog

        Chat

        Files

        Identity

        MarketIntelligence

            Modules.MarketIntelligence

            Modules.MarketIntelligence.Contracts

        Multitenancy

        Notifications

        Tickets

        Webhooks

    Tests/

---

# 5. Current Module Status

## MarketIntelligence

Current progress:

✅ Projects created

✅ Contracts project created

✅ Main module project created

✅ ProjectReferences configured

✅ Registered inside Host

✅ Build successful

Next steps:

⬜ DbContext

⬜ DbInitializer

⬜ HealthChecks

⬜ API Versioning

⬜ Endpoints

⬜ CQRS

⬜ Validators

⬜ Repository layer

⬜ Codal Client

⬜ HTML Parser

⬜ Database

⬜ Scheduler

⬜ Dashboard

---

# 6. Development Rules

Every change must follow this order:

1. Create structure
2. Build
3. Register
4. Build
5. Add Feature
6. Build
7. Commit

Never implement multiple steps before a successful build.

---

# 7. Git Workflow

Development is performed on feature branches.

Never develop directly on main.

Each milestone should end with:

- successful build
- commit
- ProjectContext update

---

# 8. Important Lessons Learned

## Lesson 001

Missing ProjectReference can produce misleading Mediator errors.

Example:

MSG0007

The real problem may NOT be Mediator.

Always verify ProjectReferences first.

---

## Lesson 002

Module registration should only happen AFTER:

- project builds
- namespace exists
- references are configured

---

## Lesson 003

Never copy Catalog module blindly.

Build MarketIntelligence incrementally.

---

# 9. Coding Philosophy

Prefer:

Small commits

Small builds

Small features

Never spend hours debugging multiple unfinished changes.

---

# 10. Architecture Decisions

ADR-001

MarketIntelligence is an independent FSH module.

ADR-002

SQL Server is the primary database.

ADR-003

Market Intelligence is part of ERP and also designed for future SaaS.

---

# 11. Roadmap

Phase 1

Infrastructure

- Module
- Registration
- DbContext

Phase 2

Codal Integration

- Search
- Reports
- Downloader

Phase 3

Parser

- Monthly Activity
- Financial Statements

Phase 4

Persistence

- Database
- History

Phase 5

Analysis

- KPIs
- Ratios
- AI

Phase 6

Dashboard

---

# 12. Current Milestone

Milestone 1

✔ MarketIntelligence module successfully registered into FSH.

The solution builds successfully after fixing namespace and project-reference issues.

---

# 13. AI Collaboration Notes

The project is developed collaboratively with ChatGPT.

When starting a new conversation:

1. Read this document first.
2. Preserve architecture decisions.
3. Continue from Current Milestone.
4. Never redesign the project from scratch.
5. Prefer incremental improvements over rewrites.

---

END OF DOCUMENT
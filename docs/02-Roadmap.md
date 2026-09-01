\# FSH.MarketIntelligence



\# Roadmap



Version: 1.0



Last Updated: 2026-07-16



---



\# Vision



FSH.MarketIntelligence aims to become a modular market intelligence platform for collecting, processing, storing and analyzing Iranian capital market information.



The system is designed to evolve from a data collection platform into a comprehensive financial intelligence and analysis platform.



---



\# Current Status



\## Completed



\- FullStackHero modular architecture established.

\- MarketIntelligence module created.

\- MarketIntelligence Contracts project created.

\- Project references configured.

\- Module successfully registered.

\- Module successfully builds.

\- Module successfully runs.

\- API endpoints are discoverable through Scalar.

\- Initial Disclosure feature created.

\- Initial Disclosure endpoint successfully verified.



---



\# Phase 1 — Module Foundation



Status: Completed



Objectives:



\- Create the MarketIntelligence module.

\- Create the Contracts project.

\- Configure project references.

\- Register the module.

\- Verify module discovery.

\- Verify endpoint mapping.

\- Verify Scalar integration.



---



\# Phase 2 — Disclosure Foundation



Status: In Progress



Objectives:



\- Define Disclosure domain model.

\- Define Disclosure contracts.

\- Implement SearchDisclosures feature.

\- Integrate Codal search API.

\- Retrieve disclosure metadata.

\- Validate the external API response.



Expected result:



The system can search and retrieve disclosure metadata from Codal.



---



\# Phase 3 — Codal Integration



Objectives:



\- Implement Codal HTTP client.

\- Implement Codal search request model.

\- Implement Codal search response model.

\- Support Codal query parameters.

\- Support pagination.

\- Handle HTTP failures.

\- Handle invalid responses.

\- Handle rate limiting and temporary failures.



Initial Codal categories:



\- Monthly activity reports.

\- Financial statements.

\- Capital increase disclosures.

\- General disclosures.



---



\# Phase 4 — Disclosure Processing



Objectives:



\- Retrieve disclosure details.

\- Resolve LetterSerial values.

\- Build disclosure URLs.

\- Retrieve disclosure HTML.

\- Detect disclosure format.

\- Parse disclosure content.



The processing pipeline will be:



Codal Search



↓



Disclosure Metadata



↓



Disclosure URL



↓



Disclosure Content



↓



Parser



↓



Structured Data



---



\# Phase 5 — Data Persistence



Objectives:



\- Implement MarketIntelligenceDbContext.

\- Configure Disclosure entity.

\- Create database migrations.

\- Store disclosure metadata.

\- Store processing status.

\- Store raw data when required.



The persistence layer must support incremental processing.



---



\# Phase 6 — Market Data Collection



Objectives:



\- Collect company information.

\- Integrate MasterInfo.

\- Collect monthly sales activity.

\- Collect daily market data.

\- Support adjusted market data.



Potential sources:



\- Codal

\- TSETMC

\- Existing historical market data



---



\# Phase 7 — Financial Intelligence



Objectives:



\- Calculate financial indicators.

\- Analyze sales trends.

\- Analyze monthly performance.

\- Compare companies.

\- Detect abnormal changes.

\- Generate financial signals.



---



\# Phase 8 — AI and Advanced Analytics



Future objectives:



\- Financial forecasting.

\- Anomaly detection.

\- Company ranking.

\- Pattern detection.

\- Natural language analysis of disclosures.

\- AI-assisted financial analysis.



---



\# Phase 9 — SaaS Platform



Long-term objectives:



\- Multi-tenant market intelligence.

\- Tenant-specific watchlists.

\- User dashboards.

\- Alerts.

\- Subscription plans.

\- Public APIs.



---



\# Development Principle



The project must evolve incrementally.



Each phase must produce a working and verifiable result before the next phase begins.



---



\# Roadmap Rule



The roadmap is directional, not rigid.



Technical discoveries, business requirements and market conditions may change the implementation order.



---



\# Current Priority



The immediate priority is:



1\. Complete Disclosure search.

2\. Integrate Codal.

3\. Retrieve disclosure metadata.

4\. Retrieve disclosure content.

5\. Implement the first real parser.

6\. Persist processed data.


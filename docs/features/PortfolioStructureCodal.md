# Codal Portfolio Layouts

## Report Types

Portfolio reports currently come from:

- `let=58, rt=2` → Monthly Activity Portfolio
- `let=6, rt=2` → Financial Statement Portfolio

## Logical Portfolio Key

A portfolio snapshot is identified by:

- ParentCompanyId
- PeriodEndDate
- SourceType
- AuditStatus

`DisclosureId` and `TracingNo` are provenance fields, not logical keys.

Corrections replace the previous report with the same logical key.

---

## Listed / Unlisted Tables

Stable structural identifiers:

| Portfolio | SheetCode | MetaTableCode |
|-----------|-----------|---------------|
|   Listed  |    4      |    1470       |
| Unlisted  |    5      |    1471       |

---

## Old Financial Statement Layout

Observed historically:

| Portfolio | MetaTableId | MetaTableCode |
|-----------|-------------|---------------|
|  Listed   |   1470      |    1470       |
|  Unlisted |   1471      |    1471       |

In this layout `MetaTableId` does NOT identify audit status.

Audit status must be derived from the Disclosure title:

- `حسابرسی نشده` → Unaudited
- `حسابرسی شده` → Audited

Do NOT use a hard-coded date boundary to identify old layouts.

---

## New Financial Statement Layout

### Unaudited

| Portfolio | MetaTableId | MetaTableCode |
|-----------|-------------|---------------|
|  Listed   |   1507      |     1470      |
|  Unlisted |   1508      |     1471      |
 
### Audited

| Portfolio | MetaTableId | MetaTableCode |
|-----------|-------------|---------------|
|  Listed   |    1529     |     1470      |
|  Unlisted |    1530     |     1471      |

---

## Audit Resolution Rule

```text
1529 / 1530 → Audited
1507 / 1508 → Unaudited
1470 / 1471 → Resolve from Disclosure.Title
unknown       → None / reject financial-statement persistence
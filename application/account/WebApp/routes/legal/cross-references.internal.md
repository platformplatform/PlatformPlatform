# Legal Documents Cross-References and Maintenance Guide

**Internal Document** - Not for public deployment

This document provides comprehensive background, principles, cross-references, and maintenance guidance for the legal document templates (Privacy Policy, Terms of Service, DPA, LIA).

**Purpose:** Enable any AI or developer to understand the context, reasoning, and standards used in these documents without needing to recreate months of legal research and iteration.

Last updated: 5 Dec, 2025

---

## CRITICAL BACKGROUND AND PRINCIPLES

### Target Audience

These legal templates are designed for:
- **Small startups with 1-5 employees**
- **B2B SaaS products** (not consumer/B2C)
- **Self-signup services** (no sales team, no contract negotiation)
- **Product-led growth** where customers sign up themselves
- **Minimal manual operations** (everything must be self-service)

### Core Philosophy

**1. Compliant but Minimal**
- Include ONLY what is legally required by GDPR, CCPA, and data protection laws
- Do NOT add aspirational practices we don't have
- Do NOT overpromise capabilities beyond our operational reality
- Be honest about constraints (90-day deletion minimum, Account-level operations only)

**2. Self-Service Everything**
- No "we will work with you to..." promises
- No "contact us and we'll help you..." commitments
- Direct customers to Compliance Center page with self-service documents
- Reference Microsoft Azure Compliance Center for infrastructure compliance
- Make all compliance documentation (pen tests, certifications, security docs) publicly available for self-service

**3. Account-Level Operations**
- **CRYSTAL CLEAR:** Account Owner accepts Terms, Privacy, DPA on behalf of ALL Users
- **CRYSTAL CLEAR:** We do NOT handle individual User data requests
- **CRYSTAL CLEAR:** Data deletion requires Account cancellation (not individual User deletion)
- All Users within an Account are bound by what Account Owner accepts

**4. No Auto-Deletion**
- Data is retained for MINIMUM 90 days after cancellation
- Data is NOT auto-deleted after 90 days
- Only deleted when Account Owner explicitly requests deletion via in-app functionality OR we reserve right to delete with reasonable notice
- Allows customers to resubscribe (critical for seasonal use - e.g., ERP used twice/year for VAT)

**5. Minimize Repetition and Cross-References**
- Do NOT duplicate retention periods, timelines, procedures across documents
- Reference other documents: "as described in our Privacy Policy" (NO section numbers)
- If something changes, it should only need updating in ONE place
- Cross-doc references: "Terms of Service" NOT "Section 13" (sections change)

**6. Material vs Non-Material Changes**

**Material changes (trigger customer termination right):**
- Adding new sub-processors
- Changing data processing purposes
- Reducing data protection safeguards
- Changing retention periods
- Moving to less secure infrastructure

**Non-material changes (do NOT trigger termination right):**
- TLS 1.3 → TLS 1.4 (security upgrade)
- Adding MFA (security enhancement)
- Company acquisition (unless affects data processing)
- Adding compliance certifications
- Clarifications, formatting, typo fixes

**Why this matters:** We need to improve security and iterate the product without triggering mass customer terminations.

### What NOT to Do (Repeated Mistakes)

**DON'T invent time frames:**
- ❌ "24 hours" for breach notification (not required)
- ❌ "30 days" for data export (we don't offer it)
- ✅ Use "without undue delay" or "reasonable time"

**DON'T invent procedures:**
- ❌ "we will work with you to provide..."
- ❌ "contact us and we'll help you..."
- ✅ "available at [compliance page]" (self-service)

**DON'T promise things we can't do:**
- ❌ Individual User data deletion
- ❌ Immediate data deletion (we need 90 days)
- ❌ Data export (until required by EU in 2027)
- ❌ Penalty-free termination for sub-processor changes
- ❌ MFA, penetration testing, formal training (if we don't have it)

**DON'T put TODO notes in document body:**
- ❌ "Note: Downstream projects should customize this..."
- ✅ All downstream guidance in top warning banner ONLY

**DON'T use section numbers across documents:**
- ❌ "as described in Privacy Policy Section 6"
- ✅ "as described in our Privacy Policy"

**DON'T auto-delete data:**
- ❌ "After 90 days, data is deleted"
- ✅ "After subscription cancellation, data may be deleted using the in-app functionality"

---

## 1. Cross-Document References That Must Stay in Sync

### Data Retention and Deletion Timeline

**Single Source of Truth: Privacy Policy Section 6**

The detailed data retention and deletion policy is written ONLY in the Privacy Policy. All other documents reference it.

| Document | Section | How It References Privacy Policy |
|----------|---------|-----------------------------------|
| **Privacy Policy** | Section 6 | Contains the complete retention and deletion policy (master copy) |
| **Terms of Service** | Section 11 | "Data retention and deletion after cancellation is described in our Privacy Policy" |
| **DPA** | Section 11 | "Data retention and deletion after Account termination or cancellation is described in our Privacy Policy" |

**Retention Policy:**
- After cancellation: Data retained for at least 60 days
- After 60 days: We may continue to retain data or delete it at our discretion
- If we decide to delete: Reasonable advance notice to Account Owner
- Account Owner can request deletion anytime via in-app deletion function
- Cancellation does not automatically trigger deletion

**Deletion Timeline When Requested:**

Public-facing policy states: "Deletion takes 60 to 90 days to complete"

**Actual implementation (internal only):**
- First 30 days: Grace period to verify deletion request was authorized
- Next 30 days: Release immutable BLOB storage lock (if implemented)
- Final 0-30 days: Complete deletion across all systems
- Total: 60-90 days from deletion request

**Examples:**
- Deletion requested during cancellation: 60-90 days from cancellation
- Deletion requested 10 days after cancellation: 70-100 days from cancellation (but policy says 60-90 from request, which is true)
- Deletion requested 1 year after cancellation: 60-90 days from deletion request

**Why 60-90 day range gives us flexibility:**
- Can change immutable storage period (0, 30, 60, or 90 days)
- Can remove immutable storage entirely
- Can start releasing immutable flag at termination instead of deletion request
- Always compliant as long as we complete within 90 days of deletion request

**CRITICAL:** Data is NEVER auto-deleted. Only deleted when:
1. Account Owner explicitly requests via in-app deletion function, OR
2. We decide to delete with advance notice to Account Owner

This allows customers to resubscribe (important for seasonal use like ERP systems used twice yearly for VAT).

### Payment Suspension vs Cancellation

| Document | Section | Key Point |
|----------|---------|-----------|
| **Terms** | Section 3 | "Payment suspension is not considered a cancellation and does not trigger the 90-day deletion period" |
| **Terms** | Section 3 | "If payment remains unpaid for an extended period (typically 12-36 months), we may permanently delete..." |
| **DPA** | Section 11 | "During payment suspension... data is retained as described in the Terms of Service" |

**Key principle:** Payment failure ≠ cancellation. Data persists during payment issues to protect customers who occasionally use the service.

### International Data Transfers

| Document | Section | Text |
|----------|---------|------|
| **Privacy Policy** | Section 12 | "These transfers are protected by Microsoft's Data Processing Agreement incorporating Standard Contractual Clauses approved by the European Commission" |
| **DPA** | Section 9 | "These transfers are protected by Microsoft's Data Processing Agreement incorporating Standard Contractual Clauses... where required by applicable data protection laws" |

**Direction:** Analytics data flows INTO EU (from non-EU customers), NOT out of EU.

**Key principle:** Personal/business data stays in selected Azure region. Only pseudonymized analytics goes to EU region.

### Sub-Processor Notice Period

| Document | Section | Period | Method |
|----------|---------|--------|--------|
| **DPA** | Section 5 | "at least 14 days advance notice" | "via email to Account Owner's registered email address" |
| **Privacy Policy** | Top warning | "update policy when processors change" | N/A |

**Key principle:** 14 days is NOT legally required by GDPR, but it's our commitment. Could be any reasonable period.

**Notification method:** Email only (not in-app) for formal legal notices.

### Material Changes Notice Period

| Document | Section | Period |
|----------|---------|--------|
| **Privacy Policy** | Section 13 | "at least 14 days notice" |
| **Terms** | Section 12 | "at least 14 days notice" |
| **DPA** | Section 14 | "at least 14 days advance notice" |

**All must match:** 14 days for material changes across all documents.

### Governing Law

| Document | Section | Text |
|----------|---------|------|
| **Privacy Policy** | Section 15 | "governed by the laws of Denmark" |
| **Terms** | Section 13 | "governed by... laws of Denmark... exclusive jurisdiction of the courts of Copenhagen, Denmark" |
| **DPA** | Section 13 | "governed by and subject to the governing law and dispute resolution provisions set forth in the Terms of Service" |

**Key principle:** DPA references Terms (avoids duplication). If Terms changes, DPA automatically follows.

### DPA Link References

| Document | Section | Link |
|----------|---------|------|
| **Privacy Policy** | Data Controller header | `[/legal/dpa](/legal/dpa)` |
| **Privacy Policy** | Section 4 | "Our DPA template is publicly available at [/legal/dpa](/legal/dpa)" |
| **Terms** | Section 7 | "The DPA is publicly available at [/legal/dpa](/legal/dpa)" |

**All must use same relative link format:** `[/legal/dpa](/legal/dpa)`

---

## 2. Terminology Standards

### Capitalized Defined Terms

Use Title Case for defined terms when referring to them as defined concepts:

| Term | Usage | Example |
|------|-------|---------|
| **Account** | The organizational workspace | "When an **Account** is deleted..." |
| **User** | Individual person with access | "All **Users** are responsible..." |
| **Service** | The software platform | "We provide the **Service**..." |
| **Personal Data** | Regulated data | "We process **Personal Data**..." |
| **Account Owner** | User with primary authority | "The **Account Owner** accepts..." |
| **Account Administrator** | User with elevated permissions | "**Account Administrators** can invite..." |

When used generically (not as defined term), use lowercase: "your account settings", "user preferences"

### "You" vs "The Account"

| Context | Use | Example |
|---------|-----|---------|
| **Instructions/explanations** | "you/your" | "you must update the date" |
| **Formal obligations (DPA)** | "the Account" | "the Account's documented instructions" |
| **Rights/choices** | "you/your" | "you may object to..." |
| **Definitions** | Formal language | "The Account, which determines..." |

**DPA uses "the Account" for formal processor obligations to be precise about who the Controller is.**

---

## 3. Account-Level Operations Only

**Critical principle:** All data operations are at Account level, NEVER individual User level.

**User deletion while Account active:** Users can be soft-deleted (marked as deleted) but Personal Data may be retained to show "created by [deleted user]" for audit trails.

**Account deletion:** Deletes ALL data for ALL Users within the Account. No selective User deletion.

**Privacy Policy Section 7 states:**
> "To request full data deletion, the Account must be cancelled. Individual User data deletion is not available while the Account remains active."

**DPA Section 11 states:**
> "Data deletion is performed at the Account level. We do not delete data for individual Users while the Account remains active."

**Why:** We don't support manual deletion of individual User data from databases. Operations are Account-scoped for efficiency and data integrity.

---

## 4. Security Practices - What We Actually Have

### From Infrastructure Code Review

**We HAVE (describe in DPA Schedule 3):**
- Encryption in transit: HTTPS/TLS 1.2+ (Azure SQL Server enforced)
- Encryption at rest: Azure platform encryption
- RBAC: Azure Active Directory authentication only (azureADOnlyAuthentication: true)
- SQL Server auditing: 90-day retention (authentication, batch operations)
- SQL Server vulnerability assessments: Recurring scans enabled
- SQL Server security alerts: Enabled
- Virtual network isolation: Subnet-based access control
- Restricted outbound network access: Enabled on SQL Server
- Application Insights: Monitoring and logging (may be sampled)
- Telemetry events: Activity logging for mutations (not comprehensive audit)

**We DON'T have (don't claim in DPA):**
- MFA for users
- Penetration testing
- Background checks for employees
- Formal security training programs
- Custom IDS/IPS
- Comprehensive audit trails (only SQL logs + telemetry)
- Vulnerability management program
- Formal DPIA procedures
- Business continuity documentation
- SOC 2 or ISO 27001 certifications

**Guidance:** Only describe what Azure PaaS provides + what we implement. Don't invent practices.

---

## 5. Controller vs Processor - When Does DPA Apply?

**DPA applies when:**
- Customer uploads THEIR customer data (names, emails of their customers)
- Customer uploads employee data
- Customer processes any third-party Personal Data through our Service
- **The customer decides WHY and HOW to process the data**

**Privacy Policy applies (we are Controller) when:**
- We collect data about Account Owners and Users (emails, names, roles)
- We collect analytics about Service usage
- We collect device/location data for our analytics
- **We decide WHY and HOW to process the data (service improvement, security)**

**Key insight:** Most B2B SaaS has DUAL role:
- Controller for analytics/service data
- Processor for customer's business data

DPA Section 1 clarifies this distinction.

---

## 6. Terminology Alignment Across Documents

### "Schedule" vs "Appendix" vs "Annex"

**Industry research:** Salesforce, Atlassian both use "Schedule"

**Our choice:** Use "Schedule" (matches industry standard for B2B SaaS)

### Contact Emails

| Document | Purpose | Email |
|----------|---------|-------|
| **Privacy Policy** | Privacy/GDPR rights | privacy@company.net |
| **Terms** | Legal/contract | legal@company.net |
| **DPA** | Data processing | legal@company.net |

**Different emails acceptable** - separates privacy rights from contract matters.

### Section Number References

**DON'T reference section numbers across documents.** They change.

**DO:**
- "as described in our Privacy Policy" (no section number)
- "as described in the Terms of Service" (no section number)
- "governed by the Terms of Service" (no section number)

**Exception:** Within same document, section numbers OK ("as described in Section 11")

---

## 7. Common Update Scenarios

### Adding a New Sub-Processor

**Steps:**
1. Update Privacy Policy Section 4 (add to list)
2. Update DPA Schedule 2 (add to list)
3. Notify all customers 14 days in advance (email or in-app)
4. Allow customers to object or terminate
5. Update both documents with new effective date

### Changing Retention Period

**Documents to update:**
1. Privacy Policy Section 6
2. Terms Section 11
3. DPA Section 11

All three must say the same thing.

### Adding New Data Types Collected

**Documents to update:**
1. Privacy Policy Section 1 (what we collect)
2. DPA Schedule 1 (types of Personal Data)

Must match exactly.

---

## 8. GDPR Article 28(3) Mandatory Elements Checklist

**All must be present in DPA:**

- ✅ Subject matter (Schedule 1: "Processing of Personal Data in connection with...")
- ✅ Duration (Schedule 1: "duration of subscription + retention period")
- ✅ Nature and purpose (Schedule 1: hosting, service functionality, backup...)
- ✅ Types of Personal Data (Schedule 1: comprehensive list)
- ✅ Categories of Data Subjects (Schedule 1: employees, customers, others)
- ✅ Process only on instructions (Section 4)
- ✅ Notify if law requires processing without instructions (Section 4)
- ✅ Notify if instructions infringe GDPR (Section 4)
- ✅ Confidentiality commitments (Section 4: "or statutory obligation")
- ✅ Security measures (Section 7 + Schedule 3)
- ✅ Sub-processor authorization and notice (Section 5 + Schedule 2)
- ✅ Data Subject rights assistance (Section 6)
- ✅ Security/breach/DPIA assistance (Section 4)
- ✅ Data deletion on request (Section 11)
- ✅ Audit rights (Section 10)

**Key:** All elements present, using globally applicable language (not EU-only article numbers).

---

## 9. Why We Made Certain Choices

### Why "or statutory obligation" for Confidentiality?

Danish employment law (funktionær contracts) provides statutory confidentiality. GDPR Article 28(3)(b) explicitly allows EITHER contractual OR statutory confidentiality. This works for small startups without formal confidentiality agreements.

### Why Keep US Legal Notification Clause?

Even though we're EU-based, the clause ("unless law prohibits") doesn't overpromise. It's a standard GDPR DPA clause. Keeping it makes the template more broadly applicable if downstream operates in US.

### Why Reference Terms for Governing Law?

Avoids having governing law stated in two places. If we update Terms jurisdiction, DPA automatically follows. Reduces risk of contradiction.

### Why Account-Level Deletion Only?

Operational reality: We don't support manual deletion of individual User data from databases. Too complex, risky, and unnecessary for B2B where Account Owner controls all User data.

### Why 90 Days Not Auto-Delete?

Customer might want to resubscribe. Example: Solo entrepreneur using ERP twice/year for VAT. If we auto-deleted, they'd lose all their data. Only delete on explicit request.

---

## 10. What Downstream Projects Must Update

**Critical updates before production:**

1. **All Documents:**
   - Company name, address, contact emails
   - Effective/Last updated dates
   - Governing law (if not Denmark)

2. **Privacy Policy:**
   - Section 1: Data collection categories
   - Section 4: Complete sub-processor list
   - Section 9: AI features (or remove)
   - Section 16: California section (if serving CA)

3. **Terms:**
   - Section 2: Service description
   - Section 3: Payment terms URLs

4. **DPA:**
   - Schedule 1: Types of Personal Data (match what app actually processes)
   - Schedule 2: All sub-processors
   - Schedule 3: Actual security practices implemented


**Optional downstream additions:**
- MFA, penetration testing, security certifications
- Formal training programs
- Additional jurisdictional compliance sections

---

## 11. Key Learnings and Context

### From Legal Review Process

**GDPR compliance insights:**
- Article 28(3) has 8 specific requirements (a-h) - all must be in DPA
- Article numbers are EU-specific - use descriptive language for global docs
- "Schedule" is industry standard (Salesforce, Atlassian)
- Statutory confidentiality (employment law) satisfies Article 28(3)(b)

**Industry practices researched:**
- Salesforce: Account owner explicitly agrees "on behalf of all users"
- Slack: $100 liability cap (extreme), customer owns all data
- Atlassian: Uses "reasonable efforts" language, defers details to "Documentation"
- Microsoft: Minimal cross-references between documents

**What makes good legal templates:**
- Honest about operational constraints
- Globally applicable (not EU-only)
- Aligned terminology across all documents
- Minimal - only what's legally required
- Clear about Account-level vs User-level operations

### Common Pitfalls to Avoid

1. **Don't promise individual User deletion** - We only delete Accounts
2. **Don't make up security practices** - Only describe what actually exists (check cloud-infrastructure bicep files)
3. **Don't use section numbers in cross-doc references** - They change (use "Terms of Service" not "Section 13")
4. **Don't duplicate governing law** - DPA references Terms
5. **Don't mix "you" and "the Account" randomly** - DPA uses "the Account" in formal obligations
6. **Don't put downstream notes in document body** - Only in top warning
7. **Don't auto-delete after 90 days** - Only on explicit request
8. **Don't promise data export before 2027** - EU Data Act requirement
9. **Don't promise 24-hour breach notification** - Use "without undue delay"
10. **Don't list specific auth methods** - Use high-level "authentication and access management"
11. **Don't promise penalty-free termination for sub-processor objections** - Normal termination per Terms

---

## 12. Document Structure Standards

### Warning Banner Format

Use blockquote with bullets:
```markdown
> ⚠️ **Important: Customize This [Document]**
>
> This is an AI-generated template... Before production use, you must:
> - Item 1
> - Item 2
```

**MarkdownRenderer requires:** Each bullet must start with `> -` to render properly on separate lines.

### Definition Format

```markdown
**Term:** Definition text here.
```

**MarkdownRenderer:** Bold term followed by colon triggers definition styling (separate paragraph with spacing).

### ASCII Table Format (Cookies, Sub-Processors)

```markdown
```
Header Column 1       Header Column 2              Header Column 3
row1-col1             row1-col2                    row1-col3
```
```

Use code blocks with aligned spacing. MarkdownRenderer renders with monospace font.

### Cross-Document Links

Use relative markdown links:
```markdown
[/legal/dpa](/legal/dpa)
[/legal/privacy](/legal/privacy)
```

---

## 13. Maintaining Consistency

**Before publishing changes:**

1. **Update all effective dates together** - Keep all docs on same date
2. **Check cross-references table** (Section 1 above) - Verify all match
3. **Search for section number references** - Update if sections renumbered
4. **Verify terminology** - Account, User, Service, Personal Data capitalized consistently
5. **Check retention periods** - 90 days, 14 days must match everywhere
6. **Verify sub-processor lists match** - Privacy Policy Section 4 = DPA Schedule 2

**Tools:**
- Global search for "90 days", "14 days", "Section X" across all legal docs
- Diff tool to compare retention/notice language
- This cross-reference document as checklist

---

## Contact

For questions about maintaining these legal documents: legal@company.net

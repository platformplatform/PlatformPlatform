# Legitimate Interest Assessment

**Document type:** Internal compliance documentation
**Last reviewed:** 4 Dec, 2025
**Status:** Active

---

> **Internal Document**
>
> This is an AI-generated template Legitimate Interest Assessment (LIA) documenting analysis under GDPR Article 6(1)(f) for processing personal data based on legitimate interests. This document is maintained for compliance purposes and may be shared with data protection authorities or data subjects upon request. Before use, you must customize this document to reflect your actual processing activities, update Section 6 Document History with approval signature and next review date, and have it reviewed by a legal professional.

---

## 1. Purpose of This Assessment

This assessment evaluates whether our use of first-party analytics for authenticated Users in our B2B SaaS platform can be lawfully based on legitimate interests under GDPR Article 6(1)(f). This assessment follows the three-part test outlined in EDPB Guidelines 06/2014 on the notion of legitimate interests.

We have determined that this processing does not require a Data Protection Impact Assessment (DPIA) under GDPR Article 35, as it does not involve high-risk systematic monitoring, automated decision-making with legal effects, or processing of sensitive Personal Data at large scale.

## 2. Processing Activity Under Assessment

**What we process:**
- Usage data from authenticated Users including pages visited, features used, interaction patterns, and session information
- Device and browser information
- Location data (country, state, city)
- User and Account metadata (subscription plans, feature flags, roles, settings)
- Business activity data (entity types, state changes, User actions)

**How we process it:**
- Data is collected via Azure Application Insights (first-party analytics)
- Data flows to our own endpoint before being processed by Microsoft Azure as our data processor
- Data is used for aggregated analysis, individual troubleshooting, and Service improvement

**Who is affected:**
- Authenticated Users of our Service
- Users are typically members of Accounts that have subscribed to our Service

## 3. Legitimate Interest Test

### 3.1 Purpose Test: What is the legitimate interest?

We have identified the following legitimate interests in processing this data:

**Product improvement:**
- Understanding which features are used and valued
- Identifying usability issues and friction points
- Prioritizing development resources effectively

**Service reliability:**
- Detecting and diagnosing errors and performance issues
- Monitoring system health and stability
- Ensuring consistent user experience

**Security:**
- Identifying unusual activity patterns that may indicate security threats
- Supporting incident investigation and response

**Business operations:**
- Understanding user adoption and engagement
- Making informed decisions about service development

These interests are genuine, clearly defined, and represent real business needs for operating a product-led SaaS service.

### 3.2 Necessity Test: Is the processing necessary?

**Why this processing is necessary:**

- We cannot effectively improve our product without understanding how it is used
- Error detection and performance monitoring require usage data to identify issues
- Alternative approaches (such as user surveys alone) would not provide the granular, objective data needed for effective product development
- The data collection is proportionate - we collect only what is needed for these purposes

**Data minimization measures:**

- We do not collect sensitive Personal Data categories
- We do not collect names, emails, or Personal Data content in analytics
- Analytics data is retained to enable long-term trend analysis essential for product improvement
- We process aggregate patterns, not detailed individual profiles

### 3.3 Balancing Test: Do individual rights override our interests?

**Impact on individuals:**

- The processing has minimal impact on individuals' privacy
- We do not use the data for profiling that produces legal or similarly significant effects
- We do not share the data with third parties for their own purposes
- Users remain in control of their account data and can exercise GDPR rights

**Reasonable expectations:**

- Users of SaaS products reasonably expect that usage data will be collected for product improvement
- This is standard practice across the SaaS industry (Atlassian, Slack, Salesforce, etc. operate similarly)
- Account Owners accept these Terms on behalf of all Users within their Account, which is standard practice in B2B SaaS
- The Privacy Policy and Terms of Service disclose data processing practices

**Relationship with data subjects:**

- There is an existing contractual relationship between Users and our Service
- The processing is directly related to providing and improving the Service they use
- There is no imbalance of power that would make consent the only appropriate basis

**Safeguards we have implemented:**

- Clear disclosure in Privacy Policy explaining what we collect and why
- Data processor agreement with Microsoft Azure
- Analytics data used for aggregate trend analysis, not individual profiling
- Security measures to protect data in transit and at rest
- GDPR rights disclosure in Privacy Policy (though full deletion requires Account cancellation)
- We do not sell data or share personally identifiable data for third-party advertising

## 4. Assessment Conclusion

Based on this assessment, we conclude that:

1. Our legitimate interests in product improvement, service reliability, security, and business operations are genuine and clearly defined

2. Processing usage analytics for authenticated Users is necessary to achieve these purposes and cannot be reasonably achieved through less privacy-intrusive means

3. The processing does not override the rights and freedoms of Users because:
   - Impact on privacy is minimal
   - Processing aligns with reasonable expectations for B2B SaaS
   - Appropriate safeguards are in place
   - Users can exercise their GDPR rights

**Therefore, legitimate interest under GDPR Article 6(1)(f) is an appropriate legal basis for this processing activity.**

## 5. Ongoing Obligations

We commit to:

- Reviewing this assessment annually or when processing activities change materially
- Maintaining transparency about our processing in our Privacy Policy
- Responding to User requests within required timeframes
- Implementing appropriate technical and organizational security measures
- Honoring objection requests where technically feasible and legally required

## 6. Document History

| Date | Version | Change |
|------|---------|--------|
| 4 Dec, 2025 | 1.0 | Initial assessment |

---

**Approved by:** [Name and role]
**Review date:** [Date of next review]

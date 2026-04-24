# Test Invoice Files

Sample invoice documents for testing the AP Invoice Intake Agent during the hackathon.

## How to Use

1. Open any `.html` file in a browser
2. Press **Ctrl+P** → **Save as PDF** to create a PDF version
3. Upload the PDF (or screenshot as PNG/JPG) using the **Create Invoice** button in the app

## Available Test Invoices

| File | Vendor | Invoice # | Amount | Currency | Notes |
|------|--------|-----------|--------|----------|-------|
| `TCS_INV_4501.html` | Tata Consultancy Services | TCS/2026/INV-4501 | ₹24,50,000 | INR | SAP Implementation |
| `Infosys_Invoice_7821.html` | Infosys Ltd | INF-2026-03-7821 | ₹18,75,000 | INR | Cloud Migration + DevOps |
| `Wipro_SOC_Q1_2026.html` | Wipro Limited | WIP/FY26/00892 | ₹32,00,000 | INR | Managed SOC Services |
| `TechM_SF_Invoice_1134.html` | Tech Mahindra | TM-INV-2026-1134 | $45,000 | USD | Salesforce CRM |
| `HCL_ERP_Support_2026.html` | HCL Technologies | HCL-2026-APR-0056 | ₹47,50,000 | INR | Annual ERP Support |

## Testing Scenarios

- **Happy path**: Upload TCS or Infosys invoice → all 8 validation checks pass
- **Missing PO**: Create invoice without PO number → Exception
- **Invalid currency**: Use currency code "XYZ" → Exception
- **Negative amount**: Enter negative total → Exception
- **Duplicate**: Create same vendor + invoice number twice → Exception

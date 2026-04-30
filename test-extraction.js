#!/usr/bin/env node

/**
 * Test script to extract invoice data from a PDF using Azure Document Intelligence
 * Usage: node test-extraction.js <path-to-pdf>
 */

const fs = require('fs');
const https = require('https');

const endpoint = process.env.AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT || 'https://ai-hackathon-docintel-fe537.cognitiveservices.azure.com/';
const apiKey = process.env.AZURE_DOCUMENT_INTELLIGENCE_KEY || '';

if (!apiKey) {
    console.error('❌ AZURE_DOCUMENT_INTELLIGENCE_KEY is not set');
    console.error('Please set it in api/local.settings.json or as an environment variable');
    process.exit(1);
}

const pdfPath = process.argv[2];
if (!pdfPath) {
    console.error('Usage: node test-extraction.js <path-to-pdf>');
    process.exit(1);
}

if (!fs.existsSync(pdfPath)) {
    console.error(`❌ File not found: ${pdfPath}`);
    process.exit(1);
}

console.log(`\n📄 Reading file: ${pdfPath}`);
const fileBuffer = fs.readFileSync(pdfPath);
console.log(`   Size: ${(fileBuffer.length / 1024).toFixed(2)} KB`);

// Step 1: Submit document for analysis
console.log('\n🔄 Submitting document to Azure Document Intelligence...');
console.log(`   Endpoint: ${endpoint}`);
console.log(`   Model: prebuilt-invoice`);

const analyzeUrl = new URL('/formrecognizer/documentModels/prebuilt-invoice:analyze?api-version=2023-07-31', endpoint);

const options = {
    method: 'POST',
    headers: {
        'Content-Type': 'application/pdf',
        'Ocp-Apim-Subscription-Key': apiKey
    }
};

const req = https.request(analyzeUrl, options, (res) => {
    console.log(`   Status: ${res.statusCode} ${res.statusMessage}`);
    
    if (res.statusCode !== 202) {
        console.error('❌ Failed to submit document');
        res.on('data', (chunk) => console.error(chunk.toString()));
        return;
    }

    const resultUrl = res.headers['operation-location'];
    if (!resultUrl) {
        console.error('❌ No operation-location header in response');
        return;
    }

    console.log(`✅ Document submitted successfully`);
    console.log(`   Operation URL: ${resultUrl}`);

    // Step 2: Poll for results
    console.log('\n⏳ Waiting for analysis to complete...');
    pollResults(resultUrl);
});

req.on('error', (e) => {
    console.error(`❌ Request error: ${e.message}`);
});

req.write(fileBuffer);
req.end();

function pollResults(resultUrl, attempt = 1) {
    setTimeout(() => {
        const url = new URL(resultUrl);
        const options = {
            method: 'GET',
            headers: {
                'Ocp-Apim-Subscription-Key': apiKey
            }
        };

        https.get(url, options, (res) => {
            let data = '';
            res.on('data', (chunk) => data += chunk);
            res.on('end', () => {
                const result = JSON.parse(data);
                
                if (result.status === 'succeeded') {
                    console.log(`✅ Analysis complete (${attempt} attempts)\n`);
                    displayResults(result);
                } else if (result.status === 'failed') {
                    console.error(`❌ Analysis failed: ${result.error?.message || 'Unknown error'}`);
                } else {
                    process.stdout.write(`   Attempt ${attempt}: ${result.status}...\r`);
                    if (attempt < 30) {
                        pollResults(resultUrl, attempt + 1);
                    } else {
                        console.error('\n❌ Timeout waiting for results');
                    }
                }
            });
        }).on('error', (e) => {
            console.error(`❌ Poll error: ${e.message}`);
        });
    }, 1000);
}

function displayResults(result) {
    const doc = result.analyzeResult?.documents?.[0];
    if (!doc) {
        console.log('❌ No invoice document found in results');
        return;
    }

    console.log('═══════════════════════════════════════════════════════════');
    console.log('                    EXTRACTED INVOICE DATA                  ');
    console.log('═══════════════════════════════════════════════════════════\n');

    const fields = doc.fields || {};
    
    console.log('📋 BASIC INFORMATION:');
    console.log('─────────────────────────────────────────────────────────');
    printField('Vendor Name', fields.VendorName);
    printField('Invoice ID', fields.InvoiceId);
    printField('Invoice Date', fields.InvoiceDate);
    printField('Due Date', fields.DueDate);
    printField('Purchase Order', fields.PurchaseOrder);
    
    console.log('\n💰 AMOUNTS:');
    console.log('─────────────────────────────────────────────────────────');
    printField('Subtotal', fields.SubTotal);
    printField('Total Tax', fields.TotalTax);
    printField('Invoice Total', fields.InvoiceTotal);
    
    console.log('\n🏢 VENDOR DETAILS:');
    console.log('─────────────────────────────────────────────────────────');
    printField('Vendor Address', fields.VendorAddress);
    printField('Vendor Tax ID', fields.VendorTaxId);
    
    console.log('\n📦 LINE ITEMS:');
    console.log('─────────────────────────────────────────────────────────');
    if (fields.Items?.valueArray) {
        fields.Items.valueArray.forEach((item, idx) => {
            const itemFields = item.valueObject || {};
            console.log(`\nItem ${idx + 1}:`);
            printField('  Description', itemFields.Description, '  ');
            printField('  Quantity', itemFields.Quantity, '  ');
            printField('  Unit Price', itemFields.UnitPrice, '  ');
            printField('  Amount', itemFields.Amount, '  ');
        });
    } else {
        console.log('  No line items found');
    }

    console.log('\n📊 CONFIDENCE SCORES:');
    console.log('─────────────────────────────────────────────────────────');
    console.log(`  Overall Document: ${(doc.confidence * 100).toFixed(1)}%`);
    
    const confScores = [];
    Object.entries(fields).forEach(([key, field]) => {
        if (field.confidence && !key.includes('Address')) {
            confScores.push({ field: key, conf: field.confidence });
        }
    });
    confScores.sort((a, b) => b.conf - a.conf);
    confScores.slice(0, 10).forEach(({ field, conf }) => {
        console.log(`  ${field.padEnd(20)}: ${(conf * 100).toFixed(1)}%`);
    });

    console.log('\n═══════════════════════════════════════════════════════════\n');
}

function printField(label, field, indent = '') {
    if (!field) {
        console.log(`${indent}${label.padEnd(20)}: --`);
        return;
    }

    let value = '--';
    let confidence = '';

    if (field.content) {
        value = field.content;
    } else if (field.valueString) {
        value = field.valueString;
    } else if (field.valueNumber !== undefined) {
        value = field.valueNumber.toString();
    } else if (field.valueDate) {
        value = field.valueDate;
    } else if (field.valueCurrency) {
        value = `${field.valueCurrency.currencyCode || ''} ${field.valueCurrency.amount || ''}`.trim();
    } else if (field.valueAddress) {
        value = field.valueAddress.streetAddress || field.valueAddress.houseNumber || '--';
    }

    if (field.confidence) {
        confidence = ` (${(field.confidence * 100).toFixed(0)}%)`;
    }

    console.log(`${indent}${label.padEnd(20)}: ${value}${confidence}`);
}

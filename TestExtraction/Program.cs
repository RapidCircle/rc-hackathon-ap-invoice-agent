using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <path-to-pdf>");
            return;
        }

        var pdfPath = args[0];
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"File not found: {pdfPath}");
            return;
        }

        var endpoint = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT") 
            ?? "https://ai-hackathon-docintel-fe537.cognitiveservices.azure.com/";
        var key = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_KEY");

        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("ERROR: AZURE_DOCUMENT_INTELLIGENCE_KEY not set");
            return;
        }

        Console.WriteLine($"\n📄 Analyzing: {Path.GetFileName(pdfPath)}");
        Console.WriteLine($"   Size: {new FileInfo(pdfPath).Length / 1024.0:F2} KB");
        Console.WriteLine($"   Endpoint: {endpoint}\n");

        var client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));

        using var stream = File.OpenRead(pdfPath);
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", stream);
        var result = operation.Value;
        var doc = result.Documents.FirstOrDefault();

        if (doc == null)
        {
            Console.WriteLine("❌ No invoice found in document");
            return;
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("                  EXTRACTED INVOICE DATA                    ");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        PrintField("Vendor Name", doc.Fields, "VendorName");
        PrintField("Invoice ID", doc.Fields, "InvoiceId");
        PrintField("Invoice Date", doc.Fields, "InvoiceDate");
        PrintField("Due Date", doc.Fields, "DueDate");
        PrintField("PO Number", doc.Fields, "PurchaseOrder");
        PrintField("Subtotal", doc.Fields, "SubTotal");
        PrintField("Total Tax", doc.Fields, "TotalTax");
        PrintField("Invoice Total", doc.Fields, "InvoiceTotal");

        Console.WriteLine($"\n📊 Overall Confidence: {doc.Confidence * 100:F1}%");
        Console.WriteLine("\n═══════════════════════════════════════════════════════════\n");
    }

    static void PrintField(string label, IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            Console.WriteLine($"{label,-20}: --");
            return;
        }

        string value = field.FieldType switch
        {
            DocumentFieldType.String => field.Value.AsString(),
            DocumentFieldType.Date => field.Value.AsDate().ToString("yyyy-MM-dd"),
            DocumentFieldType.Currency => $"{field.Value.AsCurrency().CurrencyCode} {field.Value.AsCurrency().Amount:N2}",
            _ => field.Content
        };

        Console.WriteLine($"{label,-20}: {value} ({field.Confidence * 100:F0}%)");
    }
}

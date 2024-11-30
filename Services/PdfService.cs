// Services/PdfService.cs
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Source;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using e_commerce.Models;
using System.IO;
using System.Threading.Tasks;

namespace e_commerce.Services
{
    public class PdfService : IPdfService
    {
        public async Task<byte[]> GenerateInvoicePdfAsync(Order order, Address shippingAddress)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Set up fonts
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Add header
                Paragraph header = new Paragraph("INVOICE")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    .SetFont(boldFont);
                document.Add(header);

                // Add order details
                document.Add(new Paragraph($"Order ID: {order.Id}").SetFont(normalFont));
                document.Add(new Paragraph($"Order Date: {order.OrderDate:MMMM dd, yyyy HH:mm}").SetFont(normalFont));

                // Add shipping address
                Paragraph addressHeader = new Paragraph("Shipping Address:")
                    .SetFontSize(14)
                    .SetFont(boldFont);
                document.Add(addressHeader);
                
                document.Add(new Paragraph(
                    $"{shippingAddress.FullName}\n" +
                    $"{shippingAddress.StreetAddress}\n" +
                    $"{shippingAddress.City}, {shippingAddress.State} {shippingAddress.ZipCode}\n" +
                    $"Phone: {shippingAddress.PhoneNumber}")
                    .SetFont(normalFont));

                // Create table for order items
                Table table = new Table(4).UseAllAvailableWidth();
                
                // Add header cells with bold font
                Cell productHeader = new Cell().Add(new Paragraph("Product").SetFont(boldFont));
                Cell quantityHeader = new Cell().Add(new Paragraph("Quantity").SetFont(boldFont));
                Cell priceHeader = new Cell().Add(new Paragraph("Price").SetFont(boldFont));
                Cell totalHeader = new Cell().Add(new Paragraph("Total").SetFont(boldFont));

                table.AddCell(productHeader);
                table.AddCell(quantityHeader);
                table.AddCell(priceHeader);
                table.AddCell(totalHeader);

                // Add items
                foreach (var item in order.Items)
                {
                    table.AddCell(new Cell().Add(new Paragraph(item.ProductName).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString()).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("C")).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph(item.TotalPrice.ToString("C")).SetFont(normalFont)));
                }

                document.Add(table);

                // Add total amount
                Paragraph total = new Paragraph($"Total Amount: {order.TotalAmount:C}")
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFont(boldFont);
                document.Add(total);

                document.Close();

                return ms.ToArray();
            }
        }
    }
}
// Services/EmailService.cs
using System;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using e_commerce.Models;

namespace e_commerce.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IPdfService _pdfService;
        private const string StoreEmail = "bookstore.cs308@gmail.com";
        private const string StoreEmailPassword = "opxe wbgu uryn pnsz";

        public EmailService(ILogger<EmailService> logger, IPdfService pdfService)
        {
            _logger = logger;
            _pdfService = pdfService;
        }

        public async Task SendOrderConfirmationAsync(Order order, string userEmail, Address shippingAddress)
{
    var smtp = new SmtpClient
    {
        Host = "smtp.gmail.com",
        Port = 587,
        EnableSsl = true,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(StoreEmail, StoreEmailPassword)
    };

    using var message = new MailMessage
    {
        From = new MailAddress(StoreEmail, "Book Store"),
        Subject = $"Order Confirmation - Order #{order.Id}",
        IsBodyHtml = true,
        Body = GenerateEmailBody(order, shippingAddress)
    };

    // Generate and attach PDF invoice - removed try-catch to see the error
    byte[] pdfBytes = await _pdfService.GenerateInvoicePdfAsync(order, shippingAddress);
    using var pdfStream = new MemoryStream(pdfBytes);
    var attachment = new Attachment(pdfStream, $"Invoice-{order.Id}.pdf", "application/pdf");
    message.Attachments.Add(attachment);

    message.To.Add(userEmail);

    try
    {
        await smtp.SendMailAsync(message);
        _logger.LogInformation($"Order confirmation email sent successfully to {userEmail}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to send order confirmation email to {userEmail}");
        throw;
    }
}

        private string GenerateEmailBody(Order order, Address shippingAddress)
        {
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            
            // Header
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; margin-bottom: 20px;'>");
            body.AppendLine("<h2 style='color: #333; margin: 0;'>Thank you for your order!</h2>");
            body.AppendLine("</div>");

            // Order Information
            body.AppendLine("<div style='margin-bottom: 20px;'>");
            body.AppendLine($"<p><strong>Order ID:</strong> {order.Id}</p>");
            body.AppendLine($"<p><strong>Order Date:</strong> {order.OrderDate:MMMM dd, yyyy HH:mm}</p>");
            body.AppendLine("</div>");
            
            // Shipping Address
            body.AppendLine("<div style='background-color: #fff; padding: 15px; border: 1px solid #dee2e6; margin-bottom: 20px;'>");
            body.AppendLine("<h3 style='color: #333; margin-top: 0;'>Shipping Address:</h3>");
            body.AppendLine("<p style='margin-bottom: 0;'>");
            body.AppendLine($"{shippingAddress.FullName}<br>");
            body.AppendLine($"{shippingAddress.StreetAddress}<br>");
            body.AppendLine($"{shippingAddress.City}, {shippingAddress.State} {shippingAddress.ZipCode}<br>");
            body.AppendLine($"Phone: {shippingAddress.PhoneNumber}");
            body.AppendLine("</p>");
            body.AppendLine("</div>");

            // Order Details Table
            body.AppendLine("<h3 style='color: #333;'>Order Details:</h3>");
            body.AppendLine("<table style='width:100%; border-collapse: collapse; margin-bottom: 20px;'>");
            body.AppendLine("<tr style='background-color: #f8f9fa;'>");
            body.AppendLine("<th style='padding: 12px; border: 1px solid #dee2e6; text-align: left;'>Product</th>");
            body.AppendLine("<th style='padding: 12px; border: 1px solid #dee2e6; text-align: center;'>Quantity</th>");
            body.AppendLine("<th style='padding: 12px; border: 1px solid #dee2e6; text-align: right;'>Price</th>");
            body.AppendLine("<th style='padding: 12px; border: 1px solid #dee2e6; text-align: right;'>Total</th>");
            body.AppendLine("</tr>");

            foreach (var item in order.Items)
            {
                body.AppendLine("<tr>");
                body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{item.ProductName}</td>");
                body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6; text-align: center;'>{item.Quantity}</td>");
                body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6; text-align: right;'>{item.UnitPrice:C}</td>");
                body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6; text-align: right;'>{item.TotalPrice:C}</td>");
                body.AppendLine("</tr>");
            }

            // Total Amount
            body.AppendLine("<tr style='background-color: #f8f9fa;'>");
            body.AppendLine("<td colspan='3' style='padding: 12px; border: 1px solid #dee2e6; text-align: right;'><strong>Total Amount:</strong></td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6; text-align: right;'><strong>{order.TotalAmount:C}</strong></td>");
            body.AppendLine("</tr>");
            body.AppendLine("</table>");

            // PDF Notice
            body.AppendLine("<div style='background-color: #e9ecef; padding: 15px; margin-bottom: 20px; border-radius: 4px;'>");
            body.AppendLine("<p style='margin: 0;'><strong>Note:</strong> A detailed PDF invoice is attached to this email for your records.</p>");
            body.AppendLine("</div>");
            
            // Footer
            body.AppendLine("<div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #dee2e6;'>");
            body.AppendLine("<p style='color: #666;'>If you have any questions about your order, please contact us.</p>");
            body.AppendLine("<p style='color: #666;'>Thank you for shopping with us!</p>");
            body.AppendLine("</div>");

            body.AppendLine("</body></html>");

            return body.ToString();
        }
        public async Task SendRefundStatusEmailAsync(Order order, OrderItem item, string userEmail, string status)
        {
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(StoreEmail, StoreEmailPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(StoreEmail, "Book Store"),
                Subject = $"Refund Request {status} - Order #{order.Id}",
                IsBodyHtml = true,
                Body = GenerateRefundEmailBody(order, item, status)
            };

            message.To.Add(userEmail);

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation($"Refund status email sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send refund status email to {userEmail}");
                throw;
            }
        }

        private string GenerateRefundEmailBody(Order order, OrderItem item, string status)
        {
            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            
            // Header
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; margin-bottom: 20px;'>");
            body.AppendLine($"<h2 style='color: #333; margin: 0;'>Refund Request {status}</h2>");
            body.AppendLine("</div>");

            // Order Information
            body.AppendLine("<div style='margin-bottom: 20px;'>");
            body.AppendLine($"<p><strong>Order ID:</strong> {order.Id}</p>");
            body.AppendLine($"<p><strong>Product:</strong> {item.ProductName}</p>");
            body.AppendLine($"<p><strong>Quantity:</strong> {item.Quantity}</p>");
            body.AppendLine($"<p><strong>Refund Amount:</strong> {(item.Quantity * item.UnitPrice):C}</p>");
            body.AppendLine("</div>");
            
            // Status specific message
            body.AppendLine("<div style='background-color: #fff; padding: 15px; border: 1px solid #dee2e6; margin-bottom: 20px;'>");
            if (status == "Complete")
            {
                body.AppendLine("<p>Your refund request has been approved. The refund amount will be processed to your original payment method within 3-5 business days.</p>");
                body.AppendLine($"<p>Total refund amount: {(item.Quantity * item.UnitPrice):C}</p>");
            }
            else
            {
                body.AppendLine("<p>Your refund request has been reviewed and could not be approved at this time.</p>");
                body.AppendLine("<p>If you have any questions about this decision, please contact our customer service team.</p>");
            }
            body.AppendLine("</div>");
            
            // Footer
            body.AppendLine("<div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #dee2e6;'>");
            body.AppendLine("<p style='color: #666;'>If you have any questions, please don't hesitate to contact us.</p>");
            body.AppendLine("<p style='color: #666;'>Thank you for shopping with us!</p>");
            body.AppendLine("</div>");

            body.AppendLine("</body></html>");
            return body.ToString();
        }

    }
}
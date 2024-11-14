// Services/EmailService.cs
using System;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using e_commerce.Models;

namespace e_commerce.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private const string StoreEmail = "bookstore.cs308@gmail.com";
        private const string StoreEmailPassword = "opxe wbgu uryn pnsz";

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
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

            var message = new MailMessage
            {
                From = new MailAddress(StoreEmail, "Book Store"),
                Subject = $"Order Confirmation - Order #{order.Id}",
                IsBodyHtml = true,
                Body = GenerateEmailBody(order, shippingAddress)
            };

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
            body.AppendLine("<html><body>");
            body.AppendLine("<h2>Thank you for your order!</h2>");
            body.AppendLine($"<p>Order ID: {order.Id}</p>");
            body.AppendLine($"<p>Order Date: {order.OrderDate:MMMM dd, yyyy HH:mm}</p>");
            
            body.AppendLine("<h3>Shipping Address:</h3>");
            body.AppendLine("<p>");
            body.AppendLine($"{shippingAddress.FullName}<br>");
            body.AppendLine($"{shippingAddress.StreetAddress}<br>");
            body.AppendLine($"{shippingAddress.City}, {shippingAddress.State} {shippingAddress.ZipCode}<br>");
            body.AppendLine($"Phone: {shippingAddress.PhoneNumber}");
            body.AppendLine("</p>");

            body.AppendLine("<h3>Order Details:</h3>");
            body.AppendLine("<table style='width:100%; border-collapse: collapse;'>");
            body.AppendLine("<tr style='background-color: #f8f9fa;'>");
            body.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Product</th>");
            body.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Quantity</th>");
            body.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Price</th>");
            body.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Total</th>");
            body.AppendLine("</tr>");

            foreach (var item in order.Items)
            {
                body.AppendLine("<tr>");
                body.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6;'>{item.ProductName}</td>");
                body.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6;'>{item.Quantity}</td>");
                body.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6;'>{item.UnitPrice:C}</td>");
                body.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6;'>{item.TotalPrice:C}</td>");
                body.AppendLine("</tr>");
            }

            body.AppendLine("</table>");
            body.AppendLine($"<h3>Total Amount: {order.TotalAmount:C}</h3>");
            
            body.AppendLine("<p>If you have any questions about your order, please contact us.</p>");
            body.AppendLine("<p>Thank you for shopping with us!</p>");
            body.AppendLine("</body></html>");

            return body.ToString();
        }
    }
}
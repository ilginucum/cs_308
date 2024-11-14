// Services/IEmailService.cs
using e_commerce.Models;
using System.Threading.Tasks;
using e_commerce.Services;

namespace e_commerce.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order, string userEmail, Address shippingAddress);
    }
}
// Services/IPdfService.cs
using e_commerce.Models;
using System.IO;
using System.Threading.Tasks;

namespace e_commerce.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(Order order, Address shippingAddress);
    }
}
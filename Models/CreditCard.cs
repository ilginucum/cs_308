using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace e_commerce.Models
{
    public class CreditCard
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // Nullable olarak ayarlandı

        [Required(ErrorMessage = "Card number is required.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits.")]
        public string CardNumber { get; set; } = string.Empty; // Default değer verildi

       [Required(ErrorMessage = "Card holder name is required.")]
       [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Card holder name can only contain letters and spaces.")]
       public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiration month is required.")]
        [Range(1, 12, ErrorMessage = "Expiration month must be between 1 and 12.")]
        public int ExpirationMonth { get; set; }

        [Required(ErrorMessage = "Expiration year is required.")]
        [Range(2024, 2100, ErrorMessage = "Expiration year must be in the future.")]
        public int ExpirationYear { get; set; }

        [Required(ErrorMessage = "CVV is required.")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be a 3 or 4 digit number.")]
        public string CVV { get; set; } = string.Empty; // Default olarak boş dize atanıyor

    }
}

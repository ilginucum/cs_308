using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace e_commerce.Models
{
    public enum UserType
    {
        Customer,
        SalesManager,
        ProductManager
    }

    public class UserRegistration
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("FullName")]
        public string FullName { get; set; }

        [Required]
        [BsonElement("Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [BsonElement("Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [BsonElement("Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [BsonIgnore]
        public string ConfirmPassword { get; set; }

        [BsonElement("PhoneNumber")]
        public string PhoneNumber { get; set; }

        [Required]
        [EnumDataType(typeof(UserType))]
        [BsonElement("UserType")]
        [BsonRepresentation(BsonType.String)]
        public UserType UserType { get; set; }
    }
}
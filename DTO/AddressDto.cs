using System;
using System.ComponentModel.DataAnnotations;

namespace e_commerce.DTO
{
    public class AddressDto
    {
        [Required]
        public string StreetAddress { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a valid ZIP code")]
        public string ZipCode { get; set; }
    }

    public class UpdateAddressDto : AddressDto
    {
        [Required]
        public string Id { get; set; }
    }
}
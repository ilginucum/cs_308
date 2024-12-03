using System.Collections.Generic;

namespace e_commerce.Models
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } // Ürün detayları

        public double AverageScore { get; set; } // Ortalama puan
        public int TotalRatings { get; set; } // Toplam değerlendirme sayısı
        public int TotalComments { get; set; } // Toplam yorum sayısı

        // Her bir yıldız seviyesindeki oy sayısını tutan liste (1-5 yıldız)
        public Dictionary<int, int> RatingDistribution { get; set; } 

        public List<ProductComment> Comments { get; set; } // Yorumlar listesi

    }
}

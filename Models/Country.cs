using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DataInfo.Models
{
    public class Country
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int country_id { get; set; }

        [Required]
        [Display(Name = "Country : ")]
        public string CountryName { get; set; }
    }

    public class State
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int state_id { get; set; }

        [Required]
        [Display(Name = "State : ")]
        public string StateName { get; set; }

        public int country_id { get; set; }

        [ForeignKey("country_id")]
        public virtual Country Country { get; set; }

    }

    public class City
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int city_id { get; set; }

        [Required(ErrorMessage = "A city is required")]
        [Display(Name = "City : ")]
        public string CityName { get; set; }

        public int state_id { get; set; }

        [ForeignKey("state_id")]
        public virtual State State { get; set; }

    }
}

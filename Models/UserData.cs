using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataInfo.Models
{
    [Table("UserDatas")]
    public class UserData
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int user_id { get; set; }

        [NotMapped]
        public int iid { get; set; }

        [Display(Name = "First Name : ")]
        [Required(ErrorMessage = "A First Name is required.")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "First Name can only contain letters.")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name : ")]
        [Required(ErrorMessage = "A Last Name is required.")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Last Name can only contain letters.")]
        public string LastName { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "An Email is required.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }


        [Display(Name = "Mobile No : ")]
        [Required(ErrorMessage = "A Mobile Number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must contain 10 digits.")]
        public string MobileNo { get; set; }


        [Display(Name = "Gender : ")]
        [Required(ErrorMessage = "A Gender is required.")]
        [EnumDataType(typeof(Gender))]
        [NotMapped]
        public Gender Gender1
        {
            get => Enum.TryParse(Gender_, out Gender gender) ? gender : Gender.Other;
            set => Gender_ = value.ToString();
        }

        public string Gender_ { get; set; }

        [Display(Name = "Date of Birth : ")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "A Dob is required.")]
        public string Dob { get; set; }

        [Display(Name = "Password : ")]
        [Required(ErrorMessage = "A Password is required.")]
        [MinLength(6, ErrorMessage = "A Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped]
        [Display(Name = "Confirm Password : ")]
        [Required(ErrorMessage = "A Confirm Password is required.")]
        [MinLength(6, ErrorMessage = "A Confirm Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Address : ")]
        [Required(ErrorMessage = "An Address is required.")]
        public String Address { get; set; }

        [Display(Name = "Image : ")]
        public string ImagePath { get; set; }

        [NotMapped]
        [Display(Name = "Select a Country")]
        [Required(ErrorMessage = "Country is required")]
        public int SelectedCountryId { get; set; }


        [NotMapped]
        [Display(Name = "Select a State")]
        [Required(ErrorMessage = "A state is required")]
        public int SelectedStateId { get; set; }

        [Display(Name = "Select a city")]
        [Required(ErrorMessage = "A City is required")]
        public int SelectedCityId { get; set; }

        [NotMapped]
        public string SelectedCountry { get; set; }

        [NotMapped]
        public string selectedState { get; set; }

        [NotMapped]
        public string selectedCity { get; set; }
        [NotMapped]
        public IEnumerable<SelectListItem> CountryList { get; set; }
        [NotMapped]
        public IEnumerable<SelectListItem> StateList { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem> CityList { get; set; }
        public Boolean IsActive { get; set; }

//
        [NotMapped]
        [JsonIgnore]
        public IFileHttpResult file { get; set; }

    }
    public enum Gender
    {
        Male,
        Female,
        Other
    }
}

namespace CRM_Api.Models
{
    public class Location
    {
        public class Country
        {
            public int Id { get; set; }
            public string CountryName { get; set; }
            public string Status { get; set; }
        }

        public class State
        {
            public int Id { get; set; }
            public string StateName { get; set; }
            public int CountryId { get; set; }
            public string CountryName { get; set; }
            public string Status { get; set; }
        }

        public class AddStateDto
        {
            public string StateName { get; set; }
            public int CountryId { get; set; }
            public string Status { get; set; }
        }

        public class City
        {
            public int Id { get; set; }
            public string CityName { get; set; }
            public int StateId { get; set; }
            public int CountryId { get; set; }
            public string StateName { get; set; }
            public string CountryName { get; set; }
            public string Status { get; set; }

        }
        public class AddCityDto
        {
            public string CityName { get; set; }
            public int StateId { get; set; }
            public int CountryId { get; set; }
            public string Status { get; set; }
        }

    }
}

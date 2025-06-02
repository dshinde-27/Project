using CRM_Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics.Metrics;
using static CRM_Api.Models.Location;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : Controller
    {
        private readonly string _connectionString;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        // ================= COUNTRY =================
        public LocationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Connection"); ;
        }

        [HttpGet("GetCountries")]
        public IActionResult GetCountries()
        {
            var countries = new List<Country>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var query = "SELECT Id, Country, Status FROM Country";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        countries.Add(new Country
                        {
                            Id = (int)reader["Id"],
                            CountryName = reader["Country"].ToString(),
                            Status = reader["Status"].ToString(),
                        });
                    }
                }
            }
            return Ok(countries);
        }

        [HttpPost("AddCountry")]
        public IActionResult AddCountry([FromBody] Country country)
        {
            if (country == null || string.IsNullOrWhiteSpace(country.CountryName))
                return BadRequest("Valid country data is required.");

            string checkQuery = "SELECT COUNT(*) FROM Country WHERE Country = @Country";
            SqlParameter[] checkParams = {
            new SqlParameter("@Country", country.CountryName)
            };

            var count = Convert.ToInt32(SqlHelper.ExecuteScalar(checkQuery, checkParams));

            if (count > 0)
            {
                return Conflict(new { message = "Country already exists." });
            }

            string insertQuery = "INSERT INTO Country (Country, Status) VALUES (@Country, @Status)";
            SqlParameter[] insertParams = {
            new SqlParameter("@Country", country.CountryName),
            new SqlParameter("@Status", country.Status)
            };

            SqlHelper.ExecuteNonQuery(insertQuery, insertParams);

            return Ok(new { message = "Country added successfully." });
        }

        [HttpDelete("DeleteCountry/{id}")]
        public IActionResult DeleteCountry(int id)
        {
            string checkQuery = "SELECT COUNT(*) FROM Country WHERE Id = @Id";
            SqlParameter[] checkParams = {
        new SqlParameter("@Id", id)
    };

            int count = Convert.ToInt32(SqlHelper.ExecuteScalar(checkQuery, checkParams));

            if (count == 0)
            {
                return NotFound(new { message = "Country not found." });
            }

            // Recreate parameters to avoid ArgumentException
            SqlParameter[] deleteParams = {
        new SqlParameter("@Id", id)
    };

            string deleteQuery = "DELETE FROM Country WHERE Id = @Id";
            SqlHelper.ExecuteNonQuery(deleteQuery, deleteParams);

            return Ok(new { message = "Country deleted successfully." });
        }


        // ================= STATE =================

        [HttpGet("GetStates")]
        public IActionResult GetStates()
        {
            var states = new List<State>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var query = @"SELECT S.Id, S.State, C.Country AS CountryName, S.Status 
                              FROM State S
                              JOIN Country C ON S.CountryId = C.Id";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        states.Add(new State
                        {
                            Id = (int)reader["Id"],
                            StateName = reader["State"].ToString(),
                            CountryName = reader["CountryName"].ToString(),
                            Status = reader["Status"].ToString(),
                        });
                    }
                }
            }

            return Ok(states);
        }

        [HttpPost("AddState")]
        public IActionResult AddState([FromBody] AddStateDto state)
        {
            if (state == null) return BadRequest("State data is required.");

            string countryCheckQuery = "SELECT COUNT(*) FROM Country WHERE Id = @CountryId";
            SqlParameter[] countryCheckParams = {
            new SqlParameter("@CountryId", state.CountryId)
            };
            int countryExists = Convert.ToInt32(SqlHelper.ExecuteScalar(countryCheckQuery, countryCheckParams));

            if (countryExists == 0)
            {
                return NotFound(new { message = "Country not found. Cannot add state." });
            }

            string insertQuery = @"INSERT INTO State (State, CountryId, Status) 
                           VALUES (@State, @CountryId, @Status)";
            SqlParameter[] insertParams = {
                new SqlParameter("@State", state.StateName),
                new SqlParameter("@CountryId", state.CountryId),
                new SqlParameter("@Status", state.Status)
                };

            SqlHelper.ExecuteNonQuery(insertQuery, insertParams);

            return Ok(new { message = "State added successfully." });
        }

        [HttpDelete("DeleteState/{id}")]
        public IActionResult DeleteState(int id)
        {
            string checkQuery = "SELECT COUNT(*) FROM State WHERE Id = @Id";
            SqlParameter[] checkParams = {
                new SqlParameter("@Id", id)
                };
            int exists = Convert.ToInt32(SqlHelper.ExecuteScalar(checkQuery, checkParams));

            if (exists == 0)
            {
                return NotFound(new { message = "State not found." });
            }

            string deleteQuery = "DELETE FROM State WHERE Id = @Id";
            SqlHelper.ExecuteNonQuery(deleteQuery, checkParams);

            return Ok(new { message = "State deleted successfully." });
        }

        // ================= CITY =================

        [HttpGet("GetCities")]
        public IActionResult GetCities()
        {
            var cities = new List<City>();
            string query = @"
                SELECT CI.Id, CI.City, CI.Status, 
                S.State AS StateName, C.Country AS CountryName
                FROM City CI
                JOIN State S ON CI.StateId = S.Id
                JOIN Country C ON CI.CountryId = C.Id";

            using (var reader = SqlHelper.ExecuteReader(query, null))
            {
                while (reader.Read())
                {
                    cities.Add(new City
                    {
                        Id = (int)reader["Id"],
                        CityName = reader["City"].ToString(),
                        StateName = reader["StateName"].ToString(),
                        CountryName = reader["CountryName"].ToString(),
                        Status = reader["Status"].ToString(),
                    });
                }
            }

            return Ok(cities);
        }

        [HttpPost("AddCity")]
        public IActionResult AddCity([FromBody] AddCityDto city)
        {
            if (city == null) return BadRequest("City data is required.");

            var countryExists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Country WHERE Id = @CountryId",
                new[] { new SqlParameter("@CountryId", city.CountryId) }));

            if (countryExists == 0)
                return NotFound(new { message = "Country not found." });

            var stateExists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM State WHERE Id = @StateId",
                new[] { new SqlParameter("@StateId", city.StateId) }));

            if (stateExists == 0)
                return NotFound(new { message = "State not found." });
            var insertQuery = @"INSERT INTO City (City, StateId, CountryId, Status)
                        VALUES (@City, @StateId, @CountryId, @Status)";
            SqlHelper.ExecuteNonQuery(insertQuery, new[]
            {
        new SqlParameter("@City", city.CityName),
        new SqlParameter("@StateId", city.StateId),
        new SqlParameter("@CountryId", city.CountryId),
        new SqlParameter("@Status", city.Status)
    });

            return Ok(new { message = "City added successfully." });
        }

        [HttpDelete("DeleteCity/{id}")]
        public IActionResult DeleteCity(int id)
        {
            // Check if city exists
            int exists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM City WHERE Id = @Id",
                new[] { new SqlParameter("@Id", id) }));

            if (exists == 0)
                return NotFound(new { message = "City not found." });

            // Delete city
            SqlHelper.ExecuteNonQuery(
                "DELETE FROM City WHERE Id = @Id",
                new[] { new SqlParameter("@Id", id) });

            return Ok(new { message = "City deleted successfully." });
        }
    }
}

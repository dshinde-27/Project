using CRM_Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static CRM_Api.Models.Location;

namespace CRM_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : Controller
    {
        private readonly string _connectionString;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public LocationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Connection");
        }

        // ================= COUNTRY =================

        [HttpGet("GetCountries")]
        public IActionResult GetCountries()
        {
            Logger.Info("Fetching country list.");
            var countries = new List<Country>();

            try
            {
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
                Logger.Info("Fetched " + countries.Count + " countries.");
                return Ok(countries);
            }
            catch (Exception ex)
            {
                Logger.Error("Error fetching countries: " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("AddCountry")]
        public IActionResult AddCountry([FromBody] Country country)
        {
            Logger.Info("Attempting to add country: '" + country?.CountryName + "'");

            if (country == null || string.IsNullOrWhiteSpace(country.CountryName))
            {
                Logger.Warn("Invalid country data submitted.");
                return BadRequest("Valid country data is required.");
            }

            try
            {
                string checkQuery = "SELECT COUNT(*) FROM Country WHERE Country = @Country";
                SqlParameter[] checkParams = {
                    new SqlParameter("@Country", country.CountryName)
                };

                var count = Convert.ToInt32(SqlHelper.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    Logger.Warn("Country already exists: '" + country.CountryName + "'");
                    return Conflict(new { message = "Country already exists." });
                }

                string insertQuery = "INSERT INTO Country (Country, Status) VALUES (@Country, @Status)";
                SqlParameter[] insertParams = {
                    new SqlParameter("@Country", country.CountryName),
                    new SqlParameter("@Status", country.Status)
                };

                SqlHelper.ExecuteNonQuery(insertQuery, insertParams);

                Logger.Info("Country '" + country.CountryName + "' added successfully.");
                return Ok(new { message = "Country added successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error("Error adding country '" + country.CountryName + "': " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("DeleteCountry/{id}")]
        public IActionResult DeleteCountry(int id)
        {
            Logger.Info("Attempting to delete country with ID: " + id);
            try
            {
                string checkQuery = "SELECT COUNT(*) FROM Country WHERE Id = @Id";
                SqlParameter[] checkParams = { new SqlParameter("@Id", id) };

                int count = Convert.ToInt32(SqlHelper.ExecuteScalar(checkQuery, checkParams));

                if (count == 0)
                {
                    Logger.Warn("Country ID not found: " + id);
                    return NotFound(new { message = "Country not found." });
                }

                string deleteQuery = "DELETE FROM Country WHERE Id = @Id";
                SqlHelper.ExecuteNonQuery(deleteQuery, checkParams);

                Logger.Info("Country with ID " + id + " deleted successfully.");
                return Ok(new { message = "Country deleted successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting country ID " + id + ": " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        // ================= STATE =================

        [HttpGet("GetStates")]
        public IActionResult GetStates()
        {
            Logger.Info("Fetching state list.");
            var states = new List<State>();

            try
            {
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

                Logger.Info("Fetched " + states.Count + " states.");
                return Ok(states);
            }
            catch (Exception ex)
            {
                Logger.Error("Error fetching states: " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("AddState")]
        public IActionResult AddState([FromBody] AddStateDto state)
        {
            Logger.Info("Adding state: '" + state?.StateName + "' to Country ID: " + state?.CountryId);
            if (state == null)
            {
                Logger.Warn("Null state submitted.");
                return BadRequest("State data is required.");
            }

            try
            {
                string countryCheckQuery = "SELECT COUNT(*) FROM Country WHERE Id = @CountryId";
                SqlParameter[] countryCheckParams = {
                    new SqlParameter("@CountryId", state.CountryId)
                };
                int countryExists = Convert.ToInt32(SqlHelper.ExecuteScalar(countryCheckQuery, countryCheckParams));

                if (countryExists == 0)
                {
                    Logger.Warn("Country ID not found: " + state.CountryId);
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

                Logger.Info("State '" + state.StateName + "' added successfully.");
                return Ok(new { message = "State added successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error("Error adding state: " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("DeleteState/{id}")]
        public IActionResult DeleteState(int id)
        {
            Logger.Info("Attempting to delete state with ID: " + id);
            try
            {
                string checkQuery = "SELECT COUNT(*) FROM State WHERE Id = @Id";
                SqlParameter[] checkParams = { new SqlParameter("@Id", id) };
                int exists = Convert.ToInt32(SqlHelper.ExecuteScalar(checkQuery, checkParams));

                if (exists == 0)
                {
                    Logger.Warn("State ID not found: " + id);
                    return NotFound(new { message = "State not found." });
                }

                string deleteQuery = "DELETE FROM State WHERE Id = @Id";
                SqlHelper.ExecuteNonQuery(deleteQuery, checkParams);

                Logger.Info("State with ID " + id + " deleted successfully.");
                return Ok(new { message = "State deleted successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting state ID " + id + ": " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        // ================= CITY =================

        [HttpGet("GetCities")]
        public IActionResult GetCities()
        {
            Logger.Info("Fetching city list.");
            var cities = new List<City>();
            string query = @"
                SELECT CI.Id, CI.City, CI.Status, 
                       S.State AS StateName, C.Country AS CountryName
                FROM City CI
                JOIN State S ON CI.StateId = S.Id
                JOIN Country C ON CI.CountryId = C.Id";

            try
            {
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

                Logger.Info("Fetched " + cities.Count + " cities.");
                return Ok(cities);
            }
            catch (Exception ex)
            {
                Logger.Error("Error fetching cities: " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("AddCity")]
        public IActionResult AddCity([FromBody] AddCityDto city)
        {
            Logger.Info("Adding city: '" + city?.CityName + "'");

            if (city == null)
            {
                Logger.Warn("Null city submitted.");
                return BadRequest("City data is required.");
            }

            try
            {
                int countryExists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM Country WHERE Id = @CountryId",
                    new[] { new SqlParameter("@CountryId", city.CountryId) }));

                if (countryExists == 0)
                {
                    Logger.Warn("Country ID not found: " + city.CountryId);
                    return NotFound(new { message = "Country not found." });
                }

                int stateExists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM State WHERE Id = @StateId",
                    new[] { new SqlParameter("@StateId", city.StateId) }));

                if (stateExists == 0)
                {
                    Logger.Warn("State ID not found: " + city.StateId);
                    return NotFound(new { message = "State not found." });
                }

                var insertQuery = @"INSERT INTO City (City, StateId, CountryId, Status)
                                    VALUES (@City, @StateId, @CountryId, @Status)";
                SqlHelper.ExecuteNonQuery(insertQuery, new[]
                {
                    new SqlParameter("@City", city.CityName),
                    new SqlParameter("@StateId", city.StateId),
                    new SqlParameter("@CountryId", city.CountryId),
                    new SqlParameter("@Status", city.Status)
                });

                Logger.Info("City '" + city.CityName + "' added successfully.");
                return Ok(new { message = "City added successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error("Error adding city: " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("DeleteCity/{id}")]
        public IActionResult DeleteCity(int id)
        {
            Logger.Info("Attempting to delete city with ID: " + id);
            try
            {
                int exists = Convert.ToInt32(SqlHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM City WHERE Id = @Id",
                    new[] { new SqlParameter("@Id", id) }));

                if (exists == 0)
                {
                    Logger.Warn("City ID not found: " + id);
                    return NotFound(new { message = "City not found." });
                }

                SqlHelper.ExecuteNonQuery(
                    "DELETE FROM City WHERE Id = @Id",
                    new[] { new SqlParameter("@Id", id) });

                Logger.Info("City with ID " + id + " deleted successfully.");
                return Ok(new { message = "City deleted successfully." });
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting city ID " + id + ": " + ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace Organify.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=Organify Project;Trusted_Connection=True;";

        // GET: Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Users WHERE (Username = @Username OR Email = @Username) AND Password = @Password";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Store user details in session
                        HttpContext.Session.SetString("UserRole", reader["Role"].ToString());
                        HttpContext.Session.SetString("Username", reader["Username"].ToString());
                        HttpContext.Session.SetString("Email", reader["Email"].ToString());

                        // Redirect based on role
                        if (reader["Role"].ToString() == "Admin")
                            return RedirectToAction("Index", "Admin");
                        else
                            return RedirectToAction("Home", "Home");
                    }
                }
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        // GET: Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        public IActionResult Register(string username, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Check if the email or username is already taken
                var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email OR Username = @Username";
                var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", email);
                checkCommand.Parameters.AddWithValue("@Username", username);

                var count = (int)checkCommand.ExecuteScalar();
                if (count > 0)
                {
                    ViewBag.Error = "Email or Username is already taken!";
                    return View();
                }

                // Add the user
                var insertQuery = "INSERT INTO Users (Username, Email, Password, Role) VALUES (@Username, @Email, @Password, @Role)";
                var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Username", username);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@Password", password);
                insertCommand.Parameters.AddWithValue("@Role", "Customer"); 

                insertCommand.ExecuteNonQuery();
            }

            
            return RedirectToAction("Login");
        }

    
        public IActionResult Logout()
        {
            
            HttpContext.Session.Clear();
            return RedirectToAction("Home", "Home");
        }


        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }


        [HttpPost]
        public IActionResult UpdateProfile(string username, string email, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Update query
                var query = "UPDATE Users SET Username = @Username, Email = @Email" +
                            (string.IsNullOrEmpty(password) ? "" : ", Password = @Password") +
                            " WHERE Username = @CurrentUsername";

                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@CurrentUsername", HttpContext.Session.GetString("Username"));

                if (!string.IsNullOrEmpty(password))
                {
                    command.Parameters.AddWithValue("@Password", password);
                }

                command.ExecuteNonQuery();
            }

            // Update session
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("Email", email);

            TempData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }



        

    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Organify.Models;
using System.Data;

namespace Organify.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=Organify Project;Trusted_Connection=True;";

        private bool IsAuthorized()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && role == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public IActionResult Notifier()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Notifier(string Message)
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                TempData["Error"] = "Message cannot be empty!";
                return RedirectToAction("Notifier"); // Stay on the same page
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "INSERT INTO Hubs (Message) VALUES (@Message)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Message", Message);
                        command.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Notification sent successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Notifier"); 
        }




        public IActionResult AddProduct()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult AddProduct(Product product, IFormFile ImageFile)
        {
            try
            {
                // Validate product input
                if (product == null || string.IsNullOrEmpty(product.Name) || product.Price <= 0)
                {
                    TempData["Error"] = "Invalid product details.";
                    return RedirectToAction("Product");
                }

                // Process Image Upload
                string imageUrl = null;
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }
                    imageUrl = "/images/" + fileName;
                }

                // Database Insert
                using (var connection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=Organify Project;Trusted_Connection=True;"))
                {
                    connection.Open();

                    var query = @"INSERT INTO Products (Name, Description, Price, ImageUrl, Category) 
                          VALUES (@Name, @Description, @Price, @ImageUrl, @Category)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", product.Name);
                        command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Price", product.Price);
                        command.Parameters.AddWithValue("@ImageUrl", (object)imageUrl ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Category", product.Category ?? (object)DBNull.Value);

                        var rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Product added successfully!";
                            return RedirectToAction("Product");
                        }
                        else
                        {
                            TempData["Error"] = "Failed to add the product. Please try again.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Product");
        }

        public IActionResult Product()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            var products = new List<Product>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, Description, Price, ImageUrl, Category FROM Products";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"].ToString(),
                                Price = (decimal)reader["Price"],
                                ImageUrl = reader["ImageUrl"]?.ToString(),
                                Category = reader["Category"]?.ToString()
                            });
                        }
                    }
                }
            }

            return View(products);
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "DELETE FROM Products WHERE Id = @Id";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                TempData["Message"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Product");
        }

        public IActionResult EditProduct(int id)
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            Product product = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, Description, Price, ImageUrl, Category FROM Products WHERE Id = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            product = new Product
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"].ToString(),
                                Price = (decimal)reader["Price"],
                                ImageUrl = reader["ImageUrl"]?.ToString(),
                                Category = reader["Category"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        [HttpPost]
        public IActionResult EditProduct(Product product, IFormFile ImageFile)
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                string imageUrl = product.ImageUrl;

                // Update Image if a new file is provided
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }
                    imageUrl = "/images/" + fileName;
                }

                // Update product details in the database
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"UPDATE Products 
                          SET Name = @Name, Description = @Description, Price = @Price, ImageUrl = @ImageUrl, Category = @Category 
                          WHERE Id = @Id";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", product.Name);
                        command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Price", product.Price);
                        command.Parameters.AddWithValue("@ImageUrl", imageUrl ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Category", product.Category ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Id", product.Id);

                        command.ExecuteNonQuery();
                    }
                }

                TempData["Message"] = "Product updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Product");
        }


        public IActionResult Profile()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            var username = HttpContext.Session.GetString("Username");
            var admin = new User();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Username, Email FROM Users WHERE Username = @Username";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            admin.Username = reader["Username"].ToString();
                            admin.Email = reader["Email"].ToString();
                        }
                    }
                }
            }

            return View(admin);
        }

        [HttpPost]
        public IActionResult UpdateProfile(string email)
        {
            var username = HttpContext.Session.GetString("Username");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "UPDATE Users SET Email = @Email WHERE Username = @Username";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Username", username);
                    command.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var username = HttpContext.Session.GetString("Username");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Password FROM Users WHERE Username = @Username";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    var dbPassword = command.ExecuteScalar()?.ToString();

                    if (dbPassword != currentPassword)
                    {
                        TempData["Message"] = "Current password is incorrect!";
                        return RedirectToAction("Profile");
                    }
                }

                var updateQuery = "UPDATE Users SET Password = @Password WHERE Username = @Username";
                using (var updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@Password", newPassword);
                    updateCommand.Parameters.AddWithValue("@Username", username);
                    updateCommand.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }



        public IActionResult Orders()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = new List<Order>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT o.Id, u.Username, u.Email, o.OrderDate, o.TotalAmount, o.ShippingAddress FROM Orders o JOIN Users u ON o.UserId = u.Id";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new Order
                            {
                                Id = (int)reader["Id"],
                                User = new User
                                {
                                    Username = reader["Username"].ToString(),
                                    Email = reader["Email"].ToString()
                                },
                                OrderDate = (DateTime)reader["OrderDate"],
                                TotalAmount = (decimal)reader["TotalAmount"],
                                ShippingAddress = reader["ShippingAddress"].ToString()
                            });
                        }
                    }
                }
            }

            return View(orders);
        }

        public IActionResult ContactMessages()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Account");
            }

            var contactMessages = new List<ContactMessage>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, Email, Phone, Message, SubmittedAt FROM ContactMessages";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            contactMessages.Add(new ContactMessage
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Message = reader["Message"].ToString(),
                                SubmittedAt = (DateTime)reader["SubmittedAt"]
                            });
                        }
                    }
                }
            }

            return View(contactMessages);
        }


    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Organify.Models;
using System.Collections.Generic;

namespace Organify.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=\"Organify Project\";Integrated Security=True;";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Notify()
        {
            var messages = new List<HubsMdl>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Message FROM Hubs ORDER BY Id DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new HubsMdl
                            {
                                Message = reader["Message"].ToString()
                            });
                        }
                    }
                }
            }

            return View(messages);
        }



        public IActionResult Home()
        {
            var featuredProducts = GetFeaturedProducts();
            ViewBag.FeaturedProducts = featuredProducts;
            return View();
        }

        private List<Product> GetFeaturedProducts()
        {
            var products = new List<Product>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT TOP 3 Id, Name, Description, Price, ImageUrl, Category FROM Products";

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
                                ImageUrl = reader["ImageUrl"]?.ToString() ?? "/images/default.jpg",
                                Category = reader["Category"]?.ToString()
                            });
                        }
                    }
                }
            }
            return products;
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Contact(string name, string email, string phone, string message)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO ContactMessages (Name, Email, Phone, Message) VALUES (@Name, @Email, @Phone, @Message)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Message", message);
                    command.ExecuteNonQuery();
                }
            }
            TempData["Message"] = "Thank you for contacting us! We will get back to you soon.";
            return RedirectToAction("Contact");
        }

        public IActionResult About()
        {
            return View();
        }

    }

}

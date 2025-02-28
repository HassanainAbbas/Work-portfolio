using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Organify.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Organify.Controllers
{
    public class ProductController : Controller
    {
        private readonly string _connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=Organify Project;Trusted_Connection=True;";

        // Index action to display product catalog with sorting and filtering
        public IActionResult Index(string sortBy = "", string category = "All")
        {
            // Fetch products
            var products = GetAllProducts();

            // Filter by category
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                products = products.Where(p => p.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // Sort by price
            products = sortBy switch
            {
                "price-asc" => products.OrderBy(p => p.Price).ToList(),
                "price-desc" => products.OrderByDescending(p => p.Price).ToList(),
                _ => products
            };

            // Pass data to the view
            ViewBag.SortBy = sortBy;
            ViewBag.Category = category;
            ViewBag.Categories = new List<string> { "Fruits", "Vegetables", "Dairy", "Other" };

            return View(products);
        }

        // Details action to display product details
        public IActionResult Details(int id)
        {
            var product = GetProductById(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            return View(product);
        }

        // Fetch all products from the database
        private List<Product> GetAllProducts()
        {
            var products = new List<Product>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, Description, Price, ImageUrl, Category FROM Products WHERE Name IS NOT NULL";
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

        // Fetch a single product by ID
        private Product GetProductById(int id)
        {
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
                            return new Product
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"].ToString(),
                                Price = (decimal)reader["Price"],
                                ImageUrl = reader["ImageUrl"]?.ToString() ?? "/images/default.jpg",
                                Category = reader["Category"]?.ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}
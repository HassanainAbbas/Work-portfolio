using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Organify.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Organify.Extensions;

namespace Organify.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";
        private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Organify Project;Trusted_Connection=True;";

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        }

        public IActionResult Index()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            return View(cart);
        }

        public IActionResult AddToCart(int productId)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var product = GetProductById(productId);
            if (product == null)
            {
                TempData["Message"] = "Product not found.";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(item => item.Product.Id == product.Id);

            if (cartItem != null)
            {
                cartItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem { Product = product, Quantity = 1 });
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            TempData["Message"] = $"{product.Name} added to cart successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int productId)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            cart.RemoveAll(item => item.Product.Id == productId);
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);

            TempData["Message"] = "Product removed from cart successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            if (quantity < 1)
            {
                TempData["Message"] = "Quantity must be at least 1.";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(item => item.Product.Id == productId);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            TempData["Message"] = "Cart updated successfully!";
            return RedirectToAction("Index");
        }

        private Product GetProductById(int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, Description, Price FROM Products WHERE Id = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", productId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Product
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"].ToString(),
                                Price = (decimal)reader["Price"]
                            };
                        }
                    }
                }
            }

            return null;
        }

        [HttpPost]
        public IActionResult CheckOut(string shippingAddress)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                TempData["Message"] = "Your cart is empty!";
                return RedirectToAction("Index");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Fetch logged-in user's details
                string username = HttpContext.Session.GetString("Username");
                string userQuery = "SELECT Id FROM Users WHERE Username = @Username";
                int userId;

                using (var userCommand = new SqlCommand(userQuery, connection))
                {
                    userCommand.Parameters.AddWithValue("@Username", username);
                    userId = (int)userCommand.ExecuteScalar();
                }

                // Insert Order
                string orderQuery = "INSERT INTO Orders (UserId, OrderDate, TotalAmount, ShippingAddress) OUTPUT INSERTED.Id VALUES (@UserId, @OrderDate, @TotalAmount, @ShippingAddress)";
                int orderId;

                using (var orderCommand = new SqlCommand(orderQuery, connection))
                {
                    orderCommand.Parameters.AddWithValue("@UserId", userId);
                    orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    orderCommand.Parameters.AddWithValue("@TotalAmount", cart.Sum(item => item.Product.Price * item.Quantity));
                    orderCommand.Parameters.AddWithValue("@ShippingAddress", shippingAddress);
                    orderId = (int)orderCommand.ExecuteScalar();
                }

                // Insert Order Items
                foreach (var item in cart)
                {
                    string orderItemQuery = "INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price) VALUES (@OrderId, @ProductId, @Quantity, @Price)";
                    using (var itemCommand = new SqlCommand(orderItemQuery, connection))
                    {
                        itemCommand.Parameters.AddWithValue("@OrderId", orderId);
                        itemCommand.Parameters.AddWithValue("@ProductId", item.Product.Id);
                        itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                        itemCommand.Parameters.AddWithValue("@Price", item.Product.Price);
                        itemCommand.ExecuteNonQuery();
                    }
                }
            }

            // Clear cart
            HttpContext.Session.Remove("Cart");

            TempData["Message"] = "Order placed successfully!";
            return RedirectToAction("Index");
        }


    }
}

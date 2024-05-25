using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Utility;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = _unitOfWork.product.GetAll(includeProperties: "Category");
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if(userId != null)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(item => item.Id == userId.Value).Count());
            }
            return View(products);
        }

        public IActionResult Details(int id)
        {

            ShoppingCart shoppingCart = new ShoppingCart()
            {
                Product = _unitOfWork.product.Get(u => u.ProductId == id, "Category"),
                Count = 1,
                ProductId = id

            };

            return View(shoppingCart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.Id = userId;
            ShoppingCart shoppingFromDb = _unitOfWork.ShoppingCart.Get(item => item.Id ==  userId && item.ProductId == shoppingCart.ProductId);
            if(shoppingFromDb != null)
            {
                shoppingFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(shoppingFromDb);
                _unitOfWork.Save();
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(item => item.Id == userId).Count());
                _unitOfWork.Save();
                
            }
            TempData["success"] = "The Cart Updated Successfully!";
            return RedirectToAction(nameof(Index));

        }


        public IActionResult Privacy()
        {

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

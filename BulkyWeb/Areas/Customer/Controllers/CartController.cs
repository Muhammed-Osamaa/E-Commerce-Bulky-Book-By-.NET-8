using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using Utility;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCarts = _unitOfWork.ShoppingCart.GetAll(item => item.Id == userId, "Product"),
                OrderHeader = new OrderHeader()
            };
            foreach (var item in ShoppingCartVM.ShoppingCarts)
            {
                item.price = GetPriceBasedOnQuantity(item);
                ShoppingCartVM.OrderHeader.OrderTotal += (item.price * item.Count);
            }
            return View(ShoppingCartVM);
        }
        public IActionResult Summary()
        {
            //get current user id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            //we use view Model
            //and make object of it and populated the prop 
            //retrive all column from shoppingcart in List of shopping carts
            //make object of orderheader to calculated ordertotal 
            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCarts = _unitOfWork.ShoppingCart.GetAll(item => item.Id == userId, "Product"),
                OrderHeader = new OrderHeader()
            };

            //we access shoppnigcarts list to calculate total based on count
            foreach (var item in ShoppingCartVM.ShoppingCarts)
            {
                item.price = GetPriceBasedOnQuantity(item);
                ShoppingCartVM.OrderHeader.OrderTotal += (item.price * item.Count);
            }

            //retrive Application user details based on user id
            //populated orderheader props
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUsers.Get(u => u.Id == userId);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.phoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM.ShoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.Id == userId, "Product");
            ShoppingCartVM.OrderHeader.Id = userId;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

            foreach (var item in ShoppingCartVM.ShoppingCarts)
            {
                item.price = GetPriceBasedOnQuantity(item);
                ShoppingCartVM.OrderHeader.OrderTotal += (item.price * item.Count);
            }
            ApplicationUser applicationUser = _unitOfWork.ApplicationUsers.Get(u => u.Id == userId);
            if (applicationUser.CompanyId == null)
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            _unitOfWork.orderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var item in ShoppingCartVM.ShoppingCarts)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.OrderHeaderId,
                    Price = item.price,
                    Count = item.Count,

                };
                _unitOfWork.OrderDetails.Add(orderDetail);
                _unitOfWork.Save();

            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                var domain = "https://localhost:7157";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = $"{domain}/Customer/Cart/OrderConfirmation/{ShoppingCartVM.OrderHeader.OrderHeaderId}",
                    CancelUrl = $"{domain}/Customer/Cart/Index",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                        

                    Mode = "payment",
                };

                foreach (var item in ShoppingCartVM.ShoppingCarts)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions()
                            {
                                Name = item.Product.Title
                            },
                        },
                        Quantity = item.Count
                    };

                    options.LineItems.Add(sessionLineItem);
                }
                var service = new Stripe.Checkout.SessionService();
                var session =  service.Create(options);
                _unitOfWork.orderHeader.UpdateStripPaymentId(ShoppingCartVM.OrderHeader.OrderHeaderId, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);

            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.OrderHeaderId });
        }

        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = _unitOfWork.orderHeader.Get(u => u.OrderHeaderId == id , "ApplicationUser");
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = service.Get(orderHeader.SessionId);
                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.orderHeader.UpdateStripPaymentId(id,session.Id, session.PaymentIntentId);
                    _unitOfWork.orderHeader.UpdateStatus(id, SD.StatusApproved , SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.Id == orderHeader.Id).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }

        public IActionResult Plus(int cardId)
        {
            var cardFromDb = _unitOfWork.ShoppingCart.Get(u => u.ShoppingId == cardId);
            cardFromDb.Count++;
            _unitOfWork.ShoppingCart.Update(cardFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));


        }
        public IActionResult Minus(int cardId)
        {
            var cardFromDb = _unitOfWork.ShoppingCart.Get(u => u.ShoppingId == cardId);
            if (cardFromDb.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cardFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(item => item.Id == cardFromDb.Id).Count() - 1);

            }
            else
            {
                cardFromDb.Count--;
                _unitOfWork.ShoppingCart.Update(cardFromDb);

            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Delete(int cardId)
        {
            var cardFromDb = _unitOfWork.ShoppingCart.Get(u => u.ShoppingId == cardId , tracking: true);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(item => item.Id == cardFromDb.Id).Count() - 1);
            _unitOfWork.ShoppingCart.Remove(cardFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;

            }
        }
    }
}

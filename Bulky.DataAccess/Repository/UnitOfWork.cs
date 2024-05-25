using Bulky.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public ApplicationDbContext _db;
        public ICategoryRepository category { get; private set; }

        public IProductRepository product { get; private set; }

        public ICompanyRepository company { get; private set; }

        public IShoppingCartRepository ShoppingCart { get; private set; }

        public IApplicationUserRepository ApplicationUsers { get; private set; }
        public IOrderHeaderRepository orderHeader { get; private set; }
        public IOrderDetailsRepository OrderDetails { get; private set; }

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            ApplicationUsers = new ApplicationUserRepository(db);
            category = new CategoryRepository(db);
            ShoppingCart = new ShoppingCartRepository(db);
            product = new ProductRepository(db);
            company = new CompanyRepositroy(db);
            orderHeader = new OrderHeaderRepository(db);
            OrderDetails = new OrderDetailRepository(db);
        }
        public void Save()
        {
            _db.SaveChanges();
        }
    }
}

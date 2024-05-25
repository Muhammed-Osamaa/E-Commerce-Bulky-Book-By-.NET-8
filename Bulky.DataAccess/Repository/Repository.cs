using Bulky.Data;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            dbSet = _db.Set<T>();

        }

        public void Add(T e)
        {
            dbSet.Add(e);
        }

        public T Get(System.Linq.Expressions.Expression<Func<T, bool>> filter, string? includePropties = null, bool tracking = false)
        {
            IQueryable<T> query;
            if (tracking)
            {
                query = dbSet;

            }
            else
            {
                query = dbSet.AsNoTracking();

            }
            if (!string.IsNullOrEmpty(includePropties))
            {
                foreach (var item in includePropties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                };
            }
            return query.FirstOrDefault(filter);
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if(filter != null)
            {
                query = query.Where(filter);
            }
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var item in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                };
            }
            return query.ToList();
        }

        public void Remove(T entity)
        {

            dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);

        }
    }
}

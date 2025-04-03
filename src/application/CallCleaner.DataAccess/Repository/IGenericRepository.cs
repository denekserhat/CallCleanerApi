using CallCleaner.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace CallCleaner.DataAccess.Repository;

public interface IGenericRepository<T> where T : BaseEntity
{
    DbSet<T> Table { get; }
    T GetByID(int id);
    List<T> GetList();
    Task<List<T>?> GetAllAsync();
    Task<List<T>?> GetAllAsync(Expression<Func<T, bool>> filter);
    Task<T> GetByIdAsync(int id);
    IQueryable<T> GetWhere(Expression<Func<T, bool>> filter);
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> filter);
    Task<int> GetCountAsync();
    Task<int> GetCountAsync(Expression<Func<T, bool>> filter);

    Task<bool> AddAsync(T entity);
    Task<bool> AddRangeAsync(List<T> datas);
    Task<bool> UpdateAsync(T entity);
    bool Update(T entity);
    bool Delete(T entity);
    Task<bool> RemoveAsync(int id);
    Task<bool> RemoveRangeAsync(List<int> ids);
}

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly DataContext _context;
    public GenericRepository(DataContext context)
    {
        _context = context;
    }

    public DbSet<T> Table => _context.Set<T>();
    public T GetByID(int id) => Table.Find(id);
    public List<T> GetList() => Table.ToList();
    public async Task<List<T>?> GetAllAsync() => await Table.Where(x => !x.IsDeleted).AsNoTracking().ToListAsync();
    public async Task<List<T>?> GetAllAsync(Expression<Func<T, bool>> filter) => await Table.AsNoTracking().Where(filter).ToListAsync();
    public async Task<T> GetByIdAsync(int id) => await Table.FindAsync(id);
    public IQueryable<T> GetWhere(Expression<Func<T, bool>> filter) => Table.Where(filter).AsNoTracking();
    public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> filter) => await Table.FirstOrDefaultAsync(filter);
    public async Task<int> GetCountAsync() => await Table.CountAsync();
    public async Task<int> GetCountAsync(Expression<Func<T, bool>> filter) => await Table.CountAsync(filter);


    public async Task<bool> AddAsync(T model)
    {
        EntityEntry<T> entityEntry = await Table.AddAsync(model);
        await _context.SaveChangesAsync();
        return entityEntry.State == EntityState.Added;
    }
    public async Task<bool> AddRangeAsync(List<T> datas)
    {
        await Table.AddRangeAsync(datas);
        return true;
    }
    public bool Update(T entity)
    {
        EntityEntry entityEntry = Table.Update(entity);
        _context.SaveChanges();
        return entityEntry.State == EntityState.Modified;
    }
    public bool Delete(T entity)
    {
        EntityEntry<T> entry = Table.Remove(entity);
        _context.SaveChanges();
        return entry.State == EntityState.Deleted;
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        try
        {
            EntityEntry<T> entityEntry = Table.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<bool> RemoveAsync(int id)
    {
        try
        {
            var model = await Table.FirstOrDefaultAsync(x => x.Id == id);
            Table.Remove(model);
            _context.SaveChanges();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<bool> RemoveRangeAsync(List<int> ids)
    {
        try
        {
            var models = await Table.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
            Table.RemoveRange(models);
            _context.SaveChanges();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}


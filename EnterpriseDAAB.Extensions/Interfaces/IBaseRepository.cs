using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EnterpriseDAAB
{
    public interface IBaseRepository
    {
        int Add<T>(T entity) where T : class;

        int Add<TEntity>(TEntity entity, bool useReturnId, Expression<Func<TEntity, object>> sqlColumns, bool useExcludeMode) where TEntity : class;

        int Delete<T, TId>(TId id) where T : class;

        TEntity GetData<TEntity, TId>(TId id, bool isRowMapper = false) where TEntity : class, new();

        TEntity GetData<TEntity, TId>(TId id, Expression<Func<TEntity, object>> sqlColumns, bool isRowMapper = false) where TEntity : class, new();

        IList<TEntity> GetAllData<TEntity>(bool isRowMapper = false) where TEntity : class, new();

        IList<TEntity> GetAllData<TEntity>(Expression<Func<TEntity, object>> sqlColumns, bool isRowMapper = false) where TEntity : class, new();

        int Update<TEntity>(TEntity entity) where TEntity : class;

        int Update<TEntity>(TEntity entity, Expression<Func<TEntity, object>> sqlColumns, bool isExcludeMode) where TEntity : class, new();
    }
}
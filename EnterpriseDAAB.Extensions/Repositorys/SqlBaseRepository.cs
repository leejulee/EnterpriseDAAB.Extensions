using Microsoft.Practices.EnterpriseLibrary.Data;
using MvcPaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace EnterpriseDAAB
{
    public class SqlBaseRepository : IBaseRepository, IDisposable
    {
        public SqlBaseRepository()
            : this("EnterpriseDefaultConnString")
        {
        }

        public SqlBaseRepository(string connectionStringName)
        {
            this.ConnectionStringName = connectionStringName;
        }

        private DatabaseProviderFactory factory = new DatabaseProviderFactory();

        private Database db;

        protected Database Db
        {
            get
            {
                if (this.db == null)
                {
                    this.db = this.factory.Create(this.ConnectionStringName);
                }
                return this.db;
            }
        }

        private string connectionStringName;

        /// <summary>
        /// 連線字串
        /// </summary>
        protected string ConnectionStringName
        {
            get
            {
                return connectionStringName;
            }
            set
            {
                connectionStringName = value;
            }
        }

        /// <summary>
        /// 取得單一欄位資料
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected T GetSingleRow<T>(DbCommand cmd)
        {
            return (T)this.db.ExecuteScalar(cmd);
        }

        /// <summary>
        /// 取得單一欄位資料
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected T GetSingleRow<T>(DbCommand cmd, bool useDefaultValue = false)
        {
            return ObjectHelper.ConvertToType<T>(this.db.ExecuteScalar(cmd), useDefaultValue);
        }

        /// <summary>
        /// 取得單一筆資料
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected T GetSingle<T>(DbCommand cmd) where T : new()
        {
            using (var dr = this.db.ExecuteReader(cmd))
            {
                if (dr.Read())
                {
                    var mapper = MapBuilder<T>.BuildAllProperties();
                    return mapper.MapRow(dr);
                }
            }
            return default(T);
        }

        protected T GetSingle<T>(DbCommand cmd, IRowMapper<T> rowMapper) where T : new()
        {
            if (rowMapper == null)
            {
                throw new ArgumentNullException("rowMapper is Null");
            }

            using (var dr = this.db.ExecuteReader(cmd))
            {
                if (dr.Read())
                {
                    return rowMapper.MapRow(dr);
                }
            }
            return default(T);
        }

        /// <summary>
        /// 取得所有資料列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected IList<T> GetCollection<T>(DbCommand cmd) where T : new()
        {
            IList<T> list = new List<T>();
            using (var dr = this.db.ExecuteReader(cmd))
            {
                while (dr.Read())
                {
                    var mapper = MapBuilder<T>.BuildAllProperties();
                    list.Add(mapper.MapRow(dr));
                }
            }
            return list;
        }

        /// <summary>
        /// 取得所有資料列表(自訂對應)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="rowMapper"></param>
        /// <returns></returns>
        protected IList<T> GetCollection<T>(DbCommand cmd, IRowMapper<T> rowMapper) where T : new()
        {
            if (rowMapper == null)
            {
                throw new ArgumentNullException("rowMapper is Null");
            }
            IList<T> list = new List<T>();
            using (var dr = this.db.ExecuteReader(cmd))
            {
                while (dr.Read())
                {
                    list.Add(rowMapper.MapRow(dr));
                }
            }
            return list;
        }

        /// <summary>
        /// 新增資料(自動對應類別)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        [Obsolete("Use Add<TEntity>(TEntity entity, bool useReturnId)")]
        public int Add<TEntity>(TEntity entity) where TEntity : class
        {
            return this.Add(entity, false);
        }

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="sqlColumns">The SQL columns.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public int Add<TEntity>(TEntity entity, bool useReturnId) where TEntity : class
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>(entity);

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x, entity));

            IEnumerable<string> names = this.GetMappingNameFromProperty(propertyMapInfo, isCheckAutoIncrement: true);

            string sql = string.Format("INSERT INTO [dbo].{0} ({1}) VALUES ({2})",
                        typeMapInfo.DBTableName,
                        string.Join(",", names),
                        string.Join(",", names.Select(item => "@" + item.ToLower())));

            if (useReturnId)
            {
                sql += ";SELECT CAST(scope_identity() AS int)";
            }

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                foreach (var item in propertyMapInfo)
                {
                    var name = (item.IsRename) ? item.DBColumnName : item.PropertyName;
                    this.db.AddInParameter(cmd, "@" + name.ToLower(), this.GetDBType(item.PropertyType), item.PropertyValue);
                }

                return useReturnId ? ObjectHelper.ConvertToType<int>(this.Db.ExecuteScalar(cmd)) : this.Db.ExecuteNonQuery(cmd);
            }
        }

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="sqlColumns">The SQL columns.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public int Add<TEntity>(TEntity entity, bool useReturnId, Expression<Func<TEntity, object>> sqlColumns, bool isExcludeMode) where TEntity : class
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>(entity);

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x, entity));

            IEnumerable<string> names = this.GetPropertyNames<TEntity>(sqlColumns);

            if (isExcludeMode)
            {
                propertyMapInfo = propertyMapInfo.Where(x => !names.Contains(x.PropertyName)).ToList();
                names = this.GetMappingNameFromProperty(propertyMapInfo, isCheckAutoIncrement: true);
            }
            else
            {
                names = this.GetMappingNameFromProperty(propertyMapInfo.Where(x => names.Contains(x.PropertyName)), isCheckAutoIncrement: true);
            }

            string sql = string.Format("INSERT INTO [dbo].{0} ({1}) VALUES ({2})",
                        typeMapInfo.DBTableName,
                        string.Join(",", names),
                        string.Join(",", names.Select(item => "@" + item.ToLower())));

            if (useReturnId)
            {
                sql += ";SELECT CAST(scope_identity() AS int)";
            }

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                foreach (var item in propertyMapInfo)
                {
                    var name = (item.IsRename) ? item.DBColumnName : item.PropertyName;
                    this.db.AddInParameter(cmd, "@" + name.ToLower(), this.GetDBType(item.PropertyType), item.PropertyValue);
                }

                return useReturnId ? ObjectHelper.ConvertToType<int>(this.Db.ExecuteScalar(cmd)) : this.Db.ExecuteNonQuery(cmd);
            }
        }

        /// <summary>
        /// 刪除資料(自動對應類別)
        /// <para>需配合PrimaryKeyMappingAttribute設定</para>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="id"></param>
        public int Delete<TEntity, TId>(TId id) where TEntity : class
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>();

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x));

            var primaryKeyName = this.GetPrimaryKeyName(propertyMapInfo);

            var primaryKeyParameter = string.Format("@{0}", primaryKeyName);

            string sql = string.Format("DELETE [dbo].{0} WHERE {1}={2}", typeMapInfo.DBTableName, primaryKeyName, primaryKeyParameter);

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                this.db.AddInParameter(cmd, primaryKeyParameter, this.GetDBType(typeof(TId)), id);

                return this.Db.ExecuteNonQuery(cmd);
            }
        }

        public int Update<TEntity>(TEntity entity) where TEntity : class
        {
            //TODO 需修改不更新Primarykey欄位
            var typeMapInfo = new TypeMappingInfo<TEntity>(entity);

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x, entity));

            IEnumerable<string> names = this.GetMappingNameFromProperty(propertyMapInfo, isCheckAutoIncrement: true);

            var primaryKeyName = this.GetPrimaryKeyName(propertyMapInfo);

            var primaryKeyParameter = string.Format("@{0}", primaryKeyName.ToLower());

            var parameters = string.Join(",", names.Select(item => string.Format("{0}=@{1}", item, item.ToLower())));

            string sql = string.Format("UPDATE [dbo].{0} SET {1} WHERE {2}={3}",
                        typeMapInfo.DBTableName,
                        parameters,
                        primaryKeyName,
                        primaryKeyParameter
                        );

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                foreach (var item in propertyMapInfo)
                {
                    var name = (item.IsRename) ? item.DBColumnName : item.PropertyName;
                    this.db.AddInParameter(cmd, "@" + name.ToLower(), this.GetDBType(item.PropertyType), item.PropertyValue);
                }

                return this.Db.ExecuteNonQuery(cmd);
            }
        }

        public int Update<TEntity>(TEntity entity, Expression<Func<TEntity, object>> sqlColumns, bool isExcludeMode) where TEntity : class, new()
        {
            //TODO 需修改不更新Primarykey欄位
            var typeMapInfo = new TypeMappingInfo<TEntity>(entity);

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x, entity));

            IEnumerable<string> names = this.GetPropertyNames<TEntity>(sqlColumns);

            if (isExcludeMode)
            {
                propertyMapInfo = propertyMapInfo.Where(x => !names.Contains(x.PropertyName)).ToList();
                names = this.GetMappingNameFromProperty(propertyMapInfo, isCheckAutoIncrement: true);
            }
            else
            {
                names = this.GetMappingNameFromProperty(propertyMapInfo.Where(x => names.Contains(x.PropertyName)), isCheckAutoIncrement: true);
            }

            var primaryKeyName = this.GetPrimaryKeyName(propertyMapInfo);

            var primaryKeyParameter = string.Format("@{0}", primaryKeyName.ToLower());

            var parameters = string.Join(",", names.Select(item => string.Format("{0}=@{1}", item, item.ToLower())));

            string sql = string.Format("UPDATE [dbo].{0} SET {1} WHERE {2}={3}",
                        typeMapInfo.DBTableName,
                        parameters,
                        primaryKeyName,
                        primaryKeyParameter
                        );

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                foreach (var item in propertyMapInfo)
                {
                    var name = (item.IsRename) ? item.DBColumnName : item.PropertyName;
                    this.db.AddInParameter(cmd, "@" + name.ToLower(), this.GetDBType(item.PropertyType), item.PropertyValue);
                }

                return this.Db.ExecuteNonQuery(cmd);
            }
        }

        /// <summary>
        /// 取得單一筆資料
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public TEntity GetData<TEntity, TId>(TId id, bool isRowMapper = false) where TEntity : class, new()
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>();

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x));

            var primaryKeyName = this.GetPrimaryKeyName(propertyMapInfo);

            var primaryKeyParameter = string.Format("@{0}", primaryKeyName);

            IEnumerable<string> names = this.GetMappingNameFromProperty(propertyMapInfo, true);

            string sql = string.Format("SELECT {0} FROM {1} WHERE {2}={3}"
                                , string.Join(",", names)
                                , typeMapInfo.DBTableName
                                , primaryKeyName
                                , primaryKeyParameter);

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                this.db.AddInParameter(cmd, primaryKeyParameter, this.GetDBType(typeof(TId)), id);

                if (isRowMapper)
                {
                    return this.GetSingle<TEntity>(cmd, new CommonRowMapper<TEntity>());
                }
                else
                {
                    return this.GetSingle<TEntity>(cmd);
                }
            }
        }

        public TEntity GetData<TEntity, TId>(TId id, Expression<Func<TEntity, object>> sqlColumns, bool isRowMapper = false) where TEntity : class, new()
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>();

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x));

            var primaryKeyName = this.GetPrimaryKeyName(propertyMapInfo);

            var primaryKeyParameter = string.Format("@{0}", primaryKeyName);

            IEnumerable<string> names = this.GetPropertyNames<TEntity>(sqlColumns);

            names = this.GetMappingNameFromProperty(propertyMapInfo.Where(x => names.Contains(x.PropertyName)), true);

            string sql = string.Format("SELECT {0} FROM {1} WHERE {2}={3}"
                                , string.Join(",", names)
                                , typeMapInfo.DBTableName
                                , primaryKeyName
                                , primaryKeyParameter);

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                this.db.AddInParameter(cmd, primaryKeyParameter, this.GetDBType(typeof(TId)), id);

                if (isRowMapper)
                {
                    return this.GetSingle<TEntity>(cmd, new CommonRowMapper<TEntity>());
                }
                else
                {
                    return this.GetSingle<TEntity>(cmd);
                }
            }
        }

        /// <summary>
        /// 取得所有資料
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public IList<TEntity> GetAllData<TEntity>(bool isRowMapper = false) where TEntity : class, new()
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>();

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x));

            IEnumerable<string> names = this.GetMappingNameFromProperty(propertyMapInfo, true);

            string sql = string.Format("SELECT {0} FROM {1}"
                                , string.Join(",", names)
                                , typeMapInfo.DBTableName);

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                if (isRowMapper)
                {
                    return this.GetCollection<TEntity>(cmd, new CommonRowMapper<TEntity>());
                }
                else
                {
                    return this.GetCollection<TEntity>(cmd);
                }
            }
        }

        /// <summary>
        /// 取得所有資料
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public IList<TEntity> GetAllData<TEntity>(Expression<Func<TEntity, object>> sqlColumns, bool isRowMapper = false) where TEntity : class, new()
        {
            var typeMapInfo = new TypeMappingInfo<TEntity>();

            var propertyMapInfo = typeMapInfo.PropertyInfoList.Select(x => new PropertyMappingInfo<TEntity>(x));

            IEnumerable<string> names = this.GetPropertyNames<TEntity>(sqlColumns);

            names = this.GetMappingNameFromProperty(propertyMapInfo.Where(x => names.Contains(x.PropertyName)), true);

            string sql = string.Format("SELECT {0} FROM {1}"
                                , string.Join(",", names)
                                , typeMapInfo.DBTableName);

            using (DbCommand cmd = this.Db.GetSqlStringCommand(sql))
            {
                if (isRowMapper)
                {
                    return this.GetCollection<TEntity>(cmd, new CommonRowMapper<TEntity>());
                }
                else
                {
                    return this.GetCollection<TEntity>(cmd);
                }
            }
        }

        /// <summary>
        /// 取得分頁資料列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="orderByDictionary">排序項目</param>
        /// <param name="pageNumber">資料頁碼</param>
        /// <param name="pageSize">資料筆數</param>
        /// <returns></returns>
        protected IPagedList<T> GetPagingList<T>(DbCommand cmd, IDictionary<string, OrderEnum> orderByDictionary, int pageNumber, int pageSize) where T : new()
        {
            int totalCount = 0;

            var originalSql = cmd.CommandText;

            var originalSqlIndex = originalSql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);

            if (originalSqlIndex == -1)
            {
                throw new ArgumentException(string.Format("No Support SQL : {0}", cmd.CommandText));
            }

            if (orderByDictionary == null || orderByDictionary.Count == 0)
            {
                throw new ArgumentException(string.Format("orderByDictionary 不能為Null或沒有值"));
            }

            string sqlTempData = @"SELECT * INTO #TempList FROM ( {0} ) AS list ;
                                   SELECT COUNT(1) AS TotalCount FROM #TempList ;";

            string sqlRowNumber = @" ROW_NUMBER() OVER (ORDER BY {0} )  AS Row , ";

            string sql = @"SELECT * FROM #TempList AS list
                           WHERE list.Row >= ((@pageNumber-1) * @pageSize) + 1
                             AND list.Row <= @pageSize * (@pageNumber)";

            sqlRowNumber = string.Format(sqlRowNumber, string.Join(" , ", orderByDictionary.Select(x => string.Format("{0} {1}", x.Key, x.Value))));

            cmd.CommandText = string.Format(sqlTempData, originalSql.Insert(originalSqlIndex + 7, sqlRowNumber)) + sql;

            this.Db.AddInParameter(cmd, "@pageNumber", DbType.Int32, pageNumber);
            this.Db.AddInParameter(cmd, "@pageSize", DbType.Int32, pageSize);

            IList<T> list = new List<T>();
            using (var dr = this.db.ExecuteReader(cmd))
            {
                while (dr.Read())
                {
                    totalCount = Convert.ToInt32(dr[0]);
                }
                dr.NextResult();
                while (dr.Read())
                {
                    var mapper = MapBuilder<T>.BuildAllProperties();
                    list.Add(mapper.MapRow(dr));
                }
            }

            return list.ToPagedList<T>(pageNumber - 1, pageSize, totalCount);
        }

        /// <summary>
        /// 取得分頁資料列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        /// <param name="orderByDictionary">排序項目</param>
        /// <param name="pageNumber">資料頁碼</param>
        /// <param name="pageSize">資料筆數</param>
        /// <returns></returns>
        protected IPagedList<T> GetPagingList<T>(DbCommand cmd, IRowMapper<T> rowMapper, IDictionary<string, OrderEnum> orderByDictionary, int pageNumber, int pageSize) where T : new()
        {
            int totalCount = 0;

            var originalSql = cmd.CommandText;

            var originalSqlIndex = originalSql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);

            if (rowMapper == null)
            {
                throw new ArgumentNullException("rowMapper is Null");
            }

            if (originalSqlIndex == -1)
            {
                throw new ArgumentException(string.Format("No Support SQL : {0}", cmd.CommandText));
            }

            if (orderByDictionary == null || orderByDictionary.Count == 0)
            {
                throw new ArgumentException(string.Format("orderByDictionary 不能為Null或沒有值"));
            }

            string sqlTempData = @"SELECT * INTO #TempList FROM ( {0} ) AS list ;
                                   SELECT COUNT(1) AS TotalCount FROM #TempList ;";

            string sqlRowNumber = @" ROW_NUMBER() OVER (ORDER BY {0} )  AS Row , ";

            string sql = @"SELECT * FROM #TempList AS list
                           WHERE list.Row >= ((@pageNumber-1) * @pageSize) + 1
                             AND list.Row <= @pageSize * (@pageNumber)";

            sqlRowNumber = string.Format(sqlRowNumber, string.Join(" , ", orderByDictionary.Select(x => string.Format("{0} {1}", x.Key, x.Value))));

            cmd.CommandText = string.Format(sqlTempData, originalSql.Insert(originalSqlIndex + 7, sqlRowNumber)) + sql;

            this.Db.AddInParameter(cmd, "@pageNumber", DbType.Int32, pageNumber);
            this.Db.AddInParameter(cmd, "@pageSize", DbType.Int32, pageSize);

            IList<T> list = new List<T>();
            using (var dr = this.db.ExecuteReader(cmd))
            {
                while (dr.Read())
                {
                    totalCount = Convert.ToInt32(dr[0]);
                }
                dr.NextResult();
                while (dr.Read())
                {
                    list.Add(rowMapper.MapRow(dr));
                }
            }

            return list.ToPagedList<T>(pageNumber - 1, pageSize, totalCount);
        }

        public void Dispose()
        {
            this.factory.Dispose();
            this.factory = null;
            this.db = null;
        }

        private DbType GetDBType(Type type)
        {
            TypeConverter tc = TypeDescriptor.GetConverter(typeof(DbType));

            if (tc.CanConvertFrom(type))
            {
                return (DbType)tc.ConvertFrom(type.Name);
            }

            try
            {
                if (type.IsGenericType)
                {
                    var nullableType = Nullable.GetUnderlyingType(type);
                    return (DbType)tc.ConvertFrom(nullableType.Name);
                }
                if (type.IsEnum)
                {
                    //TODO 先回傳int32長度
                    return DbType.Int32;
                }
                return (DbType)tc.ConvertFrom(type.Name);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("GetDBType Convert Error : {0}", type.FullName));
            }
        }

        /// <summary>
        /// 取得屬性名稱
        /// <para>若有對應名稱則用sql的as串接</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p"></param>
        /// <param name="useFieldRename"></param>
        private IList<string> GetMappingNameFromProperty<T>(IEnumerable<PropertyMappingInfo<T>> p, bool useFieldRename = false, bool isCheckAutoIncrement = false)
        {
            IList<string> list = new List<string>();
            string name = string.Empty;
            foreach (var item in p)
            {
                if (isCheckAutoIncrement && item.IsAutoIncrement)
                {
                    continue;
                }
                if (item.IsRename)
                {
                    name = item.DBColumnName;
                    if (useFieldRename)
                    {
                        name = string.Format("{0} AS [{1}]", item.DBColumnName, item.PropertyName);
                    }
                }
                else
                {
                    name = item.PropertyName;
                }
                list.Add(name);
            }
            return list;
        }

        /// <summary>
        /// Gets the property names.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// object is null
        /// or
        /// NewExpression is null
        /// </exception>
        /// <exception cref="System.ArgumentException">NewExpression Count is zero</exception>
        private IEnumerable<string> GetPropertyNames<TEntity>(Expression<Func<TEntity, object>> properties) where TEntity : class
        {
            if (properties == null)
            {
                throw new ArgumentNullException("object is null");
            }

            if (properties.Parameters.Count == 0)
            {
                throw new ArgumentException("NewExpression Count is zero");
            }

            var body = properties.Body as NewExpression;
            if (body == null)
            {
                throw new ArgumentNullException("NewExpression is null");
            }
            return ((NewExpression)body).Members.Select(m => m.Name);
        }

        private string GetPrimaryKeyName<TEntity>(IEnumerable<PropertyMappingInfo<TEntity>> propertyMapInfo)
        {
            var propertyInfo = propertyMapInfo.FirstOrDefault(x => x.IsPrimaryKey);

            if (propertyInfo == null)
            {
                throw new ArgumentNullException("PrimaryKeyMapping", "PrimaryKeyMapping 未設定");
            }

            return propertyInfo.PrimaryKeyName;
        }

    }
}
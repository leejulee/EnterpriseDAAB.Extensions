using System;
using System.Reflection;

namespace EnterpriseDAAB
{
    public class TypeMappingInfo<TEntity>
    {
        private Type _type;

        private TEntity _entity;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="entity"></param>
        public TypeMappingInfo(TEntity entity)
        {
            if (_type == null)
            {
                _type = typeof(TEntity);
            }

            if (_entity == null)
            {
                _entity = entity;
            }
        }

        /// <summary>
        /// 建構子
        /// </summary>
        public TypeMappingInfo()
        {
            if (_type == null)
            {
                _type = typeof(TEntity);
            }

            if (_entity == null)
            {
                _entity = default(TEntity);
            }
        }

        public string DBTableName
        {
            get
            {
                string name = string.Empty;

                var tattr = _type.GetCustomAttributes(typeof(DBMappingAttribute), false);

                if (tattr.Length > 0)
                {
                    name = (tattr[0] as DBMappingAttribute).Name;
                }
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = _type.Name;
                }
                return name;
            }
        }

        public PropertyInfo[] PropertyInfoList
        {
            get
            {
                var info = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                if (info.Length == 0)
                {
                    throw new Exception(string.Format("Model {0} Not Found Property", _type.Name));
                }

                return info;
            }
        }
    }
}
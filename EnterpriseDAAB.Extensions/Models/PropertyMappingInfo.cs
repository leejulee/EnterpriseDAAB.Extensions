using System;
using System.Linq;
using System.Reflection;

namespace EnterpriseDAAB
{
    public class PropertyMappingInfo<T>
    {
        private PropertyInfo _info = null;

        private T _model;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="info"></param>
        public PropertyMappingInfo(PropertyInfo info, T model)
            : this(info)
        {
            if (model != null)
            {
                _model = model;
            }
        }

        public PropertyMappingInfo(PropertyInfo info)
        {
            if (_info == null)
            {
                _info = info;
            }
        }

        /// <summary>
        /// 主鍵是否自動遞增
        /// </summary>
        public bool IsAutoIncrement
        {
            get
            {
                var attribute = (PrimaryKeyMappingAttribute)_info.GetCustomAttributes(typeof(PrimaryKeyMappingAttribute), true).FirstOrDefault();

                if (attribute != null)
                {
                    return attribute.IsAutoIncrement;
                }

                return false;
            }
        }

        /// <summary>
        /// 是否為主鍵
        /// </summary>
        public bool IsPrimaryKey
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.PrimaryKeyName);
            }
        }

        /// <summary>
        /// 是否重新命名
        /// </summary>
        public bool IsRename
        {
            get
            {
                //If .net 4.5 Using
                //var attribute = (T)item.GetCustomAttribute(typeof(T), true);
                var attributes = (BaseMappingAttribute[])_info.GetCustomAttributes(typeof(BaseMappingAttribute), true);

                return attributes.Length > 0 && attributes.Any(x => !string.IsNullOrWhiteSpace(x.Name));
            }
        }

        /// <summary>
        /// 主鍵名稱
        /// </summary>
        public string PrimaryKeyName
        {
            get
            {
                var columnName = string.Empty;

                var attribute = (PrimaryKeyMappingAttribute)_info.GetCustomAttributes(typeof(PrimaryKeyMappingAttribute), true).FirstOrDefault();
                if (attribute != null)
                {
                    if (!string.IsNullOrWhiteSpace(attribute.Name))
                    {
                        columnName = attribute.Name;
                    }
                    else
                    {
                        columnName = _info.Name;
                    }
                }

                return columnName;
            }
        }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string DBColumnName
        {
            get
            {
                var columnName = string.Empty;

                var attributes = _info.GetCustomAttributes(typeof(BaseMappingAttribute), true);

                switch (attributes.Length)
                {
                    case 1:
                        columnName = ((BaseMappingAttribute)attributes.First()).Name;
                        if (string.IsNullOrWhiteSpace(columnName))
                        {
                            columnName = _info.Name;
                        }
                        break;

                    case 2:
                        throw new Exception("尚未實作");
                    default:
                        columnName = _info.Name;
                        break;
                }

                return columnName;
            }
        }

        /// <summary>
        /// 屬性名稱
        /// </summary>
        public string PropertyName
        {
            get
            {
                return _info.Name;
            }
        }

        /// <summary>
        /// 屬性型別
        /// </summary>
        public Type PropertyType
        {
            get
            {
                return _info.PropertyType;
            }
        }

        /// <summary>
        /// 屬性值
        /// </summary>
        public object PropertyValue
        {
            get
            {
                if (_model != null)
                {
                    return _info.GetValue(_model, null);
                }
                return null;
            }
        }
    }
}
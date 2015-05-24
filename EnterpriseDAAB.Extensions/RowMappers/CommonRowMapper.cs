using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Data;
using System.Reflection;

namespace EnterpriseDAAB
{
    public class CommonRowMapper<TEntity> : IRowMapper<TEntity> where TEntity : new()
    {
        public bool isIgnoreCase { get; set; }

        public TEntity MapRow(IDataRecord reader)
        {
            TEntity item = new TEntity();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                PropertyInfo property = null;
                if (this.isIgnoreCase)
                {
                    //TODO 需多驗證是否對應正確
                    property = item.GetType().GetProperty(reader.GetName(i), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                }
                else
                {
                    property = item.GetType().GetProperty(reader.GetName(i));
                }

                if (property != null && !reader.GetValue(i).Equals(DBNull.Value))
                {
                    var pType = property.PropertyType;
                    bool hasSetValue = false;
                    //http://juztinwilzon.blogspot.tw/2006/10/setting-nullable-enum-through.html
                    if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        Type[] typeCol = pType.GetGenericArguments();
                        Type nullableType;
                        if (typeCol.Length > 0)
                        {
                            nullableType = typeCol[0];
                            if (nullableType.BaseType == typeof(Enum))
                            {
                                object o = Enum.Parse(nullableType, ObjectHelper.ConvertToType<string>(reader.GetValue(i)));
                                property.SetValue(item, o, null);
                                hasSetValue = true;
                            }
                        }
                    }
                    if (!hasSetValue)
                    {
                        property.SetValue(item, (reader.IsDBNull(i)) ? "[NULL]" : reader.GetValue(i), null);
                    }
                }
            }
            return item;
        }
    }
}
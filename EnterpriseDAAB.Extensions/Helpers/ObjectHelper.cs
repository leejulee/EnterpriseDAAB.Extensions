using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnterpriseDAAB
{
    public static class ObjectHelper
    {
        public static dynamic ConvertToType(object obj, Type type, bool useDefaultValue = false)
        {
            if (HasValidValue(obj))
            {
                if (!useDefaultValue)
                {
                    return OnConvertToType<dynamic>(obj, type);
                }
                try
                {
                    return OnConvertToType<dynamic>(obj, type);
                }
                catch (Exception) { }
            }
            return default(dynamic);
        }

        public static T ConvertToType<T>(object obj, bool useDefaultValue = false)
        {
            var type = typeof(T);
            if (HasValidValue(obj))
            {
                if (!useDefaultValue)
                {
                    return OnConvertToType<T>(obj, type);
                }
                try
                {
                    return OnConvertToType<T>(obj, type);
                }
                catch (Exception) { }
            }
            return default(T);
        }

        private static T OnConvertToType<T>(object obj, Type type)
        {
            if (type.IsPrimitive)
            {
                return (T)Convert.ChangeType(obj, type);
            }

            Type underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
            {
                return (T)Convert.ChangeType(obj, Nullable.GetUnderlyingType(type));
            }

            if (type.IsGenericType || type.IsInterface)
            {
                return (T)obj;
            }

            if (type.IsEnum)
            {
                var objType = obj.GetType();
                int i;
                //遇到Byte的數字的暫解方式之後再調校
                if (objType == typeof(string) && Enum.IsDefined(type, obj))
                {
                    return (T)Enum.Parse(type, obj.ToString());
                }
                else if (int.TryParse(obj.ToString(), out i) && Enum.IsDefined(type, i))
                {
                    return (T)Enum.Parse(type, i.ToString());
                }
                throw new Exception(string.Format("Enum :{0} {1} 無法轉換 ", (type.Name), (obj)));
            }

            return (T)Convert.ChangeType(obj, type);
        }

        private static bool HasValidValue(object obj)
        {
            return (obj != null && !DBNull.Value.Equals(obj));
        }
    }
}

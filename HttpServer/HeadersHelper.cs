using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace ModuleReaderManager.HttpServer
{
    static class HeadersHelper
    {
        public static string GetDescription(this Enum value)
        {
            var valueType = value.GetType();
            var memberName = Enum.GetName(valueType, value);
            if (memberName == null) return null;
            var fieldInfo = valueType.GetField(memberName);
            var attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));
            if (attribute == null) return null;
            return (attribute as DescriptionAttribute).Description;
        }
    }
}

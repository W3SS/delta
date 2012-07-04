﻿#if WINDOWS
using System;
using System.Xml;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Delta.Tiled
{
    internal static class TmxImporterExtensions
    {
        internal static void ImportLayer(this ILayer layer, XmlNode node)
        {
            layer.ID = node.Attributes["name"].Value;
            layer.IsVisible = (node.Attributes["visible"] != null) ? int.Parse(node.Attributes["visible"].Value, CultureInfo.InvariantCulture) == 1 : true;
        }

        internal static void ImportXmlProperties(this object obj, XmlNode node)
        {
            if (node == null)
                return;
            bool isFound = false;
            foreach (XmlNode propertyNode in node.ChildNodes)
            {
                isFound = false;
                string name = propertyNode.Attributes["name"] == null ? null : propertyNode.Attributes["name"].Value;
                string value = propertyNode.Attributes["value"].Value;
                if (!string.IsNullOrEmpty(name))
                {
                    PropertyInfo[] properties = obj.GetType().GetProperties();
                    foreach (var propertyInfo in properties)
                    {
                        var attributes = propertyInfo.GetCustomAttributes(true);
                        foreach (var attribute in attributes)
                        {
                            ContentSerializerAttribute contentSerializerAttribute = attribute as ContentSerializerAttribute;
                            if (contentSerializerAttribute != null)
                            {
                                if (contentSerializerAttribute.ElementName != null)
                                {
                                    if (contentSerializerAttribute.ElementName.ToLower() == name.ToLower())
                                    {
                                        isFound = true;
                                        BindProperty(obj, propertyInfo, value);
                                        break;
                                    }
                                }
                            }
                        }
                        if (!isFound && name.ToLower() == propertyInfo.Name.ToLower())
                        {
                            isFound = true;
                            BindProperty(obj, propertyInfo, value);
                        }
                        if (!isFound)
                        {
                            switch (name.ToLower())
                            {
                                case "offset":
                                    TransformableEntity entity = obj as TransformableEntity;
                                    if (entity != null)
                                    {
                                        string[] split = value.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                                        entity.Position += new Vector2(float.Parse(split[0], CultureInfo.InvariantCulture), float.Parse(split[1], CultureInfo.InvariantCulture));
                                        isFound = true;
                                    }
                                    break;
                            }
                        }
                    }
                    MethodInfo[] methods = obj.GetType().GetMethods();
                    foreach (var methodInfo in methods)
                    {
                        if (!isFound && name.ToLower() == methodInfo.Name.ToLower())
                        {
                            isFound = true;
                            InvokeMethod(obj, methodInfo, value);
                        }
                    }
                }
                else
                    continue;
                if (!isFound)
                    throw new Exception(String.Format("Could not find a property or method with the name '{0}' to bind or call in the type '{1}'.", name, obj.GetType().Name));
            }
        }

        static void BindProperty(object bindObject, PropertyInfo property, string rawValue)
        {
            property.SetValue(bindObject, ChangeType(rawValue, property.PropertyType), null);
        }

        static object ChangeType(object value, Type conversionType)
        {
            if (typeof(IConvertible).IsAssignableFrom(conversionType))
                return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
            else
                throw new Exception(String.Format("Cannot convert the value '{0}' to the type '{1}'.", value, conversionType.ToString()));
        }

        static void InvokeMethod(object invokeObject, MethodInfo method, string parameters)
        {
            string[] split = parameters.Split(new string[] { "," }, StringSplitOptions.None);
            ParameterInfo[] parameterInfos = method.GetParameters();
            if (parameterInfos.Length != split.Length)
                throw new Exception("Parameters mismatch.");
            object[] parameterValues = new object[parameterInfos.Length];
            for (int x = 0; x < parameterInfos.Length; x++)
            {
                if (string.IsNullOrEmpty(split[x]))
                    parameterValues[x] = parameterInfos[x].DefaultValue;
                else
                    parameterValues[x] = ChangeType(split[x], parameterInfos[x].ParameterType);
            }
            method.Invoke(invokeObject, parameterValues);
        }

    }
}
#endif
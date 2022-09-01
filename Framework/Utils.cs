using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using Newtonsoft.Json;

namespace Framework
{
    public class Utils
    {
        public static String GetTempFilePathWithExtension(String extension)
        {
            var path = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + extension;
            return Path.Combine(path, fileName);
        }

        public static String DoPost(String url, NameValueCollection args)
        {
            using (WebClient client = new WebClient())
            {
                byte[] response = client.UploadValues(url, args);

                return System.Text.Encoding.UTF8.GetString(response);
            }
        }

        public static IEnumerable<T> GetStaticProperties<T>(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(T) && p.CanRead).Select<PropertyInfo, T>(p => (T)p.GetValue(null));
        }

        public static IEnumerable<T> GetProperties<T>(Object instance)
        {
            return instance.GetType().GetProperties(BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(T) && p.CanRead).Select(p => (T)p.GetValue(instance));
        }

        public static String FormatJson(String json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        public static void HandleException(Exception ex)
        {
            MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
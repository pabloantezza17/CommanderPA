using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Framework
{
    /// <summary>
    /// Trae los settings de usuario de la versión .NET Framework de la app. Al pasar a .NET moderno
    /// el user.config se guarda en otra carpeta, así que sin esto la app arrancaba "de cero"
    /// (sin rama actual ni lista de branches, entre otros).
    /// </summary>
    public static class LegacySettings
    {
        /// <summary>
        /// Si la versión nueva todavía no guardó settings propios, busca el user.config más reciente
        /// de la versión vieja (%LOCALAPPDATA%\*\{exeName}_Url_*\*\user.config) y copia los valores
        /// de las secciones de cada <paramref name="settings"/>. Llamar al arrancar, antes de usarlos.
        /// </summary>
        public static void Import(String exeName, params ApplicationSettingsBase[] settings)
        {
            try
            {
                String current = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

                if (File.Exists(current)) return;

                FileInfo legacy = FindLegacyConfig(exeName, current);

                if (legacy == null) return;

                XElement userSettings = XDocument.Load(legacy.FullName).Root?.Element("userSettings");

                if (userSettings == null) return;

                foreach (var target in settings)
                {
                    XElement section = userSettings.Element(target.GetType().FullName);

                    if (section == null) continue;

                    Boolean changed = false;

                    foreach (XElement setting in section.Elements("setting"))
                    {
                        String name = (String)setting.Attribute("name");
                        SettingsProperty property = target.Properties[name];

                        if (property == null || (String)setting.Attribute("serializeAs") != "String") continue;

                        TypeConverter converter = TypeDescriptor.GetConverter(property.PropertyType);
                        String value = (String)setting.Element("value") ?? String.Empty;

                        target[name] = converter.ConvertFromInvariantString(value);
                        changed = true;
                    }

                    if (changed) target.Save();
                }
            }
            catch (Exception)
            {
                // Si algo falla, la app sigue con los valores por defecto como antes.
            }
        }

        private static FileInfo FindLegacyConfig(String exeName, String current)
        {
            String localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Directory.EnumerateDirectories(localAppData)
                .SelectMany(company => SafeEnumerate(company, exeName + "_Url_*"))
                .SelectMany(app => SafeEnumerate(app, "*"))
                .Select(version => new FileInfo(Path.Combine(version, "user.config")))
                .Where(file => file.Exists && !String.Equals(file.FullName, current, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static String[] SafeEnumerate(String path, String pattern)
        {
            try { return Directory.GetDirectories(path, pattern); }
            catch (Exception) { return new String[0]; }
        }
    }
}

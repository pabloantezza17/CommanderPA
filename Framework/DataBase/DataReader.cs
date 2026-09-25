using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace Framework.DataBase
{
    public class DataReader
    {
        public DataReader(String connectionString)
        {
            this.ConnectionString = WithLegacyEncryptDefault(connectionString);
        }

        public String ConnectionString { get; }

        /// <summary>
        /// Microsoft.Data.SqlClient encripta por defecto (Encrypt=true) y rechaza certificados no confiables,
        /// a diferencia del viejo System.Data.SqlClient. Si la cadena no dice nada, mantengo Encrypt=false
        /// para que sigan andando los SQL de desarrollo con certificados autofirmados.
        /// </summary>
        private static String WithLegacyEncryptDefault(String connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (connectionString.IndexOf("Encrypt", StringComparison.OrdinalIgnoreCase) < 0)
                builder.Encrypt = SqlConnectionEncryptOption.Optional;

            return builder.ConnectionString;
        }

        public IEnumerable<T> Read<T>(String query, params KeyValuePair<String, Object>[] parameters) where T : new()
        {
            DataTable table = this.ReadDataTable(query, parameters);

            List<T> entities = new List<T>();

            foreach (var row in table.AsEnumerable())
                entities.Add(this.CreateItemFromRow<T>(row));

            return entities;
        }

        private DataTable ReadDataTable(String query, KeyValuePair<String, Object>[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                foreach (var parameter in parameters)
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);

                using (var adapter = new SqlDataAdapter(command))
                {
                    DataTable table = new DataTable();

                    adapter.Fill(table);

                    return table;
                }
            }
        }

        private T CreateItemFromRow<T>(DataRow row) where T : new()
        {
            var properties = typeof(T).GetProperties();

            T item = new T();

            foreach (var property in properties)
            {
                if (row.Table.Columns.IndexOf(property.Name) >= 0 && row[property.Name] != DBNull.Value)
                    property.SetValue(item, row[property.Name], null);
            }

            return item;
        }
    }
}
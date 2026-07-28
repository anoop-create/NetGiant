using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.ADO
{
    public class ProductDataAccess
    {
        public ProductDataAccess()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["netgiantMasterData"].ToString();
        }

        private static string _connectionString;

        public ProductEntity GetProduct(string stockRef)
        {
            var productEntity = new ProductEntity();

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("ngmd.GetProductResults", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("WebsiteID", 1));
                    command.Parameters.Add(new SqlParameter("ProductRef", stockRef));
                    command.Parameters.Add(new SqlParameter("Account", ""));
                    conn.Open();
                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        productEntity.ProductName = (string)reader["Description"];
                        productEntity.AssemblyCount = (int)reader["AssemblyCount"];
                        productEntity.AssemblySaving = (double)reader["AssemblySaving"];
                        productEntity.Brand = (string)reader["Brand"];
                        productEntity.PriceRetail = (decimal)reader["PriceRetail"];
                        productEntity.CategoryCodeName = (string)reader["CategoryCodeName"];
                        productEntity.CrossSellProductUrl = (string)reader["CrossSellProductUrl"];
                    }
                }
            }

            return productEntity;
        }
    }
}

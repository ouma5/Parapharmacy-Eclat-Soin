using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EclatEtSoin.Helpers
{
    public static class Database
    {
        public static string ConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["PharmacyDB"];
                return cs == null ? @"Data Source=.\SQLEXPRESS;Initial Catalog=PharmacyDB;Integrated Security=True;Connect Timeout=8;" : cs.ConnectionString;
            }
        }

        public static string MasterConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["Master"];
                return cs == null ? @"Data Source=.\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;Connect Timeout=8;" : cs.ConnectionString;
            }
        }

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static void AddParameters(SqlCommand command, params SqlParameter[] parameters)
        {
            if (parameters == null) return;
            foreach (var p in parameters)
            {
                if (p.Value == null) p.Value = DBNull.Value;
                command.Parameters.Add(p);
            }
        }

        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var cn = CreateConnection())
            using (var cmd = new SqlCommand(sql, cn))
            {
                AddParameters(cmd, parameters);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var table = new DataTable();
                    da.Fill(table);
                    return table;
                }
            }
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var cn = CreateConnection())
            using (var cmd = new SqlCommand(sql, cn))
            {
                AddParameters(cmd, parameters);
                cn.Open();
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var cn = CreateConnection())
            using (var cmd = new SqlCommand(sql, cn))
            {
                AddParameters(cmd, parameters);
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static int Execute(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                AddParameters(cmd, parameters);
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        public static T InTransaction<T>(Func<SqlConnection, SqlTransaction, T> work)
        {
            using (var cn = CreateConnection())
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        var result = work(cn, tx);
                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }
    }
}

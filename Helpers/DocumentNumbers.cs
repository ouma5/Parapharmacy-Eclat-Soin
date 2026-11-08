using System;
using System.Data.SqlClient;

namespace EclatEtSoin.Helpers
{
    public static class DocumentNumbers
    {
        public static string Next(SqlConnection cn, SqlTransaction tx, string table, string prefix)
        {
            var year = DateTime.Now.Year;
            var like = prefix + "-" + year + "-%";
            var obj = Database.Scalar(cn, tx,
                "SELECT MAX(Number) FROM " + table + " WHERE Number LIKE @p",
                Database.P("@p", like));
            int n = 1;
            if (obj != null)
            {
                var last = obj.ToString();
                var parts = last.Split('-');
                if (parts.Length >= 3) int.TryParse(parts[parts.Length - 1], out n);
                n++;
            }
            return prefix + "-" + year + "-" + n.ToString("0000");
        }
    }
}

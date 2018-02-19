using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASUTP.Database;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace UnitTest
{
    [TestClass]
    public class DbTSQLInterface
    {
        private struct Method_ParametrsValidate_Args
        {
            public DbConnection Connection;

            public string Query;

            public DbType [] Types;

            public object [] Parameters;
        }

        [TestMethod]
        public void TestMethod_ParametrsValidate ()
        {
            int [] arErrors;

            Method_ParametrsValidate_Args arg;
            Method_ParametrsValidate_Args [] args;
            SqlConnection dbConn;

            dbConn = new SqlConnection (new ConnectionSettings().GetConnectionStringMSSQL());

            args = new Method_ParametrsValidate_Args [] {
                new Method_ParametrsValidate_Args { Connection = null, Query = string.Empty, Types = null, Parameters = null } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = string.Empty, Types = new DbType [] { }, Parameters = null } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = string.Empty, Types = new DbType [] { }, Parameters = new object [] { } } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = string.Empty, Types = new DbType [] { }, Parameters = new object [] { null } } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = string.Empty, Types = new DbType [] { DbType.Byte }, Parameters = new object [] { } } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = string.Empty, Types = new DbType [] { DbType.DateTime }, Parameters = new object [] { null, DateTime.MaxValue } } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = "SELECT", Types = new DbType [] { DbType.DateTime }, Parameters = new object [] { null, DateTime.MaxValue } } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = "SELECT", Types = new DbType [] { DbType.DateTime }, Parameters = null } // assert = false
                , new Method_ParametrsValidate_Args { Connection = null, Query = "SELECT NOW()", Types = new DbType [] { DbType.DateTime }, Parameters = null } // assert = false
                , new Method_ParametrsValidate_Args { Connection = new SqlConnection(), Query = "SELECT NOW()", Types = new DbType [] { DbType.DateTime }, Parameters = null } // assert = false
                , new Method_ParametrsValidate_Args { Connection = dbConn, Query = "SELECT NOW()", Types = new DbType [] { DbType.DateTime }, Parameters = null } // assert = false
            };

            arErrors = new int [args.Length];

            for (int i = 0; i < args.Length; i++) {
                arErrors [i] = 0;
                arg = args [i];
                ASUTP.Database.DbTSQLInterface.ExecNonQuery (ref arg.Connection, arg.Query, arg.Types, arg.Parameters, out arErrors [i]);
            }

            for (int i = 0; i < arErrors.Length; i++)
                Assert.AreNotEqual (arErrors [i], 0);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class ConnectionStringBuilder : DbConnectionStringBuilder
    {
        private string[] boolTrueString = new string[] { "true", "t", "1", "yes", "y" };

        public ConnectionStringBuilder(string connectionString)
        {
            base.ConnectionString = connectionString;
        }

        public Uri Uri
        {
            get
            {
                var dataSource = DataSource;
                if (dataSource == ".")
                    dataSource = "localhost";
                var uriBuilder = new UriBuilder("net.tcp", dataSource);
                if (uriBuilder.Port < 0)
                    uriBuilder.Port = 26455;
                return uriBuilder.Uri;
            }
        }

        public string DataSource
        {
            get { return (string)this["Data Source"]; }
            set { this["Data Source"] = value; }
        }

        public string UserID
        {
            get { return (string)this["User ID"]; }
            set { this["User ID"] = value; }
        }

        public string Password
        {
            get { return (string)this["Password"]; }
            set { this["Password"] = value; }
        }

        public bool FaultTolerant
        {
            get { return boolTrueString.Contains((string)this["FaultTolerant"] ?? "true"); }
            set { this["FaultTolerant"] = value; }
        }

        public override object this[string keyword]
        {
            get
            {
                if (ContainsKey(keyword))
                    return base[keyword];
                else
                    return null;
            }
            set
            {
                base[keyword] = value;
            }
        }
    }
}
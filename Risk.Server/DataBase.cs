using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class DataBase : DataContext
    {
        public byte FirmId { get; private set; }

        public DataBase(string connection, byte firmId)
            : base(connection)
        {
            this.FirmId = firmId;
        }

        #region StoredProcedures
        [Function(Name = "[Risk].[Clients]")]
        private IEnumerable<Client> GetClients([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<Client>)(result.ReturnValue));
        }

        [Function(Name = "[Risk].[Portfolios]")]
        private IEnumerable<Portfolio> GetPortfolios([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<Portfolio>)(result.ReturnValue));
        }

        [Function(Name = "[Risk].[MoneyInOutDay]")]
        private IEnumerable<MoneyInOut> GetMoneyInOutDay([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<MoneyInOut>)(result.ReturnValue));
        }

        [Function(Name = "[Risk].[Rates]")]
        public IEnumerable<Rate> GetRates([Parameter(Name = "Date", DbType = "DATE")]DateTime date)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), date);
            return ((IEnumerable<Rate>)(result.ReturnValue));
        }
        #endregion

        public void CheckConnection()
        {
            if (Connection.State == ConnectionState.Broken)
            {
                Connection.Close();
                Connection.Open();
            }
            if (Connection.State == ConnectionState.Closed)
            {
                Connection.Open();
                return;
            }
        }

        public IEnumerable<Client> GetClients()
        {
            return GetClients(FirmId);
        }

        public IEnumerable<Portfolio> GetPortfolios()
        {
            return GetPortfolios(FirmId);
        }

        public IEnumerable<MoneyInOut> GetMoneyInOutDay()
        {
            return GetMoneyInOutDay(FirmId);
        }
    }
}

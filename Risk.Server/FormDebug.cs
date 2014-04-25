using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;

namespace Risk
{
    public partial class FormDebug : Form, IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private Server server;

        public FormDebug()
        {
            InitializeComponent();

            log.Info("Start console");
            server = new Server();
            server.Configure();
            server.Start();
        }

        void IDisposable.Dispose()
        {
            server.Stop();
            log.Info("Stop console");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new CommandMessage { MessageType = MessageType.Info, Message = textBox1.Text }.ExecuteAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            new CommandSendMail { From = "<Risk (Test)> <noreply@corp.finam.ru>", To = "turyansky@corp.finam.ru", Subject = "Тест", Body = "Тестовое сообщение" }.ExecuteAsync();
        }
    }
}
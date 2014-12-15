using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NLog;

namespace Risk
{
    public partial class FormDebug : Form, IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private Server server;
        private ITable _table;

        public string TableName
        {
            get
            {
                return _table.Name;          
            }

            set
            {
                var obj = server.FindDataObject(value);
                if (obj != null)
                {
                    if (!(obj is ITable))
                        throw new Exception(String.Format("Object '{0}' is not table, type = '{1}'", value, obj.GetType().Name));
                    _table = (ITable)obj;
                    RefreshTable();
                }
            }
        }

        public FormDebug(bool safeMode)
        {
            InitializeComponent();

            log.Info("Start console");
            server = new Server();
            server.Configure();
            server.SafeMode = safeMode;
            server.Start();

            var tables = new CommandServerObjects { ObjectType = "Tables" }.Execute();
            comboBoxTable.Items.AddRange((object[])tables);

            TableName = comboBoxTable.Text;
        }

        void IDisposable.Dispose()
        {
            server.Stop();
            log.Info("Stop console");
        }

        public void RefreshTable()
        {
            int rowIndex = -1;
            int columnIndex = -1;
            if (grid.CurrentRow != null)
            {
                rowIndex = grid.CurrentCell.RowIndex;
                columnIndex = grid.CurrentCell.ColumnIndex;
            }

            var data = new CommandSelect { Object = server.FindDataObject(TableName) }.Execute();
            grid.DataSource = data;

            // Key fields read only
            if (!String.IsNullOrWhiteSpace(_table.KeyFieldNames))
                foreach (var keyField in _table.KeyFieldNames.Split(','))
                    if (grid.Columns.Contains(keyField))
                        grid.Columns[keyField].ReadOnly = true;

            if (rowIndex >= 0 && grid.RowCount > 0)
                if (rowIndex >= grid.RowCount)
                    grid.CurrentCell = grid[columnIndex, grid.RowCount - 1];
                else
                    grid.CurrentCell = grid[columnIndex, rowIndex];
        }

        private void buttonTableRefresh_Click(object sender, EventArgs e)
        {
            TableName = comboBoxTable.Text;
            RefreshTable();
        }

        private object NewRow(int index, int offset = 0)
        {
            Type itemType = _table.ObjectType;
            object result = Activator.CreateInstance(itemType);
            object currObj = index >= 0 ? ((Array)grid.DataSource).GetValue(index) : null;

            // Set key fields
            if (!String.IsNullOrWhiteSpace(_table.KeyFieldNames))
                foreach (var keyField in _table.KeyFieldNames.Split(','))
                {
                    var property = itemType.GetProperties().FirstOrDefault(p => p.Name == keyField);
                    if (property == null)
                        continue;

                    if (property.PropertyType == typeof(int))
                        property.SetValue(result, (index < 0 ? 0 : (int)property.GetValue(currObj)) + offset);
                    else
                        throw new Exception(String.Format("Not supported key field type '{0}'", property.PropertyType.Name));
                }
            return result;
        }

        private ArrayList SelectedItems()
        {            
            if (grid.SelectedCells.Count > 0)
            {
                var indexItems = new List<int>();
                foreach (DataGridViewCell cell in grid.SelectedCells)
                    if (!indexItems.Contains(cell.RowIndex))
                        indexItems.Add(cell.RowIndex);
                return new ArrayList((from i in indexItems select ((Array)grid.DataSource).GetValue(i)).ToArray());
            }
            else if (grid.CurrentRow != null)
            {
                var items = new ArrayList();
                items.Add(((Array)grid.DataSource).GetValue(grid.CurrentRow.Index));
                return items;
            }
            else
                return null;
        }

        private void buttonTableCreate_Click(object sender, EventArgs e)
        {
            var items = new ArrayList((int)tableCountEdit.Value);
            for (int i = 0; i < tableCountEdit.Value; i++)
                items.Add(NewRow(-1, i + 1));

            new CommandMerge
            {
                Object = server.FindDataObject(TableName),
                Data = items.ToArray(_table.ObjectType),
            }.Execute();

            RefreshTable();
        }

        private void buttonTableInsert_Click(object sender, EventArgs e)
        {
            var items = SelectedItems();

            if (items == null)
            {
                buttonTableAdd_Click(sender, e);
                return;
            }
         
            new CommandInsert
            {
                Object = server.FindDataObject(TableName),
                Data = NewRow(Array.IndexOf((Array)grid.DataSource, items[0]), -1),
            }.Execute();

            RefreshTable();
        }

        private void buttonTableAdd_Click(object sender, EventArgs e)
        {
            var items = SelectedItems();

            new CommandInsert
            {
                Object = server.DataObject(TableName),
                Data = NewRow(items == null ? -1 : Array.IndexOf((Array)grid.DataSource, items[items.Count - 1]), 1),
            }.Execute();

            RefreshTable();
        }

        private void buttonTableDelete_Click(object sender, EventArgs e)
        {
            var items = SelectedItems();
            if (items == null || items.Count == 0)
                return;

            new CommandDelete
            {
                Object = server.DataObject(TableName),
                Data = items,
            }.Execute();

            RefreshTable();
        }

        private void grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            new CommandUpdate
            {
                Object = server.DataObject(TableName),
                Data = ((Array)grid.DataSource).GetValue(e.RowIndex),
            }.Execute();
            // FOR DEBUG !!! RefreshTable();
        }

        private object RandomRow(int rowIndex)
        {
            Type itemType = _table.ObjectType;
            object result = Activator.CreateInstance(itemType);
            object currObj = rowIndex >= 0 ? ((Array)grid.DataSource).GetValue(rowIndex) : null;

            // Set key fields
            if (!String.IsNullOrWhiteSpace(_table.KeyFieldNames))
                foreach (var keyField in _table.KeyFieldNames.Split(','))
                {
                    var property = itemType.GetProperties().FirstOrDefault(p => p.Name == keyField);
                    if (property == null)
                        continue;

                    if (property.PropertyType == typeof(int))
                        property.SetValue(result, (rowIndex < 0 ? 0 : (int)property.GetValue(currObj)));
                    else
                        throw new Exception(String.Format("Not supported key field type '{0}'", property.PropertyType.Name));
                }

            // TODO: !!! 
            Random rnd = new Random();
            ((TestTableItem)result).Value += rnd.Next(1, 1000);           

            return result;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Random rnd = new Random();
            while (checkBox1.Checked)
            {
                for (int i = 0; i < 1000; i++)
                {
                    new CommandUpdate
                    {
                        Object = server.DataObject(TableName),
                        Data = RandomRow(rnd.Next(0, grid.RowCount)),
                    }.Execute();
                }
                RefreshTable();
                Application.DoEvents();
            }
        }

        private void buttonMail_Click(object sender, EventArgs e)
        {
            new CommandMessage { MessageType = MessageType.Info, Message = textBoxMessage.Text }.ExecuteAsync();
        }

        private void buttonMessageSend_Click(object sender, EventArgs e)
        {
            new CommandSendMail { From = textBoxMailFrom.Text, To = textBoxMailTo.Text, Subject = textBoxMailSubject.Text, Body = textBoxBody.Text }.ExecuteAsync();
        }

        private void buttonMessageSend_Click_1(object sender, EventArgs e)
        {
            new CommandMessage
            {
                Message = textBoxMessage.Text,
                MessageType = Risk.MessageType.Info,
            }.Execute();
        }
    }
}
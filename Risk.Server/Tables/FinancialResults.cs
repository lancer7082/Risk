using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Таблица FinancialResults
    /// </summary>
    [Table("FinancialResults", KeyFields = "TradeCode,SecCode")]
    public class FinancialResults : Table<FinancialResult>
    {
        #region Overrides of Table<FinancialResult>

        /// <summary>
        ///  Trigger on Add, Update, Delete
        /// </summary>
        public override void TriggerAfter(TriggerCollection<FinancialResult> items)
        {
            UpdatePositionsFinancialResults(items);
        }

        /// <summary>
        /// Обновление финреза в позициях
        /// </summary>
        /// <param name="items"></param>
        private static void UpdatePositionsFinancialResults(TriggerCollection<FinancialResult> items)
        {
            if (!Server.Positions.Any())
                return;

            var updatedPositions = new List<Position>();

            foreach (var finRes in items.Updated)
            {
                // для каждого финреза ищем соотвествующие ему позиции
                var positions = Server.Positions.Where(s => s.TradeCode == finRes.TradeCode && s.SecCode == finRes.SecCode).ToList();
                if (!positions.Any())
                    continue;

                // и добавляем в список,который затем отправим командой
                positions.ForEach(position =>
                {
                    position.FinResCurrencyDisplay = finRes.FinResCurrencyDisplay;
                    position.FinRes = finRes.FinRes;
                });
                updatedPositions.AddRange(positions);
            }

            // отправляем команды обновления полей финреза позиций
            ServerBase.Current.Execute(Command.Update("Positions", updatedPositions, "FinRes,FinResCurrencyDisplay"));
        }

        #endregion
    }
}

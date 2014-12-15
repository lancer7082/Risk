using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class TriggerPair<T>
        where T : new()
    {
        public T Inserted { get; private set; }
        public T Updated  { get; private set; }
        public T Deleted { get; private set; }

        public TriggerPair(T inserted, T deleted, PropertyInfo[] ignoreProperties = null)
        {
            if (inserted == null && deleted == null)
                throw new Exception("Trigger pair cannot be empty Inserted and Deleted");

            // Insert
            if (deleted == null)
            {
                this.Inserted = inserted;
                this.Updated = inserted;
            }

            // Delete
            else if (inserted == null)
            {
                this.Updated = deleted;
                this.Deleted = deleted;
            }

            // Update
            else
            {
                this.Inserted = inserted;
                this.Updated = deleted;
                this.Deleted = deleted.CloneObject();

                if (ignoreProperties != null)
                    foreach (var prop in ignoreProperties)
                    {
                        prop.SetValue(this.Inserted, prop.GetValue(deleted));
                    }
            }
        }

        public TriggerAction Action
        {
            get
            {
                if (Deleted == null)
                    return TriggerAction.Insert;
                else if (Inserted == null)
                    return TriggerAction.Delete;
                else
                    return TriggerAction.Update;
            }
        }

        public override string ToString()
        {
            var properties = typeof(T).GetProperties();
            return (Deleted == null ? "" : Deleted.ToString(properties, true)) + " => " + (Inserted == null ? "" : Inserted.ToString(properties, true));
        }
    }

    public enum TriggerAction
    {
        Insert,
        Update,
        Delete
    }
}
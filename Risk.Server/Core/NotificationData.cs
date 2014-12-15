using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
   public class NotificationData<T>
       where T : new()
   {
       public T[] Created { get; set; }
       public T[] Inserted { get; set; }
       public TriggerPair<T>[] Updated { get; set; }
       public T[] Deleted { get; set; }

       public bool IsEmpty 
       {
            get 
            {
                return (Inserted == null || Inserted.Length == 0)
                    && (Updated == null || Updated.Length == 0)
                    && (Deleted == null || Deleted.Length == 0);
            }
       }
   }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic.Shared
{
    public interface ICRUD<T>
    {
        T Create(T obj);
        T Read(int id);
        void Update(T obj);
        void Delete(T obj);
    }
}

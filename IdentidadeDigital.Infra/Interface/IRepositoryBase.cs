using System.Collections.Generic;

namespace IdentidadeDigital.Infra.Interface
{
    public interface IRepositoryBase<T> where T : class
    {
        void Insert(T obj);
        void Delete(T obj);
        void Update(T obj);
        ICollection<T> FindAll();
        T Find(int id);
        T Find(string id);
        void Dispose();
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using IdentidadeDigital.Infra.Interface;
using Microsoft.EntityFrameworkCore;

namespace IdentidadeDigital.Infra.Repository.Base
{
    public class RepositoryBase<T, D> : IDisposable, IRepositoryBase<T> where T : class where D : IdDigitalDbContext
    {
        //atributo para a classe de contexto com o BD 
        private readonly D _dataContext;

        protected RepositoryBase()
        {
            if (_dataContext == null)
                _dataContext = (D)new IdDigitalDbContext();
        }

        public void Insert(T obj)
        {
            _dataContext.Entry(obj).State = EntityState.Added;
            _dataContext.SaveChanges();
        }

        public void Delete(T obj)
        {
            _dataContext.Entry(obj).State = EntityState.Deleted;
            _dataContext.SaveChanges();
        }

        public void Update(T obj)
        {
            _dataContext.Entry(obj).State = EntityState.Modified;
            _dataContext.SaveChanges();
        }

        public ICollection<T> FindAll()
        {
            return _dataContext.Set<T>().ToList();
        }

        public T Find(int id)
        {
            return _dataContext.Set<T>().Find(id);
        }

        public T Find(string id)
        {
            return _dataContext.Set<T>().Find(id);
        }

        public void Dispose() //destrutor..
        {
            _dataContext.Dispose();
        }
    }
}




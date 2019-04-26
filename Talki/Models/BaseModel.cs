using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Talki.Models
{
    public sealed class BaseModel<T> : ICollection<byte> where T: class, IMessageModel, new()
    {
        public T InnerModel { get; set; }

        private List<byte> _bytes;
        public int Count => _bytes.Count();

        public bool IsReadOnly => true;

        static BaseModel()
        {

        }
        public BaseModel(T model) : this()
        {
            InnerModel = model;
        }
        public BaseModel()
        {
            _bytes = new List<byte>();
        }

        public static explicit operator BaseModel<T>(byte[] msg)
        {
            var m = new T();
            m.Load(msg);

            return new BaseModel<T>(m);
        }

        #region Collection Implementation

        public void Add(byte item)
        {
            _bytes.Add(item);
        }

        public void Clear()
        {
            _bytes.Clear();
        }

        public bool Contains(byte item)
        {
            return _bytes.Contains(item);
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            _bytes.CopyTo(array, arrayIndex);
        }

        public bool Remove(byte item)
        {
            return _bytes.Remove(item);
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return _bytes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
    }
}

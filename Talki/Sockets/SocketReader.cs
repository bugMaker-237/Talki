using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Talki.Sockets
{
    public class SocketReader : BinaryReader
    {
        private Type[] NumericTypes = new Type[]
        {
            typeof(int),
            typeof(short),
            typeof(long),
            typeof(double),
            typeof(float),
            typeof(decimal),
            typeof(uint),
            typeof(ulong),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(bool)
        };
        private Type[] CharacterTypes = new Type[]
        {
            typeof(char),
            typeof(string),
            typeof(DateTime)
        };

        public SocketReader(Stream input):base(input)
        {
        }
        public SocketReader(Stream input, Encoding encoding):base(input, encoding)
        {

        }
        public SocketReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {

        }
        public T ReadObject<T>()
        {
            //Rea
            throw new NotImplementedException();
        }
    }
}

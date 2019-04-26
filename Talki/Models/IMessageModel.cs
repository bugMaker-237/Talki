using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Talki.Models
{
    public interface IMessageModel
    {
        void Load(byte[] msg);

        byte[] GetBytes();
    }
}

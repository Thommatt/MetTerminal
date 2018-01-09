using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetTerminal2
{
    class VT100
    {
        public char TextToSend(byte b)
        {
            char c;

            c = pTextToSend(b);

            return c;
        }

        private char pTextToSend(byte b)
        {
            return 'c';
        }
    }
}

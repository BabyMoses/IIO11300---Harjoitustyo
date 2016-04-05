using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchBot
{
    class TwitchTV
    {
        public Stream stream;
        public class Stream
        {
            public long viewers { get; set; }

            public class Channel
            {
                public long followers { get; set; }
                public long views { get; set; }

               
            }
        }
    }
}


namespace TwitchBot
{
    class TwitchTV
    {
        public Stream stream { get; set; }


        public class Stream
        {
            
            public long viewers { get; set; }
            public Channel channel { get; set; }

            public class Channel
            {
                public long followers { get; set; }
                public long views { get; set; }

               
            }
        }
    }
}

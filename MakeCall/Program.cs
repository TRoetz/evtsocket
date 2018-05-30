using NEventSocket;
using NEventSocket.FreeSwitch;
using System;
using System.Reactive.Linq;

namespace MakeCall
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Connections.Subscribe(
                  async socket =>
                  {
                      await socket.Connect();

                      //after calling .Connect(), socket.ChannelData
                      //is populated with all the headers and variables of the channel

                      var uuid = socket.ChannelData.Headers[HeaderNames.UniqueId];
                      Console.WriteLine("OutboundSocket connected for channel " + uuid);

                      await socket.SubscribeEvents(EventName.ChannelHangup);

                      socket.ChannelEvents
                      .Where(x => x.EventName == EventName.ChannelHangup && x.UUID == uuid)
                      .Take(1)
                      .Subscribe(async x =>
                      {
                          Console.WriteLine("Hangup Detected on " + x.UUID);
                          await socket.Exit();
                      });


                      //if we use 'full' in our FS dialplan, we'll get events for ALL channels in FreeSwitch
                      //this is not desirable here - so we'll filter in for our unique id only
                      //cases where this is desirable is in the channel api where we want to catch other channels bridging to us
                      await socket.Filter(HeaderNames.UniqueId, uuid);

                      //tell FreeSwitch not to end the socket on hangup, we'll catch the hangup event and .Exit() ourselves
                      await socket.Linger();

                      await socket.ExecuteApplication(uuid, "answer");
                      await socket.Play(uuid, "misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                      await socket.Hangup(uuid, HangupCause.NormalClearing);
                  });

                listener.Start();

                Console.WriteLine("Press [Enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace RobotDrummer
{
    public class Beat
    {
        public OutputPort port { get; set; }
        public int dueTime { get; set; }
        public int period { get; set; }
        public Timer timer { get; set; }

        public Beat(OutputPort port, int dueTime)
        {
            this.port = port;
            this.dueTime = dueTime;
            this.period = Timeout.Infinite; 

            //TODO: on create don't play the beat so dueTime is infinite
            timer = new Timer(play, null, this.dueTime, this.period);
        }

        public void play(object data)
        {
            port.Write(true);
            Thread.Sleep(50);
            port.Write(false);
        }

        public void update(float patternTime)
        {
            // since period is infinite, it just schedules 1 hit
            // dueTime's scale is assuming 1600 millis per measure (400 per beat)
            // instead of overwriting duetime, always store as same scale and just adjust based on patternTime
            int adjustedDueTime = (int)(((float)dueTime / 1600) * patternTime);
            timer.Change(adjustedDueTime, period);
        }
    }
}

using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace RobotDrummer
{
    public class BeatPattern
    {
        public ArrayList beatList { get; set; }
        public ArrayList drumList { get; set; }
        public float patternTime { get; set; }

        public BeatPattern(ArrayList dList)
        {
            drumList = dList;
            beatList = new ArrayList();
        }

        public void generate(float pTime)
        {
            this.patternTime = pTime;

            addBeat(1, 0);
            addBeat(0, 200);
            addBeat(2, 400);
            addBeat(1, 600);
            addBeat(0, 800);
            addBeat(2, 1200);
            addBeat(1, 1400);
        }

        private void addBeat(int drum, int millis)
        {
            beatList.Add(new Beat((OutputPort)drumList[drum], millis));
        }

        internal void update(float pTime)
        {
            patternTime = pTime;

            // about 1/2 the measures, skip a random beat
            Random r = new Random();
            int skipBeat = r.Next(beatList.Count*2); 
            foreach (Beat b in beatList)
            {
                //if (skipBeat != beatList.IndexOf(b))
                    b.update(patternTime);
            }
        }
    }
}

using System;
using Microsoft.SPOT;
using System.Threading;
using SecretLabs.NETMF.Hardware;
using Microsoft.SPOT.Hardware;
using System.Collections;
using System.Diagnostics;

namespace RobotDrummer
{
    public class KnockDetector
    {
        private AnalogInput knockSensor { get; set; }
        private OutputPort knockLed { get; set; }
        private OutputPort calcLed { get; set; }
        private float beatInterval { get; set; }

        private ArrayList knockList { get; set; }
        private int knockListSize { get; set; }
        private int knockThreshVolume { get; set; }
        private int lastKnockVolume { get; set; }
        private bool knockRisingEdge { get; set; }

        private int minBeatInterval = 375;  // or 160 bpm
        private int maxBeatInterval = 750;  // or 80 bpm

        private Stopwatch stopwatch { get; set; }

        public KnockDetector()
        {
            this.beatInterval = 500;
        }

        public KnockDetector(AnalogInput ai, OutputPort opKnockLed, OutputPort opCalcLed)
        {
            beatInterval = 500;
            knockList = new ArrayList();
            knockListSize = 10;
            knockSensor = ai;
            knockLed = opKnockLed;
            calcLed = opCalcLed;
            knockThreshVolume = 75;
            lastKnockVolume = 0;
            knockRisingEdge = false;

            stopwatch = Stopwatch.StartNew();
            stopwatch.Stop();
            stopwatch.Reset();
        }

        public float getBpm()
        {
            return beatIntervalToBpm(beatInterval);
        }

        public float getBeatInterval()
        {
            // bpm / 60 = beats per second
            // 1/bps = seconds per beat
            // spb * 1000 = millis per beat
            //return (1 / (getBpm() / 60)) * 1000;

            return beatInterval;
        }

        public float beatIntervalToBpm(float f)
        {
            return 60000 / f;
        }

        public float bpmToBeatInterval(float f)
        {
            return 60000 / f;
        }

        public void detectLoop()
        {
            while (true)
            {
                detectKnock();
                Thread.Sleep(10);
            }
        }

        public void calculateLoop()
        {
            while (true)
            {
                calculateBeatInterval();
                Thread.Sleep(2000);
            }
        }

        public bool detectKnock()
        {
            // take a reading
            int currentKnockVolume = knockSensor.Read();

            // if below thresh, consider it 0 volume
            // this is the best way to deal with the threshold because 0 values 
            // need to be processed just like others, see logic below
            if (currentKnockVolume < knockThreshVolume)
                currentKnockVolume = 0;
            else
                Debug.Print(currentKnockVolume.ToString());

            bool knockSaved = false;
            if (knockRisingEdge)
            {
                if (currentKnockVolume > lastKnockVolume)
                {
                    // do nothing
                }
                else
                {
                    knockRisingEdge = false;
                    knockLed.Write(false);
                    SaveKnock(lastKnockVolume);
                    knockSaved = true;
                }
            }
            else
            {
                if (currentKnockVolume > lastKnockVolume)
                {
                    knockRisingEdge = true;
                    knockLed.Write(true);
                }
                else
                {
                    // do nothing
                }
            }

            lastKnockVolume = currentKnockVolume;
            return knockSaved;
        }

        void SaveKnock(int knockVolume)
        {
            long timeSinceKnock = !stopwatch.IsRunning() ? 0 : stopwatch.ElapsedMilliseconds;

            // if waited a while, reset knockList
            if (timeSinceKnock > 6000)
            {
                //knockList.Clear();  // better to reinstantiate?
                knockList = new ArrayList();
                timeSinceKnock = 0;
            }

            // if reached size limit, pop off first knock
            if (knockList.Count > knockListSize)
                knockList.RemoveAt(0);

            knockList.Add(new Knock() { volume = knockVolume, interval = (int)timeSinceKnock });
            Debug.Print("KNOCK STORED: " + knockVolume + ", " + timeSinceKnock);

            // reset stopwatch
            stopwatch.Stop();
            stopwatch.Reset();
            stopwatch.Start();
        }

        public void calculateBeatInterval()
        {
            if (knockList.Count < knockListSize)
                return;

            calcLed.Write(true);

            ArrayList knockListCopy = (ArrayList)knockList.Clone();
            ArrayList adjustedKnocks = new ArrayList();
            foreach (Knock k in knockListCopy)
            {
                int i = knockListCopy.IndexOf(k);
                if (true)  // put a check to see if knock is valid or null
                {
                    // add intervals between current beat and prior beat, 2 beats ago, and 3 beats ago
                    if (i > 0)
                        adjustedKnocks.Add(adjustBeatInterval(k.interval));
                    if (i > 1)
                        adjustedKnocks.Add(adjustBeatInterval(k.interval + ((Knock)knockListCopy[i - 1]).interval));
                    if (i > 3)
                        adjustedKnocks.Add(adjustBeatInterval(k.interval + ((Knock)knockListCopy[i - 1]).interval + ((Knock)knockListCopy[i - 2]).interval + ((Knock)knockListCopy[i - 3]).interval));
                }
            }

            ArrayList sortedKnocks = ((ArrayList)adjustedKnocks.Clone()).SortInts();
            ArrayList cleanerKnocks = ((ArrayList)sortedKnocks.Clone()).RemoveOutlierInts();

            int avgKnockInterval = cleanerKnocks.GetAvgInt();

            this.beatInterval = avgKnockInterval;

            calcLed.Write(false);
        }

        public int adjustBeatInterval(int i)
        {
            while (i > maxBeatInterval + 50)
                i = i / 2;
            while (i < minBeatInterval - 50)
                i = i * 2;
            return i;
        }

        public void detectKnock1()
        {
            // take a reading
            int currentKnockVolume = knockSensor.Read();

            // if it's loud enough
            if (currentKnockVolume > knockThreshVolume)
            {
                // show knock on led
                Debug.Print(currentKnockVolume.ToString());
                knockLed.Write(true);

                // wait till readings die away
                int maxKnockVolume = currentKnockVolume;
                while (currentKnockVolume >= knockThreshVolume)
                {
                    if (currentKnockVolume >= maxKnockVolume)
                        maxKnockVolume = currentKnockVolume;

                    // wait a moment then take a reading
                    Thread.Sleep(10);
                    currentKnockVolume = knockSensor.Read();
                    Debug.Print(currentKnockVolume.ToString());
                }

                // save the loudest knock
                //int timeSinceKnock = knockList.Count == 0 ? 0 : ((Knock)(knockList[knockList.Count - 1])).interval;
                long timeSinceKnock = !stopwatch.IsRunning() ? 0 : stopwatch.ElapsedMilliseconds;

                // this is to ignore some left false positives that occur very soon after some beats
                if (timeSinceKnock == 0 || timeSinceKnock >= 40)
                {
                    // if waited a while, restart recording knocks
                    if (timeSinceKnock > 6000)
                    {
                        // reset knockList 
                        knockList.Clear();
                        timeSinceKnock = 0;
                    }

                    // if reached size limit, pop off first knock
                    if (knockList.Count > 140)
                        knockList.RemoveAt(0);

                    knockList.Add(new Knock() { volume = maxKnockVolume, interval = (int)timeSinceKnock });
                    Debug.Print("KNOCK STORED: " + maxKnockVolume + ", " + timeSinceKnock);

                    stopwatch.Stop();
                    stopwatch.Reset();
                    stopwatch.Start();

                    knockLed.Write(false);
                }
            }
            Thread.Sleep(10);
        }
    }
}

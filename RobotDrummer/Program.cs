/*
 * RobotDrummer
 * 
 * Ahmad Saeed
 * http://niltoid.com
 * 7/17/2012
 * 
 * Notes:
 *  - in debug mode, all timings get thrown off, but works fine when deployed
 * 
 * 
 */


using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Collections;

namespace RobotDrummer
{
    public class Program
    {
        OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
        OutputPort solenoid1 = new OutputPort(Pins.GPIO_PIN_D7, false);
        OutputPort solenoid2 = new OutputPort(Pins.GPIO_PIN_D8, false);
        OutputPort solenoid3 = new OutputPort(Pins.GPIO_PIN_D9, false);
        AnalogInput knockSensor = new AnalogInput(Pins.GPIO_PIN_A0);
        OutputPort knockLed = new OutputPort(Pins.GPIO_PIN_D13, false);
        OutputPort calcLed = new OutputPort(Pins.GPIO_PIN_D12, false);

        ArrayList solenoidList;

        BeatPattern beatPattern;

        KnockDetector knockDetector;
        Thread knockDetectorThread;
        Thread knockCalculatorThread;
        float patternTime;

        public static void Main()
        {
            new Program().Run();
        }

        private void Run()
        {
            Debug.Print("setup begin...");
            setup();
            Debug.Print("setup end...");

            while (true)
            {
                //Debug.Print("loop begin...");
                loop();
                //Debug.Print("loop end...");
            }
        }

        public void setup()
        {
            // form a list of OutputPorts corrensponding to each solenoid
            solenoidList = new ArrayList() { solenoid1, solenoid2, solenoid3 };

            // init patternTime
            patternTime = 2000;  // or 120 bpm

            // pass it in to a new beatPattern
            beatPattern = new BeatPattern(solenoidList);
            beatPattern.generate(patternTime);

            // init knockDetector and spawn it's loop in a separate thread
            knockDetector = new KnockDetector(knockSensor, knockLed, calcLed);
            knockDetectorThread = new Thread(delegate() { knockDetector.detectLoop(); });
            knockDetectorThread.Start();
            knockCalculatorThread = new Thread(delegate() { knockDetector.calculateLoop(); });
            knockCalculatorThread.Start();

        }

        public void loop()
        {
            // wait one measure
            led.Write(true);
            Thread.Sleep((int)patternTime / 2);
            led.Write(false);
            Thread.Sleep((int)patternTime / 2);

            // time for 1 beat * 4 beats per measure
            patternTime = knockDetector.getBeatInterval() * 4;
 
            // set the bpm for the next measure
            beatPattern.update(patternTime);

            //timer0.Change(Timeout.Infinite, 0); // stop this one from firing
            //timer1.Change(0, Timeout.Infinite); // make this one fire once
        }

    }
}

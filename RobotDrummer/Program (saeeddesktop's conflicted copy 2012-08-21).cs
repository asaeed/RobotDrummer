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

        OutputPort modeLed = new OutputPort(Pins.GPIO_PIN_D11, false);
        InputPort modeButton = new InputPort(Pins.GPIO_PIN_D2, false, Port.ResistorMode.Disabled);
        Thread modeButtonThread;

        ArrayList solenoidList;

        BeatPattern beatPattern;

        KnockDetector knockDetector;
        Thread knockDetectorThread;
        Thread knockCalculatorThread;
        float patternTime;
        bool recordMode;

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
            //knockCalculatorThread = new Thread(delegate() { knockDetector.calculateLoop(); });
            //knockCalculatorThread.Start();

            modeButtonThread = new Thread(delegate() { modeButtonLoop(); });
            modeButtonThread.Start();
            recordMode = false;
        }

        public void loop()
        {
            if (recordMode)
                return;

            // set the bpm for the next measure
            beatPattern.update(patternTime);

            // wait one measure
            led.Write(true);
            Thread.Sleep((int)patternTime / 2);
            led.Write(false);
            Thread.Sleep((int)patternTime / 2);

            // time for 1 beat * 4 beats per measure
            patternTime = knockDetector.getBeatInterval() * 4;
 
            

            //timer0.Change(Timeout.Infinite, 0); // stop this one from firing
            //timer1.Change(0, Timeout.Infinite); // make this one fire once
        }

        private void modeButtonLoop()
        {
            while (true)
            {
                if (modeButton.Read())
                {
                    recordMode = !recordMode;
                    modeLed.Write(recordMode);

                    if (recordMode)
                    {
                        knockDetector.enabled = true;
                        
                    }
                    else
                    {
                        knockDetector.enabled = false;
                        knockCalculatorThread = new Thread(delegate() { knockDetector.calculateBeatInterval(); });
                        knockCalculatorThread.Start();
                    }

                    Thread.Sleep(200);
                }
                
                Thread.Sleep(10);
            }
        }
    }
}

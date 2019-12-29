﻿using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CP.IO.Ports.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var comPortName = "COM1";
            // configure the data to write, this can be a string, a byte array, or a char array
            var dataToWrite = "DataToWrite";
            var dis = new CompositeDisposable();
            // Setup the start of message and end of message
            var startChar = (0x21).AsObservable();
            var endChar = (0x0a).AsObservable();
            // Create a disposable for each COM port to allow automatic disposal upon loss of COM port
            var comdis = new CompositeDisposable();
            // Subscribe to com ports available
            SerialPortRx.PortNames().Do(x => {
                if (comdis.Count == 0 && x.Contains(comPortName)) {
                    comdis = new CompositeDisposable();
                    // Create a port
                    using var port = new SerialPortRx(comPortName, 9600);
                    // Subscribe to Exceptions from port
                    port.ErrorReceived.Subscribe(Console.WriteLine).AddTo(comdis);
                    port.IsOpenObservable.Subscribe(x => Console.WriteLine($"Port {comPortName} is {(x ? "Open" : "Closed")}")).AddTo(comdis);
                    // Subscribe to the Data Received
                    port.DataReceived.BufferUntil(startChar, endChar, 100).Subscribe(data => {
                        Console.WriteLine(data);
                    }).AddTo(comdis);
                    // Subscribe to the Is Open @500ms intervals and write to com port
                    port.WhileIsOpen(TimeSpan.FromMilliseconds(500)).Subscribe(x => {
                        port.Write(dataToWrite);
                    }).AddTo(comdis);
                    // Open the Com Port after subscriptions created
                    port.Open();
                } else {
                    comdis.Dispose();
                    Console.WriteLine($"Port {comPortName} Disposed");
                }
            }).ForEach().Subscribe(name => {
                // Show available ports
                Console.WriteLine(name);
            }).AddTo(dis);
            Console.ReadLine();
            // Cleanup ports
            comdis.Dispose();
            dis.Dispose();
        }
    }
}

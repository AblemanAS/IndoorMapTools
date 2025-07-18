using ILGPU;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IndoorMapTools.Core
{
    public class GPUAcc
    {
        private static readonly AcceleratorType[] DEVICE_PRIORITY =
            { AcceleratorType.Cuda, AcceleratorType.OpenCL, AcceleratorType.CPU };

        /// <summary>  
        /// Prints information on the given accelerator.  
        /// </summary>  
        /// <param name="accelerator">The target accelerator.</param>  
        static void PrintAcceleratorInfo(Accelerator accelerator)
        {
            Console.WriteLine($"Name: {accelerator.Name}");
            Console.WriteLine($"MemorySize: {accelerator.MemorySize}");
            Console.WriteLine($"MaxThreadsPerGroup: {accelerator.MaxNumThreadsPerGroup}");
            Console.WriteLine($"MaxSharedMemoryPerGroup: {accelerator.MaxSharedMemoryPerGroup}");
            Console.WriteLine($"MaxGridSize: {accelerator.MaxGridSize}");
            Console.WriteLine($"MaxConstantMemory: {accelerator.MaxConstantMemory}");
            Console.WriteLine($"WarpSize: {accelerator.WarpSize}");
            Console.WriteLine($"NumMultiprocessors: {accelerator.NumMultiprocessors}");
        }


        private Context context;

        /// <summary>  
        /// Detects all available accelerators and prints device information about each  
        /// of them on the command line.  
        /// </summary>  
        public void Test()
        {
            Device selectedDevice = null;

            context = Context.CreateDefault();

            foreach(AcceleratorType desiredDeviceType in DEVICE_PRIORITY)
            {
                foreach(Device device in context)
                {
                    if(device.AcceleratorType == desiredDeviceType)
                    {
                        selectedDevice = device;
                        break;
                    }
                }

                if(selectedDevice != null) break;
            }

            // Create accelerator for the given device.  
            // Note that all accelerators have to be disposed before the global context is disposed  
            using var accelerator = selectedDevice.CreateAccelerator(context);
            Console.WriteLine($"Accelerator: {selectedDevice.AcceleratorType}, {accelerator.Name}");
            PrintAcceleratorInfo(accelerator);
            Console.WriteLine();
        }

        ~GPUAcc()
        {
            // Dispose of the accelerator and context when done.  
            // Note that all accelerators have to be disposed before the global context is disposed  
            // accelerator.Dispose();
            context.Dispose();
        }
    }
}

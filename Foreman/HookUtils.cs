namespace Foreman
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal static class HookUtils
    {
        public static MemoryPatch HookMethod(MethodInfo source, MethodInfo target)
        {
            if (!CompatibleSignatures(source, target))
                throw new InvalidOperationException($"Incompatible method signatures: {source} <-> {target}");

            RuntimeHelpers.PrepareMethod(source.MethodHandle);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);
            IntPtr srcAddr = source.MethodHandle.GetFunctionPointer();
            IntPtr tgtAddr = target.MethodHandle.GetFunctionPointer();

            var arch = GetSystemProcessorArchitecture();

            long offset;
            byte[] jumpInst;
            switch (arch) {
                case ProcessorArchitecture.Amd64:
                    offset = tgtAddr.ToInt64() - srcAddr.ToInt64() - 5;
                    if (offset >= Int32.MinValue && offset <= Int32.MaxValue) {
                        jumpInst = new byte[] {
                            0xE9, // JMP rel32
                            (byte)(offset & 0xFF),
                            (byte)((offset >> 8) & 0xFF),
                            (byte)((offset >> 16) & 0xFF),
                            (byte)((offset >> 24) & 0xFF)
                        };
                    } else {
                        offset = tgtAddr.ToInt64();
                        jumpInst = new byte[] {
                            0x48, 0xB8, // MOV moffs64,rax
                            (byte)(offset & 0xFF),
                            (byte)((offset >> 8) & 0xFF),
                            (byte)((offset >> 16) & 0xFF),
                            (byte)((offset >> 24) & 0xFF),
                            (byte)((offset >> 32) & 0xFF),
                            (byte)((offset >> 40) & 0xFF),
                            (byte)((offset >> 48) & 0xFF),
                            (byte)((offset >> 56) & 0xFF),
                            0xFF, 0xE0 // JMP rax
                        };
                    }
                    break;

                case ProcessorArchitecture.X86:
                    offset = tgtAddr.ToInt32() - srcAddr.ToInt32() - 5;
                    jumpInst = new byte[] {
                        0xE9, // JMP rel32
                        (byte)(offset & 0xFF),
                        (byte)((offset >> 8) & 0xFF),
                        (byte)((offset >> 16) & 0xFF),
                        (byte)((offset >> 24) & 0xFF)
                    };
                    break;

                default:
                    throw new NotSupportedException(
                        $"Processor architecture {arch} is not supported.");
            }

            return PatchMemory(srcAddr, jumpInst);
        }

        private const int PAGE_EXECUTE_READWRITE = 0x40;

        private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
        private const int PROCESSOR_ARCHITECTURE_ARM = 5;
        private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const int PROCESSOR_ARCHITECTURE_INTEL = 0;

        private static ProcessorArchitecture GetSystemProcessorArchitecture()
        {
            var si = new SYSTEM_INFO();
            GetNativeSystemInfo(ref si);
            switch (si.wProcessorArchitecture) {
                case PROCESSOR_ARCHITECTURE_AMD64:
                    return ProcessorArchitecture.Amd64;

                case PROCESSOR_ARCHITECTURE_IA64:
                    return ProcessorArchitecture.IA64;

                case PROCESSOR_ARCHITECTURE_INTEL:
                    return ProcessorArchitecture.X86;

                case PROCESSOR_ARCHITECTURE_ARM:
                    return ProcessorArchitecture.Arm;

                default:
                    return ProcessorArchitecture.None;
            }
        }

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        private static extern UIntPtr VirtualQuery(
            IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer,
            IntPtr dwLength);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        private static extern bool VirtualProtect(
            IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        private static extern bool FlushInstructionCache(
            IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        private static UIntPtr VirtualQuery(
            IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer)
        {
            return VirtualQuery(
                lpAddress, out lpBuffer,
                (IntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public short wProcessorArchitecture;
            public short wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public UIntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public short wProcessorLevel;
            public short wProcessorRevision;
        }

        public sealed class MemoryPatch : IDisposable
        {
            private readonly IntPtr address;
            private readonly byte[] backupInstructions;

            public MemoryPatch(IntPtr address, byte[] backupInstructions)
            {
                this.address = address;
                this.backupInstructions = backupInstructions;
            }

            public void Dispose()
            {
                WriteMemory(address, backupInstructions);
            }
        }

        private static MemoryPatch PatchMemory(IntPtr address, byte[] instructions)
        {
            var backup = new byte[instructions.Length];
            Marshal.Copy(address, backup, 0, backup.Length);
            if (backup.Any(x => x == 0xCC))
                throw new InvalidOperationException(
                    "Refusing to patch memory due to breakpoints/INT3 in target memory.");

            WriteMemory(address, instructions);
            return new MemoryPatch(address, backup);
        }

        private static void WriteMemory(IntPtr address, byte[] instructions)
        {
            if (VirtualQuery(address, out MEMORY_BASIC_INFORMATION mbi) == UIntPtr.Zero)
                throw new Win32Exception();

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            } finally {
                if (!VirtualProtect(mbi.BaseAddress, mbi.RegionSize, PAGE_EXECUTE_READWRITE, out uint oldProtect))
                    throw new Win32Exception();
                Marshal.Copy(instructions, 0, address, instructions.Length);
                FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)instructions.Length);
                VirtualProtect(mbi.BaseAddress, mbi.RegionSize, oldProtect, out oldProtect);
            }
        }

        private static bool CompatibleSignatures(MethodInfo src, MethodInfo tgt)
        {
            if (src.ReturnType != tgt.ReturnType)
                return false;

            ParameterInfo[] srcParams = src.GetParameters();
            ParameterInfo[] tgtParams = tgt.GetParameters();

            if (src.IsStatic && tgt.IsStatic) {
                if (src.CallingConvention != tgt.CallingConvention)
                    return false;
                if (srcParams.Length != tgtParams.Length)
                    return false;

                for (int i = 0; i < srcParams.Length; ++i) {
                    if (srcParams[i].ParameterType != tgtParams[i].ParameterType)
                        return false;
                }

                return true;
            }

            if (!src.IsStatic && tgt.IsStatic) {
                if ((src.CallingConvention & ~CallingConventions.HasThis) != tgt.CallingConvention)
                    return false;
                if (srcParams.Length + 1 != tgtParams.Length)
                    return false;
                if (tgtParams[0].ParameterType != typeof(object) &&
                    tgtParams[0].ParameterType != src.DeclaringType)
                    return false;

                for (int i = 0; i < srcParams.Length; ++i) {
                    if (srcParams[i].ParameterType != tgtParams[i + 1].ParameterType)
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}
namespace Foreman
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal static class HookUtils
    {
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

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32")]
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

        private const int PAGE_EXECUTE_READWRITE = 0x40;

        private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
        private const int PROCESSOR_ARCHITECTURE_ARM = 5;
        private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const int PROCESSOR_ARCHITECTURE_INTEL = 0;

        private static ProcessorArchitecture GetProcessorArchitecture()
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

        private static bool HasTieredCompilation()
        {
            var frameworkDescriptionProperty = Type.GetType(
                    "System.Runtime.InteropServices.RuntimeInformation, System.Runtime.InteropServices.RuntimeInformation")
                ?.GetProperty("FrameworkDescription");

            if (frameworkDescriptionProperty == null)
                return false; // < .NET 4.7.1

            var frameworkDescription = (string)frameworkDescriptionProperty.GetValue(null)!;
            bool isNetFx = frameworkDescription.StartsWith(
                ".NET Framework", StringComparison.OrdinalIgnoreCase);
            return !isNetFx;
        }

        public static MemoryPatch HookMethod(MethodInfo original, MethodInfo replacement)
        {
            if (!CompatibleSignatures(original, replacement))
                throw new InvalidOperationException($"Incompatible method signatures: {original} <-> {replacement}");

            if (HasTieredCompilation())
                original.DisableTieredCompilation();

            IntPtr orgAddr = PrepareMethod(original);
            IntPtr repAddr = PrepareMethod(replacement);

            var arch = GetProcessorArchitecture();

            long offset;
            byte[] jumpInst;
            switch (arch) {
                case ProcessorArchitecture.Amd64:
                    offset = repAddr.ToInt64() - orgAddr.ToInt64() - 5;
                    if (offset is >= int.MinValue and <= int.MaxValue) {
                        jumpInst = new byte[] {
                            0xE9, // JMP rel32
                            (byte)(offset & 0xFF),
                            (byte)((offset >> 8) & 0xFF),
                            (byte)((offset >> 16) & 0xFF),
                            (byte)((offset >> 24) & 0xFF)
                        };
                    } else {
                        offset = repAddr.ToInt64();
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
                    offset = repAddr.ToInt32() - orgAddr.ToInt32() - 5;
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

            return PatchMemory(orgAddr, jumpInst);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FixupPrecode
        {
            [FieldOffset(0)]
            public byte m_op;
            [FieldOffset(1)]
            public int m_rel32;
            [FieldOffset(5)]
            public byte m_type;
            [FieldOffset(6)]
            public byte m_MethodDescChunkIndex;
            [FieldOffset(7)]
            public byte m_PrecodeChunkIndex;

            public const byte TypePrestub = 0x5E;
            public const byte Type = 0x5F;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct MethodDesc
        {
            [FieldOffset(0)]
            public ushort m_wFlags3AndTokenRemainder;
            [FieldOffset(2)]
            public byte m_chunkIndex;
            [FieldOffset(3)]
            public byte m_bFlags2;
            [FieldOffset(4)]
            public ushort m_wSlotNumber;
            [FieldOffset(6)]
            public ushort m_wFlags;

            public bool IsEligibleForTieredCompilation()
            {
                return (m_bFlags2 & (byte)MethodDescFlags2.IsEligibleForTieredCompilation) != 0;
            }

            public void SetEligibleForTieredCompilation(bool value)
            {
                if (value)
                    m_bFlags2 |= (byte)MethodDescFlags2.IsEligibleForTieredCompilation;
                else
                    m_bFlags2 &= unchecked((byte)~MethodDescFlags2.IsEligibleForTieredCompilation);
            }
        }

        [Flags]
        private enum MethodDescFlags2 : ushort
        {
            IsEligibleForTieredCompilation = 0x20
        }

        private static unsafe MethodDesc* GetMethodDesc(this RuntimeMethodHandle handle)
        {
            IntPtr addr = handle.GetFunctionPointer();
            var precode = (FixupPrecode*)addr;

            const byte callRel32 = 0xE8;
            const byte jmpRel32 = 0xE9;

            bool validOp = precode->m_op == jmpRel32 || precode->m_op == callRel32;
            bool validType = precode->m_type == FixupPrecode.TypePrestub || precode->m_type == FixupPrecode.Type;

            if (!validOp || !validType)
                return null;

            byte methodDescChunkIndex = precode->m_MethodDescChunkIndex;
            byte precodeChunkIndex = precode->m_PrecodeChunkIndex;

            // coreclr/src/vm/i368/stublinkerx86.h: FixupPrecode::GetBase
            // coreclr/src/vm/i368/stublinkerx86.cpp: FixupPrecode::GetMethodDesc
            IntPtr methodDescChunkBase = *(IntPtr*)(addr + (precodeChunkIndex + 1) * sizeof(FixupPrecode));
            IntPtr methodDescAddr = methodDescChunkBase + methodDescChunkIndex * sizeof(void*);

            return (MethodDesc*)methodDescAddr;
        }

        private static unsafe void DisableTieredCompilation(this MethodBase method)
        {
            var methodDesc = method.MethodHandle.GetMethodDesc();
            if (methodDesc == null)
                return;
            methodDesc->SetEligibleForTieredCompilation(false);

            Debug.WriteLine(
                "Disabled tiered compilation for method {0}.{1}",
                method.DeclaringType?.FullName, method.Name);
        }

        [Conditional("DEBUG")]
        private static unsafe void DumpMethodHandle(MethodBase method)
        {
            var methodHandle = method.MethodHandle;
            var methodDesc = methodHandle.GetMethodDesc();
            var precode = (FixupPrecode*)methodHandle.GetFunctionPointer();

            var data = new byte[8];
            Marshal.Copy(methodHandle.GetFunctionPointer(), data, 0, data.Length);

            Debug.WriteLine(
                $"Dump of {method.DeclaringType?.FullName}.{method.Name}" +
                $"\n  Address: {methodHandle.GetFunctionPointer()}" +
                $"\n  Bytes: {data[0]:X2} {data[1]:X2} {data[2]:X2} {data[3]:X2} {data[4]:X2} {data[5]:X2} {data[6]:X2} {data[7]:X2}" +
                $"\n  PreCode: {(long)precode:X8}" +
                $"\n    m_op:    {precode->m_op:X2}" +
                $"\n    m_rel32: {precode->m_rel32:X4}" +
                $"\n    m_type:  {precode->m_type:X2}" +
                $"\n    m_MDCI:  {precode->m_MethodDescChunkIndex:X2}" +
                $"\n    m_PCCI:  {precode->m_PrecodeChunkIndex:X2}" +
                (methodDesc != null ?
                    $"\n  MethodDesc: {(long)methodDesc:X8}" +
                    $"\n    m_chunkIndex: {methodDesc->m_chunkIndex:X2}" +
                    $"\n    m_bFlags2: {methodDesc->m_bFlags2:X2} (Tiered: {methodDesc->IsEligibleForTieredCompilation()})"
                    : "\n  MethodDesc: ???")
               );

            Debug.Flush();
        }

        private static IntPtr PrepareMethod(MethodBase method)
        {
            var methodHandle = method.MethodHandle;
            DumpMethodHandle(method);
            RuntimeHelpers.PrepareMethod(methodHandle);
            DumpMethodHandle(method);
            return methodHandle.GetFunctionPointer();
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
                var current = new byte[backupInstructions.Length];
                Marshal.Copy(address, current, 0, current.Length);
                if (!current.SequenceEqual(backupInstructions)) {
                    Debug.WriteLine(
                        "Refusing to revert patch because the patched instructions have been modified.");
                    return;
                }

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

            if (!VirtualProtect(mbi.BaseAddress, mbi.RegionSize, PAGE_EXECUTE_READWRITE, out uint oldProtect))
                throw new Win32Exception();
            Marshal.Copy(instructions, 0, address, instructions.Length);
            FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)instructions.Length);
            VirtualProtect(mbi.BaseAddress, mbi.RegionSize, oldProtect, out oldProtect);
        }

        private static bool CompatibleSignatures(MethodInfo x, MethodInfo y)
        {
            if (x.ReturnType != y.ReturnType)
                return false;

            ParameterInfo[] paramsX = x.GetParameters();
            ParameterInfo[] paramsY = y.GetParameters();

            if (x.IsStatic && y.IsStatic) {
                if (x.CallingConvention != y.CallingConvention)
                    return false;
                if (paramsX.Length != paramsY.Length)
                    return false;

                for (int i = 0; i < paramsX.Length; ++i) {
                    if (paramsX[i].ParameterType != paramsY[i].ParameterType)
                        return false;
                }

                return true;
            }

            if (!x.IsStatic && y.IsStatic) {
                if ((x.CallingConvention & ~CallingConventions.HasThis) != y.CallingConvention)
                    return false;
                if (paramsX.Length + 1 != paramsY.Length)
                    return false;
                if (paramsY[0].ParameterType != typeof(object) &&
                    paramsY[0].ParameterType != x.DeclaringType)
                    return false;

                for (int i = 0; i < paramsX.Length; ++i) {
                    if (paramsX[i].ParameterType != paramsY[i + 1].ParameterType)
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}

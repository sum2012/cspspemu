﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CSPspEmu.Core.Memory
{
	public unsafe sealed class FastPspMemoryUnsafe : PspMemory
	{
		override public bool HasFixedGlobalAddress { get { return true; } }
		override public IntPtr FixedGlobalAddress { get { return new IntPtr(_Base); } }

		//public readonly byte* Base = (byte*)0x50000000;
		//public readonly byte* Base = (byte*)0x40000000;
		public static byte* _Base = null;
		public static byte* StaticNullPtr;
		public static byte* StaticScratchPadPtr;
		public static byte* StaticFrameBufferPtr;
		public static byte* StaticMainPtr;

		////[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public byte* Base { get { return _Base; } }

		/*
		// to RESERVE memory in Linux, use mmap with a private, anonymous, non-accessible mapping.
		// The following line reserves 1gb of ram starting at 0x10000000.

		void* result = mmap((void*)0x10000000, 0x40000000, PROT_NONE, MAP_PRIVATE | MAP_ANON, -1, 0);

		// to COMMIT memory in Linux, use mprotect on the range of memory you'd like to commit, and
		// grant the memory READ and/or WRITE access.
		// The following line commits 1mb of the buffer.  It will return -1 on out of memory errors.

		int result3 = mprotect((void*)0x10000000, 0x100000, PROT_READ | PROT_WRITE);
		*/

		public FastPspMemoryUnsafe()
		{
			AllocMemoryOnce();
		}

		~FastPspMemoryUnsafe()
		{
			Dispose();
			//FreeMemory();
		}

		private static bool AlreadyInitialized = false;

		private void AllocMemoryOnce()
		{
			if (!AlreadyInitialized)
			{
				AlreadyInitialized = true;

				//Logger.Info("FastPspMemory.AllocMemory");

				ulong[] TryBases;
				if (Platform.Is32Bit)
				{
					if (Platform.OS == OS.Windows)
					{
						TryBases = new ulong[] { 0x31000000, 0x40000000, 0x50000000 };
					}
					else
					{
						//Logger.Error("Using mmap on linux x86 is known to cause problems");
						TryBases = new ulong[] { 0xE1000000, 0x31008008, 0x40008000, 0x50008000, 0x31000000, 0x40000000, 0x50000000 };
					}
				}
				else
				{
					if (Platform.OS == OS.Windows)
					{
						TryBases = new ulong[] { 0xE7000000, 0xE1000000, 0x0012340080000000, 0x00123400A0000000 };
					}
					else
					{
						TryBases = new ulong[] { 0x2300000000, 0x31000000, 0x40000000, 0x50000000, 0xE1000000 };
					}
				}

				uint ScratchPadAllocSize = ScratchPadSize * 0x10;
				uint FrameBufferAllocSize = FrameBufferSize;
				uint MainAllocSize = MainSize;

				foreach (var TryBase in TryBases)
				{
					_Base = (byte*)TryBase;
					Console.WriteLine("FastPspMemory.AllocMemoryOnce: Trying Base ... 0x{0:X}", TryBase);

					StaticNullPtr = _Base;
					Platform.AllocRangeGuard(_Base, _Base + ScratchPadOffset);
					StaticScratchPadPtr = (byte*)Platform.AllocRange(_Base + ScratchPadOffset, ScratchPadAllocSize);
					Platform.AllocRangeGuard(_Base + ScratchPadOffset + ScratchPadAllocSize, _Base + FrameBufferOffset);
					StaticFrameBufferPtr = (byte*)Platform.AllocRange(_Base + FrameBufferOffset, FrameBufferAllocSize);
					Platform.AllocRangeGuard(_Base + FrameBufferOffset + FrameBufferAllocSize, _Base + MainOffset);
					StaticMainPtr = (byte*)Platform.AllocRange(_Base + MainOffset, MainAllocSize);

					if (StaticScratchPadPtr != null && StaticFrameBufferPtr != null && StaticMainPtr != null)
					{
						Console.WriteLine("FastPspMemory.AllocMemoryOnce: Found Suitable Base ... 0x{0:X}", TryBase);
						break;
					}
					else
					{
						if (StaticScratchPadPtr != null) Platform.Free(StaticScratchPadPtr, ScratchPadAllocSize);
						if (StaticFrameBufferPtr != null) Platform.Free(StaticFrameBufferPtr, FrameBufferAllocSize);
						if (StaticMainPtr != null) Platform.Free(StaticMainPtr, MainAllocSize);
					}
				}

				if (_Base == null || StaticScratchPadPtr == null || StaticFrameBufferPtr == null || StaticMainPtr == null)
				{
					//Logger.Fatal("Can't allocate virtual memory!");
					Debug.Fail("Can't allocate virtual memory!");
					throw (new InvalidOperationException("Can't allocate virtual memory!"));
				}
			}

			NullPtr = StaticNullPtr;
			ScratchPadPtr = StaticScratchPadPtr;
			FrameBufferPtr = StaticFrameBufferPtr;
			MainPtr = StaticMainPtr;
		}

		/*
		private void AllocMemory()
		{
			Logger.Info("FastPspMemory.AllocMemory");
			ScratchPadPtr = VirtualAlloc(Base + ScratchPadOffset, ScratchPadSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
			FrameBufferPtr = VirtualAlloc(Base + FrameBufferOffset, FrameBufferSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
			MainPtr = VirtualAlloc(Base + MainOffset, MainSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
			if (ScratchPadPtr == null || FrameBufferPtr == null || MainPtr == null)
			{
				Logger.Fatal("Can't allocate virtual memory!");
				throw (new InvalidOperationException());
			}
		}

		private void FreeMemory()
		{
			Logger.Info("FastPspMemory.FreeMemory");
			if (ScratchPadPtr != null)
			{
				if (!VirtualFree(ScratchPadPtr, ScratchPadSize, MEM_DECOMMIT | MEM_RELEASE))
				{
				}
				if (!VirtualFree(FrameBufferPtr, FrameBufferSize, MEM_DECOMMIT | MEM_RELEASE))
				{
				}
				if (!VirtualFree(MainPtr, MainSize, MEM_DECOMMIT | MEM_RELEASE))
				{
				}
				ScratchPadPtr = null;
				FrameBufferPtr = null;
				MainPtr = null;
			}
		}
		*/

		public override uint PointerToPspAddressUnsafe(void* Pointer)
		{
			if (Pointer == null) return 0;
			return (uint)((byte*)Pointer - _Base);
		}

		public override void* PspAddressToPointerUnsafe(uint _Address)
		{
			var Address = (_Address & FastPspMemory.FastMemoryMask);
			//Console.WriteLine("Base: 0x{0:X} ; Address: 0x{1:X}", (ulong)Base, Address);
			if (Address == 0) return null;
#if false
			if (_Base == null) throw(new InvalidProgramException("Base is null"));
#endif
			return _Base + Address;
		}

		public override void Dispose()
		{
		}
	}
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpUtils.Threading;
using CSharpUtils.Extensions;
using CSPspEmu.Core.Cpu;
using CSPspEmu.Core.Memory;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CSPspEmu.Core;
using CSharpUtils;
using System.Globalization;
using System.Threading;

namespace CSPspEmu.Hle
{
	public delegate void WakeUpCallbackDelegate();

	public enum PspThreadAttributes : uint
	{
		/// <summary>
		/// 
		/// </summary>
		None = 0,

		/// <summary>
		/// Enable VFPU access for the thread.
		/// </summary>
		Vfpu = 0x00004000,

		/// <summary>
		/// Start the thread in user mode (done automatically if the thread creating it is in user mode).
		/// </summary>
		User = 0x80000000,

		/// <summary>
		/// Thread is part of the USB/WLAN API.
		/// </summary>
		UsbWlan = 0xa0000000,
		
		/// <summary>
		/// Thread is part of the VSH API.
		/// </summary>
		Vsh = 0xc0000000,

		/// <summary>
		/// Allow using scratchpad memory for a thread, NOT USABLE ON V1.0
		/// </summary>
		ScratchRamEnable = 0x00008000,
		
		/// <summary>
		/// Disables filling the stack with 0xFF on creation
		/// </summary>
		NoFillStack = 0x00100000,
		
		/// <summary>
		/// Clear the stack when the thread is deleted
		/// </summary>
		ClearStack = 0x00200000,
	}

	unsafe public class HleThread
	{
		protected MethodCacheFast MethodCache;

		/// <summary>
		/// Value used to schedule threads.
		/// </summary>
		public int PriorityValue;

		//public int Priority = 1;
		protected GreenThread GreenThread;
		public CpuThreadState CpuThreadState { get; protected set; }
		protected int MinimalInstructionCountForYield = 1000000;
		public int Id;
		//public String Name;
		public Status CurrentStatus;
		public WaitType CurrentWaitType;
		public DateTime AwakeOnTime;
		public MemoryPartition Stack;
		public String WaitDescription;
		//public int InitPriority;
		public PspThreadAttributes Attribute;
		public SceKernelThreadInfo Info;
		public bool HandleCallbacks;

		public uint GP
		{
			get { return Info.GP; }
			set { Info.GP = value; CpuThreadState.GP = value;  }
		}

		public String Name
		{
			get
			{
				fixed (byte* NamePtr = Info.Name) return PointerUtils.PtrToString(NamePtr, Encoding.ASCII);
			}
			set
			{
				fixed (byte* NamePtr = Info.Name) PointerUtils.StoreStringOnPtr(value, Encoding.ASCII, NamePtr);
			}
		}

		public enum WaitType
		{
			None,
			Timer,
			GraphicEngine,
			Audio,
			Display,
			Semaphore,
		}

		public enum Status {
			Running = 1,
			Ready = 2,
			Waiting = 4,
			Suspend = 8,
			Stopped = 16,
			Killed = 32,
		}

		public HleThread(CpuThreadState CpuThreadState)
		{
			this.MethodCache = CpuThreadState.CpuProcessor.MethodCache;
			this.GreenThread = new GreenThread();
			this.CpuThreadState = CpuThreadState;
			this.PrepareThread();
		}

		protected void PrepareThread()
		{
			GreenThread.InitAndStartStopped(MainLoop);
		}

		[HandleProcessCorruptedStateExceptions()]
		protected void MainLoop()
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo(PspConfig.CultureName);
			try
			{
				while (true)
				{
					//Debug.WriteLine("Thread({0:X}) : PC: {1:X}", this.Id, CpuThreadState.PC);
					//Console.WriteLine("PC:{0:X}", CpuThreadState.PC);
					GetDelegateAt(CpuThreadState.PC & PspMemory.MemoryMask)(CpuThreadState);
				}
			}
			catch (AccessViolationException AccessViolationException)
			{
				var Field = typeof(AccessViolationException).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(FieldInfo => FieldInfo.Name == "_target").Single();
				uint Address = (uint)((IntPtr)Field.GetValue(AccessViolationException)).ToInt32();
				throw (new PspMemory.InvalidAddressException(Address));
				//AccessViolationException.
			}
		}

		// 8903E08

		public Action<CpuThreadState> GetDelegateAt(uint PC)
		{
			//var MethodCache = CpuThreadState.CpuProcessor.MethodCache;

			var Delegate = MethodCache.TryGetMethodAt(PC);
			if (Delegate == null)
			{
				MethodCache.SetMethodAt(
					PC,
					Delegate = CpuThreadState.CpuProcessor.CreateDelegateForPC(new PspMemoryStream(CpuThreadState.CpuProcessor.Memory), PC)
				);
			}

			return Delegate;
		}

		public void Step(int InstructionCountForYield = 1000000)
		{
			CpuThreadState.StepInstructionCount = InstructionCountForYield;
			//this.MinimalInstructionCountForYield = InstructionCountForYield;
			GreenThread.SwitchTo();
		}

		public void WakeUp()
		{
			if (this.CurrentStatus != Status.Waiting)
			{
				if (this.CurrentStatus != Status.Ready)
				{
					if (this.CurrentStatus != Status.Running)
					{
						throw (new InvalidOperationException("Trying to awake a non waiting thread '" + this.CurrentStatus + "'"));
					}
				}
			}
			this.CurrentStatus = Status.Ready;
		}

		public void SetWaitAndPrepareWakeUp(WaitType WaitType, String WaitDescription, Action<WakeUpCallbackDelegate> PrepareCallback, bool HandleCallbacks = false)
		{
			SetWait0(WaitType, WaitDescription, HandleCallbacks);
			{
				PrepareCallback(WakeUp);
			}
			SetWait1();
		}

		protected void SetWait0(WaitType WaitType, String WaitDescription, bool HandleCallbacks)
		{
			this.CurrentStatus = Status.Waiting;
			this.CurrentWaitType = WaitType;
			this.WaitDescription = WaitDescription;
			this.HandleCallbacks = HandleCallbacks;
		}

		protected void SetWait1()
		{
			if (this.CurrentStatus == Status.Waiting)
			{
				CpuThreadState.Yield();
			}
		}

		/*
		public void SetWait(WaitType WaitType, String WaitDescription, bool HandleCallbacks)
		{
			SetWait0(WaitType, WaitDescription, HandleCallbacks);
			SetWait1();
		}
		*/

		public override string ToString()
		{
			return String.Format("HleThread(Id={0}, Name='{1}')", Id, Name);
		}
	}

	public struct SceKernelSysClock
	{
		//ulong Value;
		public uint Low;
		public uint High;
	}

	/// <summary>
	/// Event flag wait types
	/// </summary>
	public enum PspEventFlagWaitTypes : uint
	{
		/// <summary>
		/// Wait for all bits in the pattern to be set 
		/// </summary>
		PSP_EVENT_WAITAND = 0x00,

		/// <summary>
		/// Wait for one or more bits in the pattern to be set
		/// </summary>
		PSP_EVENT_WAITOR = 0x01,

		/// <summary>
		/// Clear all the wait pattern when it matches
		/// </summary>
		PSP_EVENT_WAITCLEARALL = 0x10,

		/// <summary>
		/// Clear the wait pattern when it matches
		/// </summary>
		PSP_EVENT_WAITCLEAR = 0x20,
	};

	public enum PspThreadStatus : uint
	{
		PSP_THREAD_RUNNING = 1,
		PSP_THREAD_READY = 2,
		PSP_THREAD_WAITING = 4,
		PSP_THREAD_SUSPEND = 8,
		PSP_THREAD_STOPPED = 16, // Before startThread
		PSP_THREAD_KILLED = 32, // Thread manager has killed the thread (stack overflow)
	}

	//alias int function(SceSize args, void* argp) SceKernelThreadEntry;
	public enum SceKernelThreadEntry : uint
	{
	}

	unsafe public struct SceKernelThreadInfo
	{
		/// <summary>
		/// Size of the structure
		/// </summary>
		public int Size;

		/// <summary>
		/// Null terminated name of the thread
		/// </summary>
		public fixed byte Name[32];

		/// <summary>
		/// Thread attributes
		/// </summary>
		public uint Attributes;

		/// <summary>
		/// Thread status
		/// </summary>
		public PspThreadStatus Status;

		/// <summary>
		/// Thread entry point
		/// </summary>
		public SceKernelThreadEntry EntryPoint;

		/// <summary>
		/// Thread stack pointer
		/// </summary>
		public uint StackPointer;

		/// <summary>
		/// Thread stack size
		/// </summary>
		public int StackSize;

		/// <summary>
		/// Pointer to the gp
		/// </summary>
		public uint GP;

		/// <summary>
		/// Initial Priority
		/// </summary>
		public int PriorityInitially;

		/// <summary>
		/// Current Priority
		/// </summary>
		public int PriorityCurrent;

		/// <summary>
		/// Wait Type
		/// </summary>
		public PspEventFlagWaitTypes WaitType;

		/// <summary>
		/// Wait id
		/// </summary>
		public int WaitId;

		/// <summary>
		/// Wakeup count
		/// </summary>
		public int WakeupCount;

		/// <summary>
		/// Exit status of the thread
		/// </summary>
		public int ExitStatus;

		/// <summary>
		/// Number of clock cycles run
		/// </summary>
		public SceKernelSysClock RunClocks;

		/// <summary>
		/// Interrupt preemption count
		/// </summary>
		public int InterruptPreemptionCount;

		/// <summary>
		/// Thread preemption count
		/// </summary>
		public int ThreadPreemptionCount;

		/// <summary>
		/// Release count
		/// </summary>
		public int ReleaseCount;
	}

	public struct SceKernelThreadOptParam
	{
	}
}

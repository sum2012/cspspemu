﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CSPspEmu.Core.Gpu.State;
using CSPspEmu.Core.Threading.Synchronization;
using CSPspEmu.Core.Memory;
using CSharpUtils;
using CSPspEmu.Hle;

namespace CSPspEmu.Core.Gpu
{
	unsafe public class GpuProcessor : PspEmulatorComponent
	{
		/*
		 *   - GU_SYNC_FINISH - 0 - Wait until the last sceGuFinish command is reached
		 *   - GU_SYNC_SIGNAL - 1 - Wait until the last (?) signal is executed
		 *   - GU_SYNC_DONE   - 2 - Wait until all commands currently in list are executed
		 *   - GU_SYNC_LIST   - 3 - Wait for the currently executed display list (GU_DIRECT)
		 *   - GU_SYNC_SEND   - 4 - Wait for the last send list
		 *   
		 *   int sceGuSync(int mode, SyncTypeEnum what)
		 *	 {
		 *		 switch (mode)
		 *		 {
		 *			 case GU_SYNC_FINISH: return sceGeDrawSync(what);
		 *			 case GU_SYNC_LIST  : return sceGeListSync(ge_list_executed[0], what);
		 *			 case GU_SYNC_SEND  : return sceGeListSync(ge_list_executed[1], what);
		 *		 	 default: case GU_SYNC_SIGNAL: case GU_SYNC_DONE: return 0;
		 *	 	 }
		 *	 }
		 */
		/// <summary>
		/// 
		/// </summary>
		public enum SyncTypeEnum : uint
		{
			/// <summary>
			/// 
			/// </summary>
			ListDone = 0,

			/// <summary>
			/// 
			/// </summary>
			ListQueued = 1,

			/// <summary>
			/// 
			/// </summary>
			ListDrawingDone = 2,

			/// <summary>
			/// 
			/// </summary>
			ListStallReached = 3,

			/// <summary>
			/// 
			/// </summary>
			ListCancelDone = 4,
		}

		/// <summary>
		/// 
		/// </summary>
		public PspConfig PspConfig;

		/// <summary>
		/// 
		/// </summary>
		[Inject]
		public PspMemory Memory;

		[Inject]
		public HleInterop HleInterop;

		//HleInterop

		/// <summary>
		/// 
		/// </summary>
		volatile public LinkedList<GpuDisplayList> DisplayListQueue;

		/// <summary>
		/// 
		/// </summary>
		volatile protected AutoResetEvent DisplayListQueueUpdated = new AutoResetEvent(false);

		/// <summary>
		/// 
		/// </summary>
		volatile protected Queue<GpuDisplayList> DisplayListFreeQueue;

		/// <summary>
		/// All the supported Psp Display Lists (Available and not available).
		/// </summary>
		readonly public GpuDisplayList[] DisplayLists = new GpuDisplayList[64];

		public enum StatusEnum
		{
			Drawing = 0,
			Completed = 1,
		}

		/// <summary>
		/// 
		/// </summary>
		//public event Action DrawSync;
		readonly public WaitableStateMachine<StatusEnum> Status = new WaitableStateMachine<StatusEnum>(Debug: false);
		//readonly public WaitableStateMachine<StatusEnum> Status = new WaitableStateMachine<StatusEnum>(Debug: true);

		/// <summary>
		/// 
		/// </summary>
		[Inject]
		public GpuImpl GpuImpl;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="PspConfig"></param>
		/// <param name="Memory"></param>
		public override void InitializeComponent()
		{
			if (sizeof(GpuStateStruct) > sizeof(uint) * 512)
			{
				ConsoleUtils.SaveRestoreConsoleColor(ConsoleColor.Red, () =>
				{
					Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
					Console.WriteLine("GpuStateStruct too big. Maybe x64? . Size: " + sizeof(GpuStateStruct));
					Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
				});
				
			}

			this.PspConfig = PspEmulatorContext.PspConfig;
			this.DisplayListQueue = new LinkedList<GpuDisplayList>();
			this.DisplayListFreeQueue = new Queue<GpuDisplayList>();
			for (int n = 0; n < DisplayLists.Length; n++)
			{
				var DisplayList = new GpuDisplayList(Memory, this, n);
				this.DisplayLists[n] = DisplayList;
				//this.DisplayListFreeQueue.Enqueue(DisplayLists[n]);
				EnqueueFreeDisplayList(DisplayLists[n]);
			}
		}

		protected void AddedDisplayList()
		{
			GpuImpl.AddedDisplayList();
		}

		AutoResetEvent DisplayListFreeEvent = new AutoResetEvent(false);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public GpuDisplayList DequeueFreeDisplayList()
		{
			lock (DisplayListFreeQueue)
			{
				var DisplayList = this.DisplayListFreeQueue.Dequeue();
				DisplayList.Available = false;
				return DisplayList;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="GpuDisplayList"></param>
		public void EnqueueFreeDisplayList(GpuDisplayList GpuDisplayList)
		{
			//Console.WriteLine("EnqueueFreeDisplayList: {0}", this.DisplayListFreeQueue.Count);
			AddedDisplayList();
			lock (DisplayListFreeQueue)
			{
				this.DisplayListFreeQueue.Enqueue(GpuDisplayList);
				GpuDisplayList.Freed();
			}
			DisplayListFreeEvent.Set();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="DisplayList"></param>
		public void EnqueueDisplayListFirst(GpuDisplayList DisplayList)
		{
			//Console.WriteLine("EnqueueDisplayListFirst: {0}", this.DisplayListFreeQueue.Count);
			AddedDisplayList();
			lock (DisplayListQueue)
			{
				DisplayListQueue.AddFirst(DisplayList);
			}
			DisplayListQueueUpdated.Set();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="DisplayList"></param>
		public void EnqueueDisplayListLast(GpuDisplayList DisplayList)
		{
			//Console.WriteLine("EnqueueDisplayListLast: {0}", this.DisplayListFreeQueue.Count);
			AddedDisplayList();
			lock (DisplayListQueue)
			{
				DisplayListQueue.AddLast(DisplayList);
			}
			DisplayListQueueUpdated.Set();
		}


		public void ProcessInit()
		{
			GpuDisplayList.InstructionSwitch = GpuDisplayList.GenerateSwitch();
		}

		/// <summary>
		/// 
		/// </summary>
		public void ProcessStep()
		{
			GpuDisplayList CurrentGpuDisplayList;
			Status.SetValue(StatusEnum.Completed);

			//Thread.Sleep(1);
			if (PspConfig.VerticalSynchronization)
			{
				DisplayListQueueUpdated.WaitOne(1);
			}
			else
			{
				DisplayListQueueUpdated.WaitOne(0);
			}

			while (true)
			{
				lock (DisplayListQueue) if (DisplayListQueue.Count <= 0) break;

				Status.SetValue(StatusEnum.Drawing);

				lock (DisplayListQueue) CurrentGpuDisplayList = DisplayListQueue.RemoveFirstAndGet();
				{
					CurrentGpuDisplayList.Process();
				}
				EnqueueFreeDisplayList(CurrentGpuDisplayList);
			}

			Status.SetValue(StatusEnum.Completed);

			//if (DrawSync != null) DrawSync();
		}

		public void GeDrawSync(SyncTypeEnum SyncType, Action SyncCallback)
		{
			//Console.Error.WriteLine("-- GeDrawSync --------------------------------");
			//Console.WriteLine("GeDrawSync: {0}", this.DisplayListFreeQueue.Count);
			if (SyncType != SyncTypeEnum.ListDone) throw new NotImplementedException();
			Status.CallbackOnStateOnce(StatusEnum.Completed, () =>
			{
				//Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
				CapturingWaypoint();
				SyncCallback();
			});
		}

		private void CapturingWaypoint()
		{
			if (CapturingFrame)
			{
				CapturingFrame = false;
				Console.WriteLine("EndCapturingFrame!");
				GpuImpl.EndCapture();
			}

			if (StartCapturingFrame)
			{
				StartCapturingFrame = false;
				CapturingFrame = true;
				GpuImpl.StartCapture();
				Console.WriteLine("StartCapturingFrame!");
			}
		}

		internal void MarkDepthBufferLoad()
		{
			//throw new NotImplementedException();
		}

		public void SetCurrent()
		{
			GpuImpl.SetCurrent();
		}
		public void UnsetCurrent()
		{
			GpuImpl.UnsetCurrent();
		}

		bool StartCapturingFrame = false;
		bool CapturingFrame = false;

		public void CaptureFrame()
		{
			StartCapturingFrame = true;
			Console.WriteLine("Waiting StartCapturingFrame!");
		}
	}

	public enum TextureLevelMode { Auto = 0, Const = 1, Slope = 2 }
}

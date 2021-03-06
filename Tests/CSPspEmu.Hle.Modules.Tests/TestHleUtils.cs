﻿using CSPspEmu.Core.Audio;
using CSPspEmu.Core.Cpu;
using CSPspEmu.Core.Gpu;
using CSPspEmu.Core.Memory;
using CSPspEmu.Hle.Managers;
using CSPspEmu.Hle.Modules.interruptman;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[InjectMap(typeof(PspMemory), typeof(LazyPspMemory))]
[InjectMap(typeof(GpuImpl), typeof(GpuImplNull))]
[InjectMap(typeof(PspAudioImpl), typeof(AudioImplNull))]
[InjectMap(typeof(ICpuConnector), typeof(CpuConnector))]
[InjectMap(typeof(IInterruptManager), typeof(HleInterruptManager))]
public class TestHleUtils
{
	class CpuConnector : ICpuConnector
	{
		public void Yield(CpuThreadState CpuThreadState)
		{
		}
	}
	static public InjectContext CreateInjectContext(object Bootstrap)
	{
		var _InjectContext = InjectContext.Bootstrap(new TestHleUtils());
		_InjectContext.InjectDependencesTo(Bootstrap);
		return _InjectContext;
	}
}

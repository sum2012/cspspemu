﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSPspEmu.Hle.Attributes;
using CSPspEmu.Hle.Modules.modulemgr;

namespace CSPspEmu.Hle.Modules.loadcore
{
	[HlePspModule(ModuleFlags = ModuleFlags.KernelMode | ModuleFlags.Flags0x00010011)]
	unsafe public class LoadCoreForKernel : HleModuleHost
	{
		//public enum SceModule : uint { }
		public enum SceUID : int { }

		/// <summary>
		/// 
		/// </summary>
		[HlePspFunction(NID = 0xACE23476, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		[HlePspUnknownDefinitionAttribute]
		public void sceKernelCheckPspConfig()
		{
			throw(new NotImplementedException());
		}

		/// <summary>
		/// 
		/// </summary>
		[HlePspFunction(NID = 0xBF983EF2, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		[HlePspUnknownDefinitionAttribute]
		public void sceKernelProbeExecutableObject()
		{
			throw(new NotImplementedException());
		}

		/// <summary>
		/// Find a module by it's UID.
		/// </summary>
		/// <param name="ModuleId">The UID of the module.</param>
		/// <returns>Pointer to the ::SceModule structure if found, otherwise NULL.</returns>
		[HlePspFunction(NID = 0xCCE4A157, FirmwareVersion = 150)]
		//[HlePspNotImplemented]
		public uint sceKernelFindModuleByUID(int ModuleId)
		{
			var ModuleMgrForUser = HleState.ModuleManager.GetModule<ModuleMgrForUser>();
			var Module = ModuleMgrForUser.Modules.Get(ModuleId);
			return Module.SceModuleStructPartition.Low;
		}

		/// <summary>
		/// Invalidate the CPU's instruction cache.
		/// </summary>
		[HlePspFunction(NID = 0xD8779AC6, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public void sceKernelIcacheClearAll()
		{
			HleState.CpuProcessor.sceKernelIcacheInvalidateAll();
			//unimplemented();
		}

		/// <summary>
		/// Find a module by it's name.
		/// </summary>
		/// <param name="ModuleName">The name of the module.</param>
		/// <returns>Pointer to the ::SceModule structure if found, otherwise NULL.</returns>
		[HlePspFunction(NID = 0xCF8A41B1, FirmwareVersion = 150)]
		[HlePspNotImplemented]
		public int sceKernelFindModuleByName(string ModuleName)
		{
			Console.WriteLine("sceKernelFindModuleByName('{0}') not implemented", ModuleName);
			return 0;
			/*
			logWarning();
			//unimplemented();
			return null;
			*/
			//throw(new NotImplementedException());
		}

	}
}

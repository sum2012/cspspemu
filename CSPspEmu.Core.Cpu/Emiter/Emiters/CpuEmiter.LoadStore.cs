﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;

namespace CSPspEmu.Core.Cpu.Emiter
{
	unsafe sealed public partial class CpuEmiter
	{
		private void _load_i(Action Action)
		{
			if (!(MipsMethodEmiter.Processor.Memory is FastPspMemory))
			{
				MipsMethodEmiter.SavePC(PC);
			}

			MipsMethodEmiter.SaveGPR(RT, () =>
			{
				MipsMethodEmiter._getmemptr(() =>
				{
					MipsMethodEmiter.LoadGPR_Unsigned(RS);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, IMM);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Add);
				});
				Action();
			});
		}

		private void _save_common(Action Action)
		{
			MipsMethodEmiter._getmemptr(() =>
			{
				MipsMethodEmiter.LoadGPR_Unsigned(RS);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, IMM);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Add);
			});
			Action();
		}

		private void _save_i(OpCode OpCode)
		{
			_save_common(() =>
			{
				MipsMethodEmiter.LoadGPR_Unsigned(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCode);
			});
		}

		// Load Byte/Half word/Word (Left/Right/Unsigned).
		public void lb() { _load_i(() => { MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_I1); }); }
		public void lh() { _load_i(() => { MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_I2); }); }
		public void lw() { _load_i(() => { MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_I4); }); }
		/*
		 * registers[instruction.RT] = (
		 *     (registers[instruction.RT] & 0x_0000_FFFF) |
		 *     ((memory.tread!(ushort)(registers[instruction.RS] + instruction.IMM - 1) << 16) & 0x_FFFF_0000)
		 * );
		 */
		public void lwl()
		{
			MipsMethodEmiter.SaveGPR(RT, () =>
			{
				// registers[instruction.RT] & 0x_0000_FFFF
				MipsMethodEmiter.LoadGPR_Unsigned(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, 0x0000FFFF);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.And);

				// ((memory.tread!(ushort)(registers[instruction.RS] + instruction.IMM - 1) << 16) & 0x_FFFF_0000)
				MipsMethodEmiter._getmemptr(() =>
				{
					MipsMethodEmiter.LoadGPR_Unsigned(RS);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, IMM - 1);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Add);
				});
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_I2);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, 16);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Shl);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, 0xFFFF0000);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.And);

				// OR
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Or);
			});
		}
		/*
		 * registers[instruction.RT] = (
		 *     (registers[instruction.RT] & 0x_FFFF_0000) |
		 *     ((memory.tread!(ushort)(registers[instruction.RS] + instruction.IMM - 0) << 0) & 0x_0000_FFFF)
		 * );
		 */
		public void lwr()
		{
			MipsMethodEmiter.SaveGPR(RT, () =>
			{
				// registers[instruction.RT] & 0x_FFFF_0000
				MipsMethodEmiter.LoadGPR_Unsigned(RT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, 0xFFFF0000);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.And);

				// ((memory.tread!(ushort)(registers[instruction.RS] + instruction.IMM - 0) << 0) & 0x_0000_FFFF)
				MipsMethodEmiter._getmemptr(() =>
				{
					MipsMethodEmiter.LoadGPR_Unsigned(RS);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, IMM - 0);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Add);
				});
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_I2);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4_0);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Shl);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, 0x0000FFFF);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.And);

				// OR
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Or);
			});	
		}
		public void lbu() { _load_i(() => { MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_U1); }); }
		public void lhu() { _load_i(() => { MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_U2); }); }

		// Store Byte/Half word/Word (Left/Right).
		public void sb() { _save_i(OpCodes.Stind_I1); }
		public void sh() { _save_i(OpCodes.Stind_I2); }
		public void sw() { _save_i(OpCodes.Stind_I4); }
		public void swl() { throw (new NotImplementedException()); }
		public void swr() { throw (new NotImplementedException()); }

		// Load Linked word.
		// Store Conditional word.
		public void ll() { throw (new NotImplementedException()); }
		public void sc() { throw (new NotImplementedException()); }

		// Load Word to Cop1 floating point.
		// Store Word from Cop1 floating point.
		public void lwc1()
		{
			MipsMethodEmiter.SaveFPR(FT, () =>
			{
				MipsMethodEmiter._getmemptr(() =>
				{
					MipsMethodEmiter.LoadGPR_Unsigned(RS);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldc_I4, IMM);
					MipsMethodEmiter.ILGenerator.Emit(OpCodes.Add);
				});
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Ldind_R4);
			});
		}
		public void swc1() {
			_save_common(() =>
			{
				MipsMethodEmiter.LoadFPR(FT);
				MipsMethodEmiter.ILGenerator.Emit(OpCodes.Stind_R4); 
			});
		}
	}
}
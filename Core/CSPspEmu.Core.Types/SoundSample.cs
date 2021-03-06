﻿using System;
using System.Runtime;
using System.Runtime.CompilerServices;
namespace CSPspEmu.Core.Audio
{
	public struct MonoShortSoundSample
	{
		public short Value;  // Left/Right audio

		public MonoShortSoundSample(short Value)
		{
			this.Value = Value;
		}

		public static MonoShortSoundSample Mix(MonoShortSoundSample A, MonoShortSoundSample B)
		{
			return new MonoShortSoundSample((short)((A.Value + B.Value) / 2));
		}

		public short MonoLeftRight
		{
			set
			{
				Value = value;
			}
		}
	}

	public struct StereoShortSoundSample
	{
		public short Left;  // Left audio
		public short Right; // Right audio

		public short GetByIndex(int Index)
		{
			return (Index == 0) ? Left : Right;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public StereoShortSoundSample(int Left, int Right)
		{
			this.Left = Clamp(Left);
			this.Right = Clamp(Right);
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public StereoShortSoundSample(int LeftRight)
		{
			this.Left = Clamp(LeftRight);
			this.Right = Clamp(LeftRight);
		}

		public int MaxAmplitudeLeftRight
		{
			//[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
			get
			{
				return Math.Min(short.MaxValue, Math.Max(Math.Abs((int)Left), Math.Abs((int)Right)));
			}
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static StereoShortSoundSample Mix(StereoShortSoundSample A, StereoShortSoundSample B)
		{
			return new StereoShortSoundSample(Clamp((A.Left + B.Left) / 2), Clamp((A.Right + B.Right) / 2));
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static implicit operator StereoIntSoundSample(StereoShortSoundSample StereoShortSoundSample)
		{
			return new StereoIntSoundSample((int)StereoShortSoundSample.Left, (int)StereoShortSoundSample.Right);
		}

		public short MonoLeftRight
		{
			set
			{
				Left = value;
				Right = value;
			}
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		static public short Clamp(int Value)
		{
			if (Value < short.MinValue) return short.MinValue;
			if (Value > short.MaxValue) return short.MaxValue;
			return (short)Value;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public StereoShortSoundSample ApplyVolumes(int LeftVolume, int RightVolume)
		{
			return new StereoShortSoundSample(
				Clamp(this.Left * LeftVolume / 0x1000),
				Clamp(this.Right * RightVolume / 0x1000)
			);
		}
	}

	public struct StereoIntSoundSample
	{
		public int Left;  // Left audio
		public int Right; // Right audio

		public StereoIntSoundSample(int Left, int Right)
		{
			this.Left = Left;
			this.Right = Right;
		}

		public int MaxAmplitudeLeftRight
		{
			get
			{
				return Math.Max(Math.Abs(Left), Math.Abs(Right));
			}
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static StereoIntSoundSample Mix(StereoIntSoundSample A, StereoIntSoundSample B)
		{
			return new StereoIntSoundSample((int)((A.Left + B.Left) / 2), (int)((A.Right + B.Right) / 2));
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static StereoIntSoundSample operator +(StereoIntSoundSample A, StereoIntSoundSample B)
		{
			return new StereoIntSoundSample((A.Left + B.Left), (A.Right + B.Right));
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static StereoIntSoundSample operator /(StereoIntSoundSample A, int Div)
		{
			return new StereoIntSoundSample((A.Left / Div), (A.Right / Div));
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static StereoIntSoundSample operator *(StereoIntSoundSample A, int Mult)
		{
			return new StereoIntSoundSample((A.Left * Mult), (A.Right * Mult));
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
		public static implicit operator StereoShortSoundSample(StereoIntSoundSample StereoIntSoundSample)
		{
			return new StereoShortSoundSample(StereoIntSoundSample.Left, StereoIntSoundSample.Right);
		}

		public int MonoLeftRight
		{
			//[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
			set
			{
				Left = value;
				Right = value;
			}
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Villermen.RuneScapeCacheTools.Cache
{
	public class Vector3
	{
		public int Level { get; }
		public int Y { get; }
		public int X { get; }
		public int Z { get; }

		private static int UnknownInteger;

		private static Vector3[] UnknownVector3Array = new Vector3[0];

		public Vector3()
		{
			Level = -1;
		}

		private Vector3(Vector3 vector)
		{
			Level = vector.Level;
			X = vector.X;
			Y = vector.Y;
			Z = vector.Z;
		}

		private Vector3(int unknownInteger, bool unknownBoolean)
		{
			if (unknownInteger == -1)
			{
				Level = -1;
			}
			else
			{
				Level = unknownInteger >> 28 & 3;
				X = (unknownInteger >> 14 & 0x3fff) << 9;
				Y = 0;
				Z = (unknownInteger & 0x3fff) << 9;

				if (unknownBoolean)
				{
					X += 256;
					Z += 256;
				}
			}
		}

		public Vector3(int level, int x, int y, int z)
		{
			Level = level;
			X = x;
			Y = y;
			Z = z;
		}

		// TODO: implement other methods

	}
}
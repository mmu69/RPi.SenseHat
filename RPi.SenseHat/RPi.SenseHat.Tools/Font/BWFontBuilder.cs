﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Emmellsoft.IoT.Rpi.SenseHat.Fonts;

namespace Emmellsoft.IoT.Rpi.SenseHat.Tools.Font
{
	public static class BwFontBuilder
	{
		public static BwFont GetBwFont(Bitmap bitmap, string chars)
		{
			if (bitmap.Height != 9)
			{
				throw new ArgumentException("The bitmap should be 8 pixels high plus one control-pixel at the top (i.e. 9 pixels).");
			}

			int x = 0;
			int charIndex = -1;

			var charList = new List<BwCharacter>();
			var charColumns = new List<byte>();

			do
			{
				byte controlPixelAlpha = bitmap.GetPixel(x, 0).A;
				if (controlPixelAlpha > 128)
				{
					// Next character starts!

					if (charIndex > -1)
					{
						// Handle the previous one!
						BwCharacter c = new BwCharacter(chars[charIndex], charColumns.ToArray());
						charList.Add(c);
                    }

					charColumns.Clear();

					charIndex++;
					if (charIndex > chars.Length)
					{
						throw new ArgumentException("The string of chars has less chars than the bitmap!");
					}
				}

				int fontColumnByte = 0;
				for (int y = 8; y >= 1; y--)
				{
					byte fontPixelAlpha = bitmap.GetPixel(x, y).A;

					if (fontPixelAlpha > 128)
					{
						fontColumnByte++;
					}

					if (y > 1)
					{
						fontColumnByte <<= 1;
					}
				}

				charColumns.Add((byte)fontColumnByte);

				x++;
			}
			while (x < bitmap.Width);

			if (charIndex < chars.Length - 1)
			{
				throw new ArgumentException("More characters in the string than in the bitmap!");
			}

			if (charIndex > -1)
			{
				BwCharacter c = new BwCharacter(chars[charIndex], charColumns.ToArray());
				charList.Add(c);
			}

			return new BwFont(charList);
		}

		public static Tuple<string, Bitmap> GetFontBitmap(IEnumerable<byte> fontBytes)
		{
			byte[] bytesArray = fontBytes.ToArray();

			var width = MessureBitmapWidth(bytesArray);
			if (width == 0)
			{
				throw new ArgumentException("Zero width image!");
			}

			Bitmap bitmap = new Bitmap(width, 9);
			var chars = new StringBuilder();

			for (int i = 0; i < width; i++)
			{
				bitmap.SetPixel(i, 0, Color.Transparent);
			}

			int x = 0;

			int index = 0;
			bool isBeginningOfChar = true;
			bool isEscaped = false;

			while (index < bytesArray.Length)
			{
				byte b = bytesArray[index];
				if (isBeginningOfChar)
				{
					// Needs 2 bytes for Unicode
					if (index >= bytesArray.Length - 2)
					{
						throw new ArgumentException("Beginning of char at the end!");
					}

					char c = Encoding.Unicode.GetString(bytesArray, index, 2).First();

					chars.Append(c);

					index += 1;
					isBeginningOfChar = false;

					bitmap.SetPixel(x, 0, Color.Green);
				}
				else if ((b == 0xFF) && !isEscaped)
				{
					if (index == bytesArray.Length - 1)
					{
						throw new ArgumentException("Escape byte at the end!");
					}

					if (bytesArray[index + 1] == 0x00)
					{
						isBeginningOfChar = true;
						index++;
					}
					else
					{
						isEscaped = true;
					}
				}
				else
				{
					int mask = 1;

					for (int y = 1; y <= 8; y++)
					{
						if ((b & mask) == mask)
						{
							bitmap.SetPixel(x, y, Color.Red);
						}
						else
						{
							bitmap.SetPixel(x, y, Color.Transparent);
						}

						mask <<= 1;
					}

					isEscaped = false;
					x++;
				}

				index++;
			}

			return new Tuple<string, Bitmap>(chars.ToString(), bitmap);
		}

		private static int MessureBitmapWidth(byte[] bytesArray)
		{
			int width = 0;

			int index = 0;
			bool isBeginningOfChar = true;
			bool isEscaped = false;

			while (index < bytesArray.Length)
			{
				byte b = bytesArray[index];
				if (isBeginningOfChar)
				{
					// Needs 2 bytes for Unicode
					if (index >= bytesArray.Length - 2)
					{
						throw new ArgumentException("Beginning of char at the end!");
					}

					index += 1;
					isBeginningOfChar = false;
				}
				else if ((b == 0xFF) && !isEscaped)
				{
					if (index == bytesArray.Length - 1)
					{
						throw new ArgumentException("Escape byte at the end!");
					}

					if (bytesArray[index + 1] == 0x00)
					{
						isBeginningOfChar = true;
						index++;
					}
					else
					{
						isEscaped = true;
					}
				}
				else
				{
					isEscaped = false;
					width++;
				}

				index++;
			}

			return width;
		}
	}
}